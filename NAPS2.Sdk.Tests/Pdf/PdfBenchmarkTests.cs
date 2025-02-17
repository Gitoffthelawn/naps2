using NAPS2.Pdf;
using Xunit;

namespace NAPS2.Sdk.Tests.Pdf;

public class PdfBenchmarkTests : ContextualTests
{
    [BenchmarkFact]
    public async Task PdfSharpExport300()
    {
        var filePath = Path.Combine(FolderPath, "test");
        using var image = ScanningContext.CreateProcessedImage(LoadImage(ImageResources.dog));

        var pdfExporter = new PdfExporter(ScanningContext);
        for (int i = 0; i < 300; i++)
        {
            await pdfExporter.Export(filePath + i + ".pdf", [image]);
        }
    }

    [BenchmarkFact]
    public async Task PdfSharpExportHuge()
    {
        var filePath = Path.Combine(FolderPath, "test");
        using var image = ScanningContext.CreateProcessedImage(LoadImage(ImageResources.dog_huge));

        var pdfExporter = new PdfExporter(ScanningContext);
        await pdfExporter.Export(filePath + ".pdf", [image]);
    }

    [BenchmarkFact]
    public async Task PdfSharpExportHugePng()
    {
        var filePath = Path.Combine(FolderPath, "test");
        using var image = ScanningContext.CreateProcessedImage(LoadImage(ImageResources.dog_huge_png));

        var pdfExporter = new PdfExporter(ScanningContext);
        await pdfExporter.Export(filePath + ".pdf", [image]);
    }

    [BenchmarkFact]
    public async Task PdfiumExport300()
    {
        var filePath = Path.Combine(FolderPath, "test");
        using var image = ScanningContext.CreateProcessedImage(LoadImage(ImageResources.dog));

        var pdfExporter = new PdfiumPdfExporter(ScanningContext);
        for (int i = 0; i < 300; i++)
        {
            await pdfExporter.Export(filePath + i + ".pdf", [image]);
        }
    }

    [BenchmarkFact]
    public async Task PdfiumExportHuge()
    {
        var filePath = Path.Combine(FolderPath, "test");
        using var image = ScanningContext.CreateProcessedImage(LoadImage(ImageResources.dog_huge));

        var pdfExporter = new PdfiumPdfExporter(ScanningContext);
        await pdfExporter.Export(filePath + ".pdf", [image]);
    }

    [BenchmarkFact]
    public async Task PdfiumExportHugePng()
    {
        var filePath = Path.Combine(FolderPath, "test");
        using var image = ScanningContext.CreateProcessedImage(LoadImage(ImageResources.dog_huge_png));

        var pdfExporter = new PdfiumPdfExporter(ScanningContext);
        await pdfExporter.Export(filePath + ".pdf", [image]);
    }

    [BenchmarkFact]
    public async Task Import300Naps2()
    {
        SetUpFileStorage();
        var filePath = CopyResourceToFile(PdfResources.image_pdf, "test.pdf");

        var pdfExporter = new PdfImporter(ScanningContext);
        for (int i = 0; i < 300; i++)
        {
            await pdfExporter.Import(filePath).ToListAsync();
        }
    }

    [BenchmarkFact]
    public async Task Import300Naps2Bw()
    {
        SetUpFileStorage();
        var filePath = CopyResourceToFile(PdfResources.image_pdf_bw, "test.pdf");

        var pdfExporter = new PdfImporter(ScanningContext);
        for (int i = 0; i < 300; i++)
        {
            await pdfExporter.Import(filePath).ToListAsync();
        }
    }

    [BenchmarkFact]
    public async Task Import300NonNaps2()
    {
        SetUpFileStorage();
        var filePath = CopyResourceToFile(PdfResources.word_generated_pdf, "test.pdf");

        var pdfExporter = new PdfImporter(ScanningContext);
        for (int i = 0; i < 300; i++)
        {
            await pdfExporter.Import(filePath).ToListAsync();
        }
    }

    public class BenchmarkFact : FactAttribute
    {
        public BenchmarkFact()
        {
            Skip = "comment out this line to run benchmarks";
        }
    }
}