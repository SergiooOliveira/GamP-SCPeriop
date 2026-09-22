using GamP_SCPeriop.Helpers;
using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Entity.Model;
using GamP_SCPeriop.Shared.Enum;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace GamP_SCPeriop.Server.Data
{
    /// <summary>
    /// Development-only test data. Wipes users, pathways, enrollments, evaluations, badges and
    /// notifications, then recreates a realistic data set (2 admins, 5 supervisors, 20 students).
    /// Admin (base) templates are kept.
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
        private readonly ILogger<DbSeeder> _logger;

        private Random _random = new();
        private DateTime _today;

        public DbSeeder(AppDbContext context, ILogger<DbSeeder> logger)
        {
            _context = context;
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

            // Personal templates belong to supervisors that are about to be removed. Admin base templates stay.
            var personalTemplateIds = await _context.PathwayTemplates
                .Where(t => t.SupervisorOwnerId != null)
                .Select(t => t.Id)
                .ToListAsync();

            if (personalTemplateIds.Any())
            {
                await _context.BadgeTemplates.Where(b => personalTemplateIds.Contains(b.PathwayTemplateId)).ExecuteDeleteAsync();
                await _context.ComponentTemplates
                    .Where(c => c.ParentComponentTemplateId != null && personalTemplateIds.Contains(c.ModuleTemplate!.PathwayTemplateId))
                    .ExecuteDeleteAsync();
                await _context.ComponentTemplates.Where(c => personalTemplateIds.Contains(c.ModuleTemplate!.PathwayTemplateId)).ExecuteDeleteAsync();
                await _context.ModuleTemplates.Where(m => personalTemplateIds.Contains(m.PathwayTemplateId)).ExecuteDeleteAsync();
                await _context.PathwayTemplates.Where(t => personalTemplateIds.Contains(t.Id)).ExecuteDeleteAsync();
            }

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

            int enrollmentCount = 0, evaluationCount = 0, badgeCount = 0, userBadgeCount = 0;

            foreach (var plan in PathwayPlans)
            {
                var supervisor = supervisors[plan.SupervisorIndex];
                var pathwayStart = _today.AddDays(plan.StartOffsetDays);

                // 1. The pathway and its base ("geral") modules
                var pathway = new Pathway
                {
                    Title = plan.Title,
                    ProfessorId = supervisor.Id,
                    MinimumPassScore = 50,
                    MinimumApprovalScore = 80,
                    IsArchived = plan.IsArchived,
                    StartDate = pathwayStart,
                    EndDate = pathwayStart.AddDays(plan.ModuleIndexes.Length * ModuleLengthDays - 1)
                };

                var moduleWeights = SplitWeights(plan.ModuleIndexes.Length);
                for (int order = 0; order < plan.ModuleIndexes.Length; order++)
                {
                    var blueprint = ModuleCatalog[plan.ModuleIndexes[order]];
                    var moduleStart = pathwayStart.AddDays(order * ModuleLengthDays);
                    pathway.Modules.Add(BuildBaseModule(blueprint, order, moduleWeights[order], moduleStart));
                }

                _context.Pathways.Add(pathway);
                await _context.SaveChangesAsync();

                // 2. Badges, frozen onto this pathway (module badges point at the base module id)
                var moduleBadges = new Dictionary<int, Badge>();
                for (int order = 0; order < pathway.Modules.Count; order++)
                {
                    var blueprint = ModuleCatalog[plan.ModuleIndexes[order]];
                    var badge = new Badge
                    {
                        PathwayId = pathway.Id,
                        Name = blueprint.BadgeName,
                        Description = $"Concluíste o módulo \"{blueprint.Title}\" com pelo menos 65% das práticas bem avaliadas.",
                        Icon = blueprint.BadgeIcon,
                        Tier = blueprint.BadgeTier,
                        TriggerType = BadgeTriggerType.ModuleCompletion,
                        TriggerValue = pathway.Modules[order].Id.ToString()
                    };
                    moduleBadges[pathway.Modules[order].Id] = badge;
                    _context.Badges.Add(badge);
                }

                var excellenceBadge = new Badge
                {
                    PathwayId = pathway.Id,
                    Name = "Excelência Clínica",
                    Description = "Obtiveste pelo menos 5 avaliações \"Consistente\" neste percurso.",
                    Icon = "bi-star-fill",
                    Tier = BadgeTier.Epic,
                    TriggerType = BadgeTriggerType.ExcellenceGrade,
                    TriggerValue = "5"
                };
                var pathwayBadge = new Badge
                {
                    PathwayId = pathway.Id,
                    Name = "Percurso Concluído",
                    Description = $"Concluíste todas as práticas do percurso \"{pathway.Title}\".",
                    Icon = "bi-trophy-fill",
                    Tier = BadgeTier.Legendary,
                    TriggerType = BadgeTriggerType.PathwayMilestone,
                    TriggerValue = "100"
                };
                _context.Badges.AddRange(excellenceBadge, pathwayBadge);
                badgeCount += moduleBadges.Count + 2;
                await _context.SaveChangesAsync();

                // 3. Enrollments: every student gets independent copies of the modules (same as EnrollmentController)
                foreach (var studentIndex in plan.StudentIndexes)
                {
                    var student = students[studentIndex];
                    var skill = SkillProfiles[studentIndex % SkillProfiles.Length];

                    var enrollment = new Enrollment { StudentId = student.Id, PathwayId = pathway.Id };
                    _context.Enrollments.Add(enrollment);

                    var clones = pathway.Modules.Select(m => CloneForEnrollment(m, enrollment)).ToList();
                    await _context.SaveChangesAsync();
                    enrollmentCount++;

                    // 4. Evaluations, depending on how far along each stage is
                    var evaluations = new List<ComponentEvaluation>();
                    foreach (var clone in clones)
                    {
                        foreach (var component in GradableComponents(clone))
                        {
                            var evaluation = Evaluate(component, clone, enrollment, skill, plan.IsArchived);
                            if (evaluation != null) evaluations.Add(evaluation);
                        }
                    }
                    _context.ComponentEvaluations.AddRange(evaluations);
                    evaluationCount += evaluations.Count;

                    enrollment.ProgressPercentage = CalculateProgress(clones.SelectMany(c => c.Components), evaluations);
                    enrollment.IsStarred = plan.IsArchived == false && _random.Next(4) == 0;

                    // 5. Badges earned (same thresholds as BadgeService) + their notifications
                    var earned = new List<(Badge Badge, DateTime At)>();
                    foreach (var clone in clones)
                    {
                        var moduleEvaluations = evaluations.Where(e => clone.Components.Any(c => c.Id == e.ModuleComponentId)).ToList();
                        if (CalculateProgress(clone.Components, moduleEvaluations) >= 65)
                            earned.Add((moduleBadges[clone.OriginalModuleId!.Value], moduleEvaluations.Max(e => e.EvaluatedAt)));
                    }

                    var consistent = evaluations.Where(e => e.Status == ComponentStatus.Consistente).OrderBy(e => e.EvaluatedAt).ToList();
                    if (consistent.Count >= 5)
                        earned.Add((excellenceBadge, consistent[4].EvaluatedAt));

                    if (enrollment.ProgressPercentage >= 100)
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
                Pathways = PathwayPlans.Length,
                Enrollments = enrollmentCount,
                Evaluations = evaluationCount,
                Badges = badgeCount,
                BadgesEarned = userBadgeCount,
                Password = TestPassword
            };
        }

        private Module BuildBaseModule(ModuleBlueprint blueprint, int order, float weight, DateTime moduleStart)
        {
            var module = new Module
            {
                Title = blueprint.Title,
                Weight = weight,
                OrderIndex = order,
                StageTimelines = new List<ModuleStageTimelineDto>()
            };

            var stageStart = moduleStart;
            var stages = ModuleStageHelper.GetTimelineStages().ToList();
            for (int i = 0; i < stages.Count; i++)
            {
                var length = StageLengthDays[Math.Min(i, StageLengthDays.Length - 1)];
                module.StageTimelines.Add(new ModuleStageTimelineDto
                {
                    Stage = stages[i],
                    StartDate = stageStart,
                    EndDate = stageStart.AddDays(length - 1)
                });
                stageStart = stageStart.AddDays(length);
            }

            // Theory items are added one by one in the builder, with no weight (theory is never graded)
            for (int i = 0; i < blueprint.Theory.Length; i++)
            {
                module.Components.Add(new ModuleComponent
                {
                    Title = blueprint.Theory[i].Title,
                    Description = blueprint.Theory[i].Description,
                    Stage = ModuleStage.Teorica,
                    Weight = 0,
                    OrderIndex = i
                });
            }

            // Like the "new module" form: the same parameters are copied into every practical stage,
            // with weights that add up to 100 per stage (and per group of sub-parameters)
            var parameterWeights = SplitWeights(blueprint.Parameters.Length);
            foreach (var stage in stages)
            {
                for (int i = 0; i < blueprint.Parameters.Length; i++)
                {
                    var parameter = blueprint.Parameters[i];
                    var parent = new ModuleComponent { Title = parameter.Title, Stage = stage, Weight = parameterWeights[i], OrderIndex = i };
                    module.Components.Add(parent);

                    if (parameter.Children == null) continue;

                    var childWeights = SplitWeights(parameter.Children.Length);
                    for (int j = 0; j < parameter.Children.Length; j++)
                    {
                        module.Components.Add(new ModuleComponent
                        {
                            Title = parameter.Children[j],
                            Stage = stage,
                            Weight = childWeights[j],
                            OrderIndex = j,
                            ParentComponent = parent
                        });
                    }
                }
            }

            return module;
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
        /// Mirrors EnrollmentController.CreateEnrollment: an independent copy of the module per student
        /// </summary>
        private Module CloneForEnrollment(Module baseModule, Enrollment enrollment)
        {
            var clone = new Module
            {
                Title = baseModule.Title,
                Weight = baseModule.Weight,
                OrderIndex = baseModule.OrderIndex,
                PathwayId = null,
                IsFromTemplate = true,
                OriginalModuleId = baseModule.Id,
                StageTimelines = baseModule.StageTimelines!.Select(t => new ModuleStageTimelineDto
                {
                    Stage = t.Stage,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate
                }).ToList()
            };

            var parentMap = new Dictionary<ModuleComponent, ModuleComponent>();
            foreach (var component in baseModule.Components.OrderBy(c => c.ParentComponent == null ? 0 : 1))
            {
                var copy = new ModuleComponent
                {
                    Title = component.Title,
                    Description = component.Description,
                    Stage = component.Stage,
                    Weight = component.Weight,
                    OrderIndex = component.OrderIndex,
                    PdfFilePath = component.PdfFilePath,
                    IsFromTemplate = true,
                    ParentComponent = component.ParentComponent == null ? null : parentMap[component.ParentComponent]
                };
                parentMap[component] = copy;
                clone.Components.Add(copy);
            }

            _context.Modules.Add(clone);
            _context.EnrollmentModules.Add(new EnrollmentModule
            {
                Enrollment = enrollment,
                Module = clone,
                StartDate = clone.StageTimelines.Min(t => t.StartDate),
                EndDate = clone.StageTimelines.Max(t => t.EndDate)
            });

            return clone;
        }

        /// <summary>
        /// What the evaluation page lets a supervisor grade: practical-stage items with a weight,
        /// excluding group headers (their sub-parameters are graded instead). Theory is never graded.
        /// </summary>
        private static IEnumerable<ModuleComponent> GradableComponents(Module module) =>
            module.Components.Where(c =>
                c.Stage != ModuleStage.Teorica &&
                c.Weight > 0 &&
                !module.Components.Any(child => child.ParentComponent == c));

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

        /// <summary>
        /// Same formula as EvaluationController: % of weighted practice leaves graded AcimaDaMedia or Consistente
        /// </summary>
        private static int CalculateProgress(IEnumerable<ModuleComponent> components, IEnumerable<ComponentEvaluation> evaluations)
        {
            var list = components.ToList();
            var parentIds = list.Where(c => c.ParentComponentId.HasValue).Select(c => c.ParentComponentId!.Value).ToHashSet();

            var assessableIds = list
                .Where(c => c.Stage == ModuleStage.PraticaSupervisionada || c.Stage == ModuleStage.PraticaAssistida)
                .Where(c => !parentIds.Contains(c.Id))
                .Where(c => c.Weight > 0)
                .Select(c => c.Id)
                .ToHashSet();

            if (!assessableIds.Any()) return 0;

            int completed = evaluations.Count(e =>
                assessableIds.Contains(e.ModuleComponentId) &&
                (e.Status == ComponentStatus.AcimaDaMedia || e.Status == ComponentStatus.Consistente));

            return (int)((double)completed / assessableIds.Count * 100);
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
        /// Start offsets are relative to today, so the data always has past, current and future pathways
        /// </summary>
        private static readonly PathwayPlan[] PathwayPlans =
        {
            new("Estágio em Bloco Operatório — Hospital de Braga", 0, new[] { 0, 2, 3 }, -45, false, new[] { 0, 1, 2, 3 }),
            new("Estágio em Anestesiologia — ULS Santo António", 1, new[] { 0, 1, 4 }, -25, false, new[] { 4, 5, 6, 7 }),
            new("Estágio em Cirurgia Geral — ULS São João", 2, new[] { 0, 2, 3, 4 }, -70, false, new[] { 8, 9, 10, 11 }),
            new("Estágio em Recobro (UCPA) — Hospital de Guimarães", 3, new[] { 0, 4 }, 10, false, new[] { 12, 13, 14, 15 }),
            new("Estágio em Ortopedia — ULS Alto Minho", 4, new[] { 0, 2 }, -50, false, new[] { 16, 17, 18, 19 }),
            new("Estágio em Bloco Operatório — 2.º Semestre 2025/26", 0, new[] { 0, 2, 3 }, -200, true, new[] { 0, 4, 8, 12, 16 })
        };

        /// <summary>
        /// Each module: theory items, then the parameters that the builder copies into all 4 practical stages
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

        private record PathwayPlan(string Title, int SupervisorIndex, int[] ModuleIndexes, int StartOffsetDays, bool IsArchived, int[] StudentIndexes);

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
        public int Pathways { get; set; }
        public int Enrollments { get; set; }
        public int Evaluations { get; set; }
        public int Badges { get; set; }
        public int BadgesEarned { get; set; }
        public string Password { get; set; } = string.Empty;

        public override string ToString() =>
            $"{Admins} admins, {Supervisors} supervisors, {Students} students, {Pathways} pathways, " +
            $"{Enrollments} enrollments, {Evaluations} evaluations, {BadgesEarned}/{Badges} badges earned";
    }
}
