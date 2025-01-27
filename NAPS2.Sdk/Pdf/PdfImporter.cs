﻿using NAPS2.ImportExport;
using NAPS2.Pdf.Pdfium;
using NAPS2.Scan;

namespace NAPS2.Pdf;

/// <summary>
/// Imports PDF files.
/// </summary>
public class PdfImporter
{
    private const int MAX_PASSWORD_ATTEMPTS = 5;

    private readonly ScanningContext _scanningContext;
    private readonly IPdfPasswordProvider? _pdfPasswordProvider;

    public PdfImporter(ScanningContext scanningContext, IPdfPasswordProvider? pdfPasswordProvider = null)
    {
        _scanningContext = scanningContext;
        _pdfPasswordProvider = pdfPasswordProvider;
    }

    public IAsyncEnumerable<ProcessedImage> Import(string filePath, ImportParams? importParams = null,
        ProgressHandler progress = default) => Import(new InputPathOrStream(filePath, null, null), importParams, progress);

    public IAsyncEnumerable<ProcessedImage> Import(Stream stream, ImportParams? importParams = null,
        ProgressHandler progress = default) => Import(new InputPathOrStream(null, stream, null), importParams, progress);

    internal IAsyncEnumerable<ProcessedImage> Import(InputPathOrStream input, ImportParams? importParams = null,
        ProgressHandler progress = default)
    {
        importParams ??= new ImportParams();
        return AsyncProducers.RunProducer<ProcessedImage>(produceImage =>
        {
            if (progress.IsCancellationRequested) return;

            lock (PdfiumNativeLibrary.Instance)
            {
                using var document = LoadDocument(input, importParams);
                if (document == null) return;
                progress.Report(0, document.PageCount);

                // TODO: Maybe do a permissions check

                // TODO: Make sure to test slices (both unit and command line)
                using var pages = importParams.Slice
                    .Indices(document.PageCount)
                    .Select(index => document.GetPage(index))
                    .ToDisposableList();

                int i = 0;
                foreach (var page in pages.InnerList)
                {
                    if (progress.IsCancellationRequested) return;
                    var image = GetImageFromPage(page, importParams);
                    progress.Report(++i, document.PageCount);
                    produceImage(image);
                }
            }
        });
    }

    private PdfDocument? LoadDocument(InputPathOrStream input, ImportParams importParams)
    {
        PdfDocument? doc = null;
        try
        {
            var password = importParams.Password;
            var passwordAttempts = 0;
            while (passwordAttempts < MAX_PASSWORD_ATTEMPTS)
            {
                try
                {
                    doc = input.LoadPdfDoc(password);
                    break;
                }
                catch (PdfiumException ex) when (ex.ErrorCode == PdfiumErrorCode.PasswordNeeded &&
                                                 _pdfPasswordProvider != null)
                {
                    if (!_pdfPasswordProvider.ProvidePassword(input.FileName, passwordAttempts++, out password))
                    {
                        return null;
                    }
                }
                catch (PdfiumException ex) when (ex.ErrorCode == PdfiumErrorCode.FileNotFoundOrUnavailable)
                {
                    if (input.FilePath != null && !File.Exists(input.FilePath))
                    {
                        throw new FileNotFoundException($"Could not find pdf file: '{input.FilePath}'");
                    }
                    if (input.FilePath != null)
                    {
                        throw new IOException($"Could not open pdf file for reading: '{input.FilePath}'");
                    }
                    throw new IOException("Could not open pdf file for reading");
                }
            }
            return doc;
        }
        catch (Exception)
        {
            doc?.Dispose();
            throw;
        }
    }

    private ProcessedImage GetImageFromPage(PdfPage page, ImportParams importParams)
    {
        using var storage = PdfiumImageExtractor.GetSingleImage(_scanningContext.ImageContext, page, false);
        if (storage != null)
        {
            var pageSize = new PageSize((decimal) page.Width * 72, (decimal) page.Height * 72, PageSizeUnit.Inch);
            var image = _scanningContext.CreateProcessedImage(storage, false, -1, pageSize);
            return ImportPostProcessor.AddPostProcessingData(
                image,
                storage,
                importParams.ThumbnailSize,
                importParams.BarcodeDetectionOptions,
                true);
        }

        return ExportRawPdfPage(page, importParams);
    }

    private ProcessedImage ExportRawPdfPage(PdfPage page, ImportParams importParams)
    {
        IImageStorage storage;
        using var document = PdfDocument.CreateNew();
        document.ImportPage(page);
        if (_scanningContext.FileStorageManager != null)
        {
            string pdfPath = _scanningContext.FileStorageManager.NextFilePath() + ".pdf";
            document.Save(pdfPath);
            storage = new ImageFileStorage(pdfPath);
        }
        else
        {
            var stream = new MemoryStream();
            document.Save(stream);
            storage = new ImageMemoryStorage(stream, ".pdf");
        }

        var image = _scanningContext.CreateProcessedImage(storage);
        return ImportPostProcessor.AddPostProcessingData(
            image,
            null,
            importParams.ThumbnailSize,
            importParams.BarcodeDetectionOptions,
            true);
    }
}