using GamP_SCPeriop.Helpers;
using GamP_SCPeriop.Server.Services;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Data.Template;
using GamP_SCPeriop.Shared.Entity.Model;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace GamP_SCPeriop.Server.Data
{
    /// <summary>
    /// Development-only test data. Wipes users, templates, pathways, enrollments, evaluations, badges and
    /// notifications, then recreates a realistic data set the same way the app does it:
    ///   1. the admins' Pathway Templates ("Moldes"), with their modules, parameters and badge templates;
    ///   2. each supervisor's pathway created FROM a template (same code as the "create pathway" button);
    ///   3. the supervisor setting the stage dates, then enrolling students (same code as "Adicionar Aluno");
    ///   4. grades, badges and notifications according to how far each stage is.
    /// Run it with: dotnet run --project GamP-SCPeriop.Server -- --seed
    /// </summary>
    public class DbSeeder
    {
        public const string TestPassword = "Teste123!";
        public const string TestEmailDomain = "gamp.test";

        // Length of each module in days, and how those days are split between the timeline stages
        private const int ModuleLengthDays = 21;
        private static readonly int[] StageLengthDays = { 4, 5, 6, 6 };

        private readonly AppDbContext _context;
        private readonly PathwayService _pathways;
        private readonly ILogger<DbSeeder> _logger;

        private Random _random = new();
        private DateTime _today;

        public DbSeeder(AppDbContext context, PathwayService pathways, ILogger<DbSeeder> logger)
        {
            _context = context;
            _pathways = pathways;
            _logger = logger;
        }

        public async Task<SeedSummary> ResetAndSeedAsync()
        {
            SeedSummary summary = null!;

            // EnableRetryOnFailure requires user transactions to run inside the execution strategy
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                _context.ChangeTracker.Clear();
                _random = new Random(2026); // Same data on every run
                _today = DateTime.Today;

                await using var transaction = await _context.Database.BeginTransactionAsync();
                await ClearDataAsync();
                summary = await SeedAsync();
                await transaction.CommitAsync();
            });

            _context.ChangeTracker.Clear();
            _logger.LogInformation("Test data seeded: {Summary}", summary);
            return summary;
        }

        #region Clear

        private async Task ClearDataAsync()
        {
            // Order matters: children before parents (several relations are DeleteBehavior.Restrict)
            await _context.UserBadges.ExecuteDeleteAsync();
            await _context.Notifications.ExecuteDeleteAsync();
            await _context.ComponentEvaluations.ExecuteDeleteAsync();
            await _context.EnrollmentModules.ExecuteDeleteAsync();
            await _context.Enrollments.ExecuteDeleteAsync();
            await _context.Badges.ExecuteDeleteAsync();
            await _context.ModuleStageTimelines.ExecuteDeleteAsync();
            await _context.ModuleComponents.Where(c => c.ParentComponentId != null).ExecuteDeleteAsync();
            await _context.ModuleComponents.ExecuteDeleteAsync();
            await _context.Modules.ExecuteDeleteAsync();
            await _context.Pathways.ExecuteDeleteAsync();

            // Templates are recreated too (the original sample "Moldes" included)
            await _context.BadgeTemplates.ExecuteDeleteAsync();
            await _context.ComponentTemplates.Where(c => c.ParentComponentTemplateId != null).ExecuteDeleteAsync();
            await _context.ComponentTemplates.ExecuteDeleteAsync();
            await _context.ModuleTemplates.ExecuteDeleteAsync();
            await _context.PathwayTemplates.ExecuteDeleteAsync();

            await _context.Users.ExecuteDeleteAsync();
        }

        #endregion

        #region Seed

        private async Task<SeedSummary> SeedAsync()
        {
            // BCrypt is slow on purpose, so every test account shares one hash
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(TestPassword);

            var admins = AdminNames.Select(n => NewUser(n, UserRole.Admin, passwordHash)).ToList();
            var supervisors = SupervisorNames.Select(n => NewUser(n, UserRole.Supervisor, passwordHash)).ToList();
            var students = StudentNames.Select(n => NewUser(n, UserRole.Supervisionado, passwordHash)).ToList();

            _context.Users.AddRange(admins);
            _context.Users.AddRange(supervisors);
            _context.Users.AddRange(students);
            await _context.SaveChangesAsync();

            // 1. The admins' templates
            var templateIds = new List<int>();
            foreach (var templatePlan in TemplatePlans)
                templateIds.Add(await CreateTemplateAsync(templatePlan));

            int enrollmentCount = 0, evaluationCount = 0, badgeCount = 0, userBadgeCount = 0;

            foreach (var plan in PathwayPlans)
            {
                var supervisor = supervisors[plan.SupervisorIndex];
                var templatePlan = TemplatePlans[plan.TemplateIndex];
                var pathwayStart = _today.AddDays(plan.StartOffsetDays);

                // 2. The supervisor creates the pathway from a template (modules, parameters and badges are copied)
                var pathway = await _pathways.CreatePathwayAsync(new PathwayCreateDto
                {
                    Title = plan.Title,
                    ProfessorId = supervisor.Id,
                    TemplateId = templateIds[plan.TemplateIndex],
                    MinimumPassScore = 50,
                    MinimumApprovalScore = 80,
                    StartDate = pathwayStart,
                    EndDate = pathwayStart.AddDays(templatePlan.ModuleIndexes.Length * ModuleLengthDays - 1)
                });
                pathway.IsArchived = plan.IsArchived;

                // 3. ...and sets the stage dates in the builder, module after module
                var baseModules = await _context.Modules
                    .Include(m => m.StageTimelines)
                    .Where(m => m.PathwayId == pathway.Id)
                    .OrderBy(m => m.OrderIndex)
                    .ToListAsync();
                for (int order = 0; order < baseModules.Count; order++)
                    SetStageDates(baseModules[order], pathwayStart.AddDays(order * ModuleLengthDays));
                await _context.SaveChangesAsync();

                // Badges copied from the template: module badges point at the pathway's (base) modules
                var badges = await _context.Badges.Where(b => b.PathwayId == pathway.Id).ToListAsync();
                badgeCount += badges.Count;
                var moduleBadges = badges
                    .Where(b => b.TriggerType == BadgeTriggerType.ModuleCompletion && int.TryParse(b.TriggerValue, out _))
                    .ToDictionary(b => int.Parse(b.TriggerValue));
                var excellenceBadge = badges.FirstOrDefault(b => b.TriggerType == BadgeTriggerType.ExcellenceGrade);
                var pathwayBadge = badges.FirstOrDefault(b => b.TriggerType == BadgeTriggerType.PathwayMilestone);

                // 4. Students enrolled (each gets independent copies of the modules)
                foreach (var studentIndex in plan.StudentIndexes)
                {
                    var student = students[studentIndex];
                    var skill = SkillProfiles[studentIndex % SkillProfiles.Length];

                    var enrollment = await _pathways.EnrollStudentAsync(student.Id, pathway.Id);
                    enrollmentCount++;

                    var clones = await _context.Modules
                        .Include(m => m.Components)
                        .Include(m => m.StageTimelines)
                        .Where(m => _context.EnrollmentModules.Any(em => em.EnrollmentId == enrollment.Id && em.ModuleId == m.Id))
                        .ToListAsync();

                    // 5. Evaluations, depending on how far along each stage is
                    var evaluations = new List<ComponentEvaluation>();
                    foreach (var clone in clones)
                    {
                        foreach (var component in clone.Components.Where(c => EvaluationRules.IsGradable(c, clone.Components)))
                        {
                            var evaluation = Evaluate(component, clone, enrollment, skill, plan.IsArchived);
                            if (evaluation != null) evaluations.Add(evaluation);
                        }
                    }
                    _context.ComponentEvaluations.AddRange(evaluations);
                    evaluationCount += evaluations.Count;

                    enrollment.ProgressPercentage = EvaluationRules.CalculateProgress(clones.SelectMany(c => c.Components).ToList(), evaluations);
                    enrollment.IsStarred = !plan.IsArchived && _random.Next(4) == 0;

                    // 6. Badges earned (same thresholds as BadgeService) + their notifications
                    var earned = new List<(Badge Badge, DateTime At)>();
                    foreach (var clone in clones)
                    {
                        var moduleEvaluations = evaluations.Where(e => clone.Components.Any(c => c.Id == e.ModuleComponentId)).ToList();
                        if (moduleEvaluations.Any()
                            && EvaluationRules.CalculateProgress(clone.Components, moduleEvaluations) >= 65
                            && moduleBadges.TryGetValue(clone.OriginalModuleId!.Value, out var moduleBadge))
                        {
                            earned.Add((moduleBadge, moduleEvaluations.Max(e => e.EvaluatedAt)));
                        }
                    }

                    var consistent = evaluations.Where(e => e.Status == ComponentStatus.Consistente).OrderBy(e => e.EvaluatedAt).ToList();
                    if (excellenceBadge != null && consistent.Count >= 5)
                        earned.Add((excellenceBadge, consistent[4].EvaluatedAt));

                    if (pathwayBadge != null && enrollment.ProgressPercentage >= 100)
                        earned.Add((pathwayBadge, evaluations.Max(e => e.EvaluatedAt)));

                    foreach (var (badge, at) in earned)
                    {
                        _context.UserBadges.Add(new UserBadge { UserId = student.Id, BadgeId = badge.Id, EarnedAt = at });
                        _context.Notifications.Add(NewNotification(student, supervisor, at,
                            "Nova Conquista! 🏆",
                            $"Desbloqueaste a badge '{badge.Name}'!",
                            "/badges"));
                    }
                    userBadgeCount += earned.Count;

                    if (evaluations.Any())
                    {
                        _context.Notifications.Add(NewNotification(student, supervisor, evaluations.Max(e => e.EvaluatedAt),
                            "Atualização de Avaliação 📊",
                            $"O professor atualizou as tuas avaliações no percurso '{pathway.Title}'.",
                            $"/pathway/{pathway.Id}"));
                    }

                    await _context.SaveChangesAsync();
                }
            }

            return new SeedSummary
            {
                Admins = admins.Count,
                Supervisors = supervisors.Count,
                Students = students.Count,
                Templates = TemplatePlans.Length,
                Pathways = PathwayPlans.Length,
                Enrollments = enrollmentCount,
                Evaluations = evaluationCount,
                Badges = badgeCount,
                BadgesEarned = userBadgeCount,
                Password = TestPassword
            };
        }

        /// <summary>
        /// A Pathway Template as the admin builds it in "Pathway Templates": modules with weights, theory items,
        /// the same parameters in all 4 practical stages, and the template's badges.
        /// </summary>
        private async Task<int> CreateTemplateAsync(TemplatePlan plan)
        {
            var template = new PathwayTemplate
            {
                Title = plan.Title,
                Description = plan.Description,
                MinimumApprovalScore = 80,
                IsAdminBase = true
            };

            var moduleWeights = SplitWeights(plan.ModuleIndexes.Length);
            var practicalStages = ModuleStageHelper.GetTimelineStages().ToList();

            for (int order = 0; order < plan.ModuleIndexes.Length; order++)
            {
                var blueprint = ModuleCatalog[plan.ModuleIndexes[order]];
                var moduleTemplate = new ModuleTemplate { Title = blueprint.Title, Weight = moduleWeights[order], OrderIndex = order };

                // Theory items: no weight (theory is never graded)
                for (int i = 0; i < blueprint.Theory.Length; i++)
                {
                    moduleTemplate.ComponentTemplates.Add(new ComponentTemplate
                    {
                        Title = blueprint.Theory[i].Title,
                        Description = blueprint.Theory[i].Description,
                        Stage = ModuleStage.Teorica,
                        Weight = 0,
                        OrderIndex = i
                    });
                }

                // Like the "new module" form: the same parameters in every practical stage, weights adding up to 100
                var parameterWeights = SplitWeights(blueprint.Parameters.Length);
                foreach (var stage in practicalStages)
                {
                    for (int i = 0; i < blueprint.Parameters.Length; i++)
                    {
                        var parameter = blueprint.Parameters[i];
                        var parent = new ComponentTemplate { Title = parameter.Title, Stage = stage, Weight = parameterWeights[i], OrderIndex = i };
                        moduleTemplate.ComponentTemplates.Add(parent);

                        if (parameter.Children == null) continue;

                        var childWeights = SplitWeights(parameter.Children.Length);
                        for (int j = 0; j < parameter.Children.Length; j++)
                        {
                            moduleTemplate.ComponentTemplates.Add(new ComponentTemplate
                            {
                                Title = parameter.Children[j],
                                Stage = stage,
                                Weight = childWeights[j],
                                OrderIndex = j,
                                ParentComponentTemplate = parent
                            });
                        }
                    }
                }

                template.ModuleTemplates.Add(moduleTemplate);
            }

            _context.PathwayTemplates.Add(template);
            await _context.SaveChangesAsync();

            // Badge templates: module badges reference the template module (translated to the real module when a pathway is created)
            var moduleTemplates = template.ModuleTemplates.OrderBy(m => m.OrderIndex).ToList();
            for (int order = 0; order < moduleTemplates.Count; order++)
            {
                var blueprint = ModuleCatalog[plan.ModuleIndexes[order]];
                _context.BadgeTemplates.Add(new BadgeTemplate
                {
                    PathwayTemplateId = template.Id,
                    Name = blueprint.BadgeName,
                    Description = $"Concluíste o módulo \"{blueprint.Title}\" com pelo menos 65% das práticas bem avaliadas.",
                    Icon = blueprint.BadgeIcon,
                    Tier = blueprint.BadgeTier,
                    TriggerType = BadgeTriggerType.ModuleCompletion,
                    TriggerValue = moduleTemplates[order].Id.ToString()
                });
            }

            _context.BadgeTemplates.Add(new BadgeTemplate
            {
                PathwayTemplateId = template.Id,
                Name = "Excelência Clínica",
                Description = "Obtiveste pelo menos 5 avaliações \"Consistente\" neste percurso.",
                Icon = "bi-star-fill",
                Tier = BadgeTier.Epic,
                TriggerType = BadgeTriggerType.ExcellenceGrade,
                TriggerValue = "5"
            });
            _context.BadgeTemplates.Add(new BadgeTemplate
            {
                PathwayTemplateId = template.Id,
                Name = "Percurso Concluído",
                Description = "Concluíste todas as práticas do percurso.",
                Icon = "bi-trophy-fill",
                Tier = BadgeTier.Legendary,
                TriggerType = BadgeTriggerType.PathwayMilestone,
                TriggerValue = "100"
            });
            await _context.SaveChangesAsync();

            return template.Id;
        }

        /// <summary>
        /// What the supervisor does in the builder: consecutive date ranges for the module's practical stages
        /// </summary>
        private static void SetStageDates(Module module, DateTime moduleStart)
        {
            var stageStart = moduleStart;
            var timelines = module.StageTimelines!.OrderBy(t => t.Stage).ToList();
            for (int i = 0; i < timelines.Count; i++)
            {
                var length = StageLengthDays[Math.Min(i, StageLengthDays.Length - 1)];
                timelines[i].StartDate = stageStart;
                timelines[i].EndDate = stageStart.AddDays(length - 1);
                stageStart = stageStart.AddDays(length);
            }
        }

        /// <summary>
        /// Splits 100 into <paramref name="count"/> weights with at most 2 decimals, the last one taking the remainder
        /// (same rounding as the builder's auto-distribution, e.g. 33.33 / 33.33 / 33.34)
        /// </summary>
        private static float[] SplitWeights(int count)
        {
            var split = (float)Math.Round(100.0 / count, 2);
            var weights = Enumerable.Repeat(split, count).ToArray();
            weights[^1] = (float)Math.Round(100.0 - split * (count - 1), 2);
            return weights;
        }

        /// <summary>
        /// Finished stages are graded, the current stage is partly graded, future stages are left pending.
        /// Observation stages are a checklist (ticked = Consistente); practice stages get one of the four grades.
        /// </summary>
        private ComponentEvaluation? Evaluate(ModuleComponent component, Module module, Enrollment enrollment, SkillProfile skill, bool forceFinished)
        {
            var timeline = module.StageTimelines!.First(t => t.Stage == component.Stage);
            var stageStart = timeline.StartDate!.Value;
            var stageEnd = timeline.EndDate!.Value;

            bool isFinished = forceFinished || stageEnd < _today;
            if (!isFinished && (stageStart > _today || _random.Next(2) == 0)) return null;

            ComponentStatus status;
            if (component.Stage is ModuleStage.ObservacaoPassiva or ModuleStage.ObservacaoParticipada)
            {
                // Unticked items simply have no evaluation
                if (_random.Next(100) >= skill.ObservationTicked) return null;
                status = ComponentStatus.Consistente;
            }
            else
            {
                status = skill.PickGrade(_random);
            }

            var lastDay = stageEnd < _today ? stageEnd : _today;
            var span = Math.Max(0, (lastDay - stageStart).Days);
            var evaluatedAt = stageStart.AddDays(_random.Next(span + 1)).AddHours(9 + _random.Next(9));
            if (evaluatedAt > DateTime.Now) evaluatedAt = DateTime.Now.AddMinutes(-_random.Next(30, 240));

            return new ComponentEvaluation
            {
                Enrollment = enrollment,
                ModuleComponentId = component.Id,
                Status = status,
                EvaluatedAt = evaluatedAt.ToUniversalTime() // the API stores UTC (DateTime.UtcNow)
            };
        }

        private Notification NewNotification(User receiver, User sender, DateTime createdAt, string title, string message, string targetUrl) => new()
        {
            ReceiverId = receiver.Id,
            SenderId = sender.Id,
            Title = title,
            Message = message,
            TargetUrl = targetUrl,
            CreatedAt = createdAt,
            IsRead = createdAt < _today.AddDays(-7) || _random.Next(2) == 0
        };

        private static User NewUser(string fullName, UserRole role, string passwordHash) => new()
        {
            FullName = fullName,
            Email = ToEmail(fullName),
            Password = passwordHash,
            Role = role
        };

        /// <summary>
        /// "João Pedro Carvalho" -> "joao.carvalho@gamp.test"
        /// </summary>
        private static string ToEmail(string fullName)
        {
            var names = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var local = $"{names[0]}.{names[^1]}".ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var ascii = new string(local.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray());
            return $"{ascii}@{TestEmailDomain}";
        }

        #endregion

        #region Test data

        private static readonly string[] AdminNames =
        {
            "Ana Margarida Ferreira",
            "João Pedro Carvalho"
        };

        private static readonly string[] SupervisorNames =
        {
            "Rui Manuel Sousa",
            "Catarina Isabel Lopes",
            "Miguel Ângelo Ribeiro",
            "Sofia Alexandra Martins",
            "Tiago Filipe Gonçalves"
        };

        private static readonly string[] StudentNames =
        {
            "Beatriz Oliveira Santos", "Diogo Miguel Costa", "Inês Rodrigues Pereira", "Francisco José Almeida",
            "Mariana Sofia Fernandes", "Gonçalo Nuno Marques", "Leonor Cardoso Pinto", "Rodrigo André Teixeira",
            "Matilde Correia Mendes", "Tomás Henrique Nunes", "Carolina Vieira Moreira", "Duarte Ricardo Rocha",
            "Joana Filipa Monteiro", "Afonso Luís Barbosa", "Rita Isabel Fonseca", "Martim Alexandre Coelho",
            "Sara Cristina Machado", "Guilherme Paulo Antunes", "Marta Raquel Cunha", "Pedro Miguel Azevedo"
        };

        /// <summary>
        /// The admins' Pathway Templates ("Moldes"); ModuleIndexes point into <see cref="ModuleCatalog"/>
        /// </summary>
        private static readonly TemplatePlan[] TemplatePlans =
        {
            new("Molde — Bloco Operatório", "Acolhimento, instrumentação e circulação em bloco operatório.", new[] { 0, 2, 3 }),
            new("Molde — Anestesiologia", "Enfermagem de anestesia e cuidados pós-anestésicos.", new[] { 0, 1, 4 }),
            new("Molde — Cirurgia Geral", "Percurso completo em bloco de cirurgia geral.", new[] { 0, 2, 3, 4 }),
            new("Molde — Recobro (UCPA)", "Estágio curto na Unidade de Cuidados Pós-Anestésicos.", new[] { 0, 4 }),
            new("Molde — Ortopedia", "Acolhimento e instrumentação em cirurgia ortopédica.", new[] { 0, 2 })
        };

        /// <summary>
        /// Supervisors' pathways, each created from a template. Start offsets are relative to today,
        /// so the data always has past, current and future pathways.
        /// </summary>
        private static readonly PathwayPlan[] PathwayPlans =
        {
            new("Estágio em Bloco Operatório — Hospital de Braga", 0, 0, -45, false, new[] { 0, 1, 2, 3 }),
            new("Estágio em Anestesiologia — ULS Santo António", 1, 1, -25, false, new[] { 4, 5, 6, 7 }),
            new("Estágio em Cirurgia Geral — ULS São João", 2, 2, -70, false, new[] { 8, 9, 10, 11 }),
            new("Estágio em Recobro (UCPA) — Hospital de Guimarães", 3, 3, 10, false, new[] { 12, 13, 14, 15 }),
            new("Estágio em Ortopedia — ULS Alto Minho", 4, 4, -50, false, new[] { 16, 17, 18, 19 }),
            new("Estágio em Bloco Operatório — 2.º Semestre 2025/26", 0, 0, -200, true, new[] { 0, 4, 8, 12, 16 })
        };

        /// <summary>
        /// Each module: theory items, then the parameters that are copied into all 4 practical stages
        /// </summary>
        private static readonly ModuleBlueprint[] ModuleCatalog =
        {
            new("Acolhimento e Normas do Serviço", "Primeiros Passos", "bi-door-open-fill", BadgeTier.Common,
                new TheoryBlueprint[]
                {
                    new("Manual de Acolhimento", "Leitura do manual de acolhimento do serviço."),
                    new("Normas de Prevenção e Controlo de Infeção", "Circular normativa do serviço sobre precauções básicas.")
                },
                new ParameterBlueprint[]
                {
                    new("Higienização cirúrgica das mãos"),
                    new("Utilização de equipamento de proteção individual"),
                    new("Circuito do doente no bloco operatório")
                }),
            new("Enfermagem de Anestesia", "Guardião da Anestesia", "bi-heart-pulse-fill", BadgeTier.Rare,
                new TheoryBlueprint[]
                {
                    new("Farmacologia anestésica", "Fármacos de indução, manutenção e emergência.")
                },
                new ParameterBlueprint[]
                {
                    new("Preparação do posto de anestesia"),
                    new("Monitorização hemodinâmica"),
                    new("Apoio à indução anestésica"),
                    new("Registos clínicos (SClínico)", new[]
                    {
                        "Regista diagnósticos de enfermagem",
                        "Regista atitudes terapêuticas",
                        "Regista sinais vitais e glicemia capilar"
                    })
                }),
            new("Enfermagem de Instrumentação", "Mãos de Instrumentista", "bi-scissors", BadgeTier.Uncommon,
                new TheoryBlueprint[]
                {
                    new("Instrumental cirúrgico básico", "Identificação e manuseamento do instrumental mais usado.")
                },
                new ParameterBlueprint[]
                {
                    new("Organização da mesa operatória"),
                    new("Contagem de compressas e instrumental"),
                    new("Instrumentação cirúrgica"),
                    new("Lista de verificação de segurança cirúrgica")
                }),
            new("Enfermagem de Circulação", "Circulante Exemplar", "bi-arrow-repeat", BadgeTier.Uncommon,
                new TheoryBlueprint[]
                {
                    new("Posicionamento cirúrgico do doente", "Posições cirúrgicas e prevenção de lesões.")
                },
                new ParameterBlueprint[]
                {
                    new("Acolhimento do doente na sala"),
                    new("Posicionamento do doente"),
                    new("Gestão de material e consumíveis (Ghaf)", new[]
                    {
                        "Efetua débitos ao armazém",
                        "Efetua devoluções ao armazém"
                    })
                }),
            new("Unidade de Cuidados Pós-Anestésicos (UCPA)", "Vigilante do Recobro", "bi-eye-fill", BadgeTier.Rare,
                new TheoryBlueprint[]
                {
                    new("Escala de Aldrete e critérios de alta", "Avaliação do doente no recobro.")
                },
                new ParameterBlueprint[]
                {
                    new("Receção do doente no recobro"),
                    new("Avaliação e controlo da dor pós-operatória"),
                    new("Preparação da alta da UCPA")
                })
        };

        /// <summary>
        /// Chances (%) of Consistente, AcimaDaMedia, AbaixoDaMedia and Inconsistente for each kind of student,
        /// and the chance (%) of each observation checklist item being ticked
        /// </summary>
        private static readonly SkillProfile[] SkillProfiles =
        {
            new(60, 30, 8, 2, 95),   // strong
            new(35, 40, 20, 5, 85),  // good
            new(15, 35, 35, 15, 70), // average
            new(5, 20, 40, 35, 55)   // struggling
        };

        #endregion

        #region Types

        private record TemplatePlan(string Title, string Description, int[] ModuleIndexes);

        private record PathwayPlan(string Title, int SupervisorIndex, int TemplateIndex, int StartOffsetDays, bool IsArchived, int[] StudentIndexes);

        private record ModuleBlueprint(string Title, string BadgeName, string BadgeIcon, BadgeTier BadgeTier, TheoryBlueprint[] Theory, ParameterBlueprint[] Parameters);

        private record TheoryBlueprint(string Title, string? Description = null);

        private record ParameterBlueprint(string Title, string[]? Children = null);

        private record SkillProfile(int Consistente, int AcimaDaMedia, int AbaixoDaMedia, int Inconsistente, int ObservationTicked)
        {
            public ComponentStatus PickGrade(Random random)
            {
                int roll = random.Next(Consistente + AcimaDaMedia + AbaixoDaMedia + Inconsistente);
                if ((roll -= Consistente) < 0) return ComponentStatus.Consistente;
                if ((roll -= AcimaDaMedia) < 0) return ComponentStatus.AcimaDaMedia;
                if (roll - AbaixoDaMedia < 0) return ComponentStatus.AbaixoDaMedia;
                return ComponentStatus.Inconsistente;
            }
        }

        #endregion
    }

    public class SeedSummary
    {
        public int Admins { get; set; }
        public int Supervisors { get; set; }
        public int Students { get; set; }
        public int Templates { get; set; }
        public int Pathways { get; set; }
        public int Enrollments { get; set; }
        public int Evaluations { get; set; }
        public int Badges { get; set; }
        public int BadgesEarned { get; set; }
        public string Password { get; set; } = string.Empty;

        public override string ToString() =>
            $"{Admins} admins, {Supervisors} supervisors, {Students} students, {Templates} templates, {Pathways} pathways, " +
            $"{Enrollments} enrollments, {Evaluations} evaluations, {BadgesEarned}/{Badges} badges earned";
    }
}
