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

                    // 2. CONTEÚDO (Iterar sobre cada Percurso)
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                    {
                        foreach (var enrollment in _enrollments)
                        {
                            // Bloco do Percurso (Pathway)
                            column.Item().PaddingBottom(15).Column(pathwayCol =>
                            {
                                // Título do Percurso
                                pathwayCol.Item().Background(Colors.Grey.Lighten3).Padding(5)
                                    .Text($"Percurso: {enrollment.Pathway?.Title} (Progresso: {enrollment.ProgressPercentage}%)")
                                    .FontSize(14).SemiBold();

                                if (enrollment.Pathway?.Modules != null)
                                {
                                    foreach (var module in enrollment.Pathway.Modules)
                                    {
                                        // Título do Módulo
                                        pathwayCol.Item().PaddingTop(10).PaddingLeft(10)
                                            .Text($"Módulo: {module.Title}").FontSize(12).SemiBold().FontColor(Colors.Blue.Darken2);

                                        if (module.Components != null)
                                        {
                                            var assessableComponents = module.Components.Where(c => c.Stage != ModuleStage.Teorica).ToList();
                                            var topLevelItems = assessableComponents.Where(c => c.ParentComponentId == null).ToList();

                                            foreach (var item in topLevelItems)
                                            {
                                                var children = assessableComponents.Where(c => c.ParentComponentId == item.Id).ToList();

                                                // Parâmetro Pai
                                                pathwayCol.Item().PaddingTop(5).PaddingLeft(20).Row(row =>
                                                {
                                                    row.RelativeItem().Text(item.Title).FontSize(11).SemiBold();
                                                    // Só mostra o estado se não tiver filhos
                                                    if (!children.Any())
                                                        row.ConstantItem(120).AlignRight().Text(item.Status.ToString()).FontSize(10).FontColor(GetColorForStatus(item.Status));
                                                });

                                                // Sub-parâmetros (Filhos Indentados)
                                                foreach (var child in children)
                                                {
                                                    pathwayCol.Item().PaddingTop(2).PaddingLeft(35).Row(row =>
                                                    {
                                                        row.RelativeItem().Text($"- {child.Title}").FontSize(10);
                                                        row.ConstantItem(120).AlignRight().Text(child.Status.ToString()).FontSize(10).FontColor(GetColorForStatus(child.Status));
                                                    });
                                                }
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
    }
}