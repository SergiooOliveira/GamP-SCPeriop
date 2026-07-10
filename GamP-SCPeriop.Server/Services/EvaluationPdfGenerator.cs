using GamP_SCPeriop.Shared.Data;
using GamP_SCPeriop.Shared.Enum;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GamP_SCPeriop.Server.Services
{
    public class EvaluationPdfGenerator
    {
        private readonly List<Enrollment> _enrollments;

        public EvaluationPdfGenerator(List<Enrollment> enrollments)
        {
            _enrollments = enrollments;
        }

        public byte[] GeneratePdf()
        {
            var studentName = _enrollments.FirstOrDefault()?.Student?.DisplayShortName ?? "Aluno";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    // 1. CABEÇALHO GLOBAL
                    page.Header().PaddingBottom(10).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Relatório Global de Avaliação").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                            col.Item().Text($"Aluno: {studentName}").FontSize(14).FontColor(Colors.Grey.Darken3);
                        });
                        row.ConstantItem(100).AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy")).FontSize(10).FontColor(Colors.Grey.Medium);
                    });

                    // 2. CONTEÚDO
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                    {
                        foreach (var enrollment in _enrollments)
                        {
                            column.Item().PaddingBottom(20).Column(pathwayCol =>
                            {
                                // Título do Percurso
                                pathwayCol.Item().Background(Colors.Grey.Lighten3).Padding(8)
                                    .Text($"Percurso: {enrollment.Pathway?.Title} (Progresso Global: {CalculateGlobalProgress(enrollment)}%)")
                                    .FontSize(12).SemiBold().FontColor(Colors.Black);

                                if (enrollment.Pathway?.Modules != null)
                                {
                                    foreach (var module in enrollment.Pathway.Modules)
                                    {
                                        // Título do Módulo
                                        pathwayCol.Item().PaddingTop(15).PaddingBottom(5)
                                            .Text($"Módulo: {module.Title}").FontSize(12).SemiBold().FontColor(Colors.Blue.Darken2);

                                        if (module.Components != null)
                                        {
                                            // FILTRO: Apenas a fase de Avaliação Final (Prática Supervisionada)
                                            var assessableComponents = module.Components.Where(c => c.Stage == ModuleStage.PraticaSupervisionada).ToList();
                                            var topLevelItems = assessableComponents.Where(c => c.ParentComponentId == null).ToList();

                                            if (topLevelItems.Any())
                                            {
                                                // INÍCIO DA TABELA
                                                pathwayCol.Item().Table(table =>
                                                {
                                                    // Definição das Colunas
                                                    table.ColumnsDefinition(columns =>
                                                    {
                                                        columns.RelativeColumn();    // Coluna larga para as tarefas
                                                        columns.ConstantColumn(130); // Coluna fixa para a classificação
                                                    });

                                                    // Cabeçalho da Tabela
                                                    table.Header(header =>
                                                    {
                                                        header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5)
                                                              .Text("Parâmetro de Avaliação").SemiBold().FontSize(10).FontColor(Colors.Grey.Darken2);

                                                        header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5)
                                                              .AlignRight().Text("Classificação Final").SemiBold().FontSize(10).FontColor(Colors.Grey.Darken2);
                                                    });

                                                    // Preenchimento das Linhas
                                                    foreach (var item in topLevelItems)
                                                    {
                                                        var children = assessableComponents.Where(c => c.ParentComponentId == item.Id).ToList();

                                                        // Tarefa Pai
                                                        table.Cell().PaddingVertical(4).Text(item.Title).FontSize(10).SemiBold();

                                                        if (!children.Any())
                                                        {
                                                            table.Cell().PaddingVertical(4).AlignRight()
                                                                 .Text(GetStatusText(item.Status)).FontSize(10).SemiBold().FontColor(GetColorForStatus(item.Status));
                                                        }
                                                        else
                                                        {
                                                            table.Cell().PaddingVertical(4).Text(""); // Deixa vazio se tiver filhos
                                                        }

                                                        // Tarefas Filhas
                                                        foreach (var child in children)
                                                        {
                                                            table.Cell().PaddingVertical(3).PaddingLeft(15)
                                                                 .Text($"- {child.Title}").FontSize(9);

                                                            table.Cell().PaddingVertical(3).AlignRight()
                                                                 .Text(GetStatusText(child.Status)).FontSize(9).SemiBold().FontColor(GetColorForStatus(child.Status));
                                                        }
                                                    }
                                                });
                                            }
                                            else
                                            {
                                                pathwayCol.Item().PaddingTop(5).Text("Sem parâmetros avaliados nesta fase.").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                                            }
                                        }
                                    }
                                }
                            });
                        }
                    });

                    // 3. RODAPÉ
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        // Helper para pintar as avaliações no PDF
        private string GetColorForStatus(ComponentStatus status)
        {
            return status switch
            {
                ComponentStatus.Consistente => Colors.Green.Darken2,
                ComponentStatus.AcimaDaMedia => Colors.Orange.Darken2,
                ComponentStatus.AbaixoDaMedia => Colors.Orange.Darken2,
                ComponentStatus.Inconsistente => Colors.Red.Darken2,
                _ => Colors.Grey.Medium
            };
        }

        // Helper para formatar o texto corretamente (com espaços e em Português)
        private string GetStatusText(ComponentStatus status)
        {
            return status switch
            {
                ComponentStatus.Consistente => "Consistente",
                ComponentStatus.AcimaDaMedia => "Acima da Média",
                ComponentStatus.AbaixoDaMedia => "Abaixo da Média",
                ComponentStatus.Inconsistente => "Inconsistente",
                _ => "Pendente" // Substitui o "Pending"
            };
        }

        private int CalculateGlobalProgress(Enrollment enrollment)
        {
            if (enrollment?.Pathway?.Modules == null) return 0;

            int totalComponents = 0;
            int completedComponents = 0;

            foreach (var module in enrollment.Pathway.Modules)
            {
                if (module.Components == null) continue;

                var assessable = module.Components
                    .Where(c => c.Stage == ModuleStage.PraticaSupervisionada)
                    .Where(c => !module.Components.Any(child => child.ParentComponentId == c.Id))
                    .ToList();

                totalComponents += assessable.Count;
                completedComponents += assessable.Count(c =>
                    c.Status == ComponentStatus.AcimaDaMedia ||
                    c.Status == ComponentStatus.Consistente);
            }

            return totalComponents > 0 ? (int)((double)completedComponents / totalComponents * 100) : 0;
        }
    }
}