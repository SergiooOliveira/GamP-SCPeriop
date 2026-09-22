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

                float moduleWeight = 100f / plan.ModuleIndexes.Length;
                for (int order = 0; order < plan.ModuleIndexes.Length; order++)
                {
                    var blueprint = ModuleCatalog[plan.ModuleIndexes[order]];
                    var moduleStart = pathwayStart.AddDays(order * ModuleLengthDays);
                    pathway.Modules.Add(BuildBaseModule(blueprint, order, moduleWeight, moduleStart));
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
                        foreach (var component in LeafComponents(clone))
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

            float componentWeight = 100f / blueprint.Components.Length;
            for (int i = 0; i < blueprint.Components.Length; i++)
            {
                var parentBlueprint = blueprint.Components[i];
                var parent = NewComponent(parentBlueprint, i, componentWeight);
                module.Components.Add(parent);

                if (parentBlueprint.Children == null) continue;

                float childWeight = 100f / parentBlueprint.Children.Length;
                for (int j = 0; j < parentBlueprint.Children.Length; j++)
                {
                    var child = NewComponent(parentBlueprint.Children[j], j, childWeight);
                    child.ParentComponent = parent;
                    module.Components.Add(child);
                }
            }

            return module;
        }

        private static ModuleComponent NewComponent(ComponentBlueprint blueprint, int order, float weight) => new()
        {
            Title = blueprint.Title,
            Description = blueprint.Description,
            Stage = blueprint.Stage,
            Weight = weight,
            OrderIndex = order
        };

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

        private static IEnumerable<ModuleComponent> LeafComponents(Module module) =>
            module.Components.Where(c => !module.Components.Any(child => child.ParentComponent == c));

        /// <summary>
        /// Finished stages get a final grade, the current stage is partly graded, future stages are left pending
        /// </summary>
        private ComponentEvaluation? Evaluate(ModuleComponent component, Module module, Enrollment enrollment, SkillProfile skill, bool forceFinished)
        {
            var timelines = module.StageTimelines!;
            var moduleStart = timelines.Min(t => t.StartDate!.Value);

            // Theory has no timeline of its own: it is due by the end of the first stage
            var (stageStart, stageEnd) = component.Stage == ModuleStage.Teorica
                ? (moduleStart, timelines.OrderBy(t => t.StartDate).First().EndDate!.Value)
                : (timelines.First(t => t.Stage == component.Stage).StartDate!.Value, timelines.First(t => t.Stage == component.Stage).EndDate!.Value);

            ComponentStatus status;
            if (forceFinished || stageEnd < _today)
            {
                status = skill.PickGrade(_random);
            }
            else if (stageStart <= _today)
            {
                if (_random.Next(2) == 0) return null;
                status = _random.Next(3) == 0 ? skill.PickGrade(_random) : ComponentStatus.EmProgresso;
            }
            else
            {
                return null;
            }

            var lastDay = stageEnd < _today ? stageEnd : _today;
            var span = Math.Max(0, (lastDay - stageStart).Days);
            var evaluatedAt = stageStart.AddDays(_random.Next(span + 1)).AddHours(9 + _random.Next(9));

            return new ComponentEvaluation
            {
                Enrollment = enrollment,
                ModuleComponentId = component.Id,
                Status = status,
                EvaluatedAt = evaluatedAt
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

        private static readonly ModuleBlueprint[] ModuleCatalog =
        {
            new("Acolhimento e Normas do Serviço", "Primeiros Passos", "bi-door-open-fill", BadgeTier.Common, new ComponentBlueprint[]
            {
                new("Manual de Acolhimento", ModuleStage.Teorica, "Leitura do manual de acolhimento do serviço."),
                new("Normas de Prevenção e Controlo de Infeção", ModuleStage.Teorica),
                new("Circuito do doente no bloco operatório", ModuleStage.ObservacaoPassiva),
                new("Higienização cirúrgica das mãos", ModuleStage.PraticaAssistida),
                new("Colocação de equipamento de proteção individual", ModuleStage.PraticaAssistida),
                new("Preparação da sala operatória", ModuleStage.PraticaSupervisionada)
            }),
            new("Enfermagem de Anestesia", "Guardião da Anestesia", "bi-heart-pulse-fill", BadgeTier.Rare, new ComponentBlueprint[]
            {
                new("Farmacologia anestésica", ModuleStage.Teorica, "Fármacos de indução, manutenção e emergência."),
                new("Preparação do posto de anestesia", ModuleStage.ObservacaoParticipada),
                new("Verificação do ventilador e do aspirador", ModuleStage.PraticaAssistida),
                new("Monitorização hemodinâmica", ModuleStage.PraticaAssistida),
                new("Apoio à indução anestésica", ModuleStage.PraticaSupervisionada),
                new("Registos clínicos (SClínico)", ModuleStage.PraticaSupervisionada, null, new ComponentBlueprint[]
                {
                    new("Regista diagnósticos de enfermagem", ModuleStage.PraticaSupervisionada),
                    new("Regista atitudes terapêuticas", ModuleStage.PraticaSupervisionada),
                    new("Regista sinais vitais e glicemia capilar", ModuleStage.PraticaSupervisionada)
                })
            }),
            new("Enfermagem de Instrumentação", "Mãos de Instrumentista", "bi-scissors", BadgeTier.Uncommon, new ComponentBlueprint[]
            {
                new("Instrumental cirúrgico básico", ModuleStage.Teorica),
                new("Organização da mesa operatória", ModuleStage.ObservacaoPassiva),
                new("Montagem da mesa operatória", ModuleStage.ObservacaoParticipada),
                new("Contagem de compressas e instrumental", ModuleStage.PraticaAssistida),
                new("Instrumentação em cirurgia de pequena complexidade", ModuleStage.PraticaSupervisionada),
                new("Lista de verificação de segurança cirúrgica", ModuleStage.PraticaSupervisionada)
            }),
            new("Enfermagem de Circulação", "Circulante Exemplar", "bi-arrow-repeat", BadgeTier.Uncommon, new ComponentBlueprint[]
            {
                new("Posicionamento cirúrgico do doente", ModuleStage.Teorica),
                new("Acolhimento do doente na sala", ModuleStage.ObservacaoPassiva),
                new("Gestão de material e consumíveis (Ghaf)", ModuleStage.PraticaAssistida, null, new ComponentBlueprint[]
                {
                    new("Efetua débitos ao armazém", ModuleStage.PraticaAssistida),
                    new("Efetua devoluções ao armazém", ModuleStage.PraticaAssistida)
                }),
                new("Colaboração no posicionamento do doente", ModuleStage.PraticaAssistida),
                new("Circulação em cirurgia programada", ModuleStage.PraticaSupervisionada)
            }),
            new("Unidade de Cuidados Pós-Anestésicos (UCPA)", "Vigilante do Recobro", "bi-eye-fill", BadgeTier.Rare, new ComponentBlueprint[]
            {
                new("Escala de Aldrete e critérios de alta", ModuleStage.Teorica),
                new("Transferência do doente para a UCPA", ModuleStage.ObservacaoPassiva),
                new("Receção do doente no recobro", ModuleStage.PraticaAssistida),
                new("Avaliação e controlo da dor pós-operatória", ModuleStage.PraticaSupervisionada),
                new("Preparação da alta da UCPA", ModuleStage.PraticaSupervisionada)
            })
        };

        /// <summary>
        /// Chances (%) of Consistente, AcimaDaMedia, AbaixoDaMedia and Inconsistente for each kind of student
        /// </summary>
        private static readonly SkillProfile[] SkillProfiles =
        {
            new(60, 30, 8, 2),   // strong
            new(35, 40, 20, 5),  // good
            new(15, 35, 35, 15), // average
            new(5, 20, 40, 35)   // struggling
        };

        #endregion

        #region Types

        private record PathwayPlan(string Title, int SupervisorIndex, int[] ModuleIndexes, int StartOffsetDays, bool IsArchived, int[] StudentIndexes);

        private record ModuleBlueprint(string Title, string BadgeName, string BadgeIcon, BadgeTier BadgeTier, ComponentBlueprint[] Components);

        private record ComponentBlueprint(string Title, ModuleStage Stage, string? Description = null, ComponentBlueprint[]? Children = null);

        private record SkillProfile(int Consistente, int AcimaDaMedia, int AbaixoDaMedia, int Inconsistente)
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
