using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Exceptions;

namespace CareerSyncAI.Services;

public sealed class CvTextResult
{
    public string Text { get; private init; } = string.Empty;
    public string? Error { get; private init; }
    public bool Success => Error is null;

    public static CvTextResult Ok(string text) => new() { Text = text };
    public static CvTextResult Fail(string error) => new() { Error = error };
}


public interface ICvTextExtractor
{
    Task<CvTextResult> ExtractAsync(IFormFile file, CancellationToken ct = default);
    CvTextResult Extract(string fileName, byte[] content);
}

public class CvTextExtractor : ICvTextExtractor
{
    public const long MaxFileBytes = 5 * 1024 * 1024;   
    private const int MaxPdfPages = 20;                  
    private const int MinCharacters = 40;                

    private static readonly Regex Spaces = new(@"[ \t]+", RegexOptions.Compiled);
    private static readonly Regex BlankLines = new(@"\n{3,}", RegexOptions.Compiled);

    private readonly ILogger<CvTextExtractor> _logger;

    public CvTextExtractor(ILogger<CvTextExtractor> logger)
    {
        _logger = logger;
    }

    public async Task<CvTextResult> ExtractAsync(IFormFile file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0) return CvTextResult.Fail("The file you chose is empty.");
        if (file.Length > MaxFileBytes) return CvTextResult.Fail("That file is larger than 5 MB. Please upload a smaller CV.");

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, ct);
        return Extract(file.FileName, buffer.ToArray());
    }

    public CvTextResult Extract(string fileName, byte[] content)
    {
        if (content.Length == 0) return CvTextResult.Fail("The file you chose is empty.");
        if (content.Length > MaxFileBytes) return CvTextResult.Fail("That file is larger than 5 MB. Please upload a smaller CV.");

        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        string text;

        switch (extension)
        {
            case ".pdf":
                if (!LooksLikePdf(content))
                    return CvTextResult.Fail("This file isn't a valid PDF. Try exporting your CV to PDF again, or paste its text below.");

                var pdf = ReadPdf(fileName!, content);
                if (!pdf.Success) return pdf;
                text = pdf.Text;
                break;

            case ".txt":
                text = ReadPlainText(content);
                break;

            default:
                return CvTextResult.Fail("Please upload your CV as a PDF (.pdf) or text (.txt) file, or paste its text below.");
        }

        text = Clean(text);

        if (text.Length < MinCharacters)
        {
            return extension == ".pdf"
                ? CvTextResult.Fail("We couldn't find any text in this PDF. It may be a scanned image. Upload a PDF saved directly from Word or Google Docs, or paste your CV text below.")
                : CvTextResult.Fail("This file doesn't contain enough text to analyse. Check it's your CV, or paste the text below.");
        }

        return CvTextResult.Ok(text);
    }

    private CvTextResult ReadPdf(string fileName, byte[] content)
    {
        try
        {
            using var document = PdfDocument.Open(content);
            if (document.NumberOfPages > MaxPdfPages)
                return CvTextResult.Fail($"That PDF has {document.NumberOfPages} pages. Please upload just your CV (up to {MaxPdfPages} pages).");

            var sb = new StringBuilder();
            foreach (var page in document.GetPages())
            {

                double? previousBottom = null;
                foreach (var word in page.GetWords())
                {
                    var bottom = word.BoundingBox.Bottom;
                    var lineHeight = Math.Max(word.BoundingBox.Height, 2);
                    if (previousBottom is double prev)
                        sb.Append(Math.Abs(bottom - prev) > lineHeight * 0.5 ? '\n' : ' ');
                    sb.Append(word.Text);
                    previousBottom = bottom;
                }
                sb.Append("\n\n");
            }
            return CvTextResult.Ok(sb.ToString());
        }
        catch (PdfDocumentEncryptedException)
        {
            return CvTextResult.Fail("This PDF is password-protected. Remove the password (or save an unprotected copy) and upload it again.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read PDF CV {FileName}", fileName);
            return CvTextResult.Fail("We couldn't read this PDF. Try saving your CV as a PDF again, or paste its text below.");
        }
    }

    private static string ReadPlainText(byte[] content)
    {
        using var reader = new StreamReader(new MemoryStream(content), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static bool LooksLikePdf(byte[] content)
    {
        var head = Encoding.ASCII.GetString(content, 0, Math.Min(content.Length, 1024));
        return head.Contains("%PDF-", StringComparison.Ordinal);
    }

    private static string Clean(string text)
    {
        text = text.Normalize(NormalizationForm.FormKC).Replace("\r\n", "\n").Replace('\r', '\n').Replace('\0', ' ');
        var lines = text.Split('\n').Select(l => Spaces.Replace(l, " ").Trim());
        return BlankLines.Replace(string.Join("\n", lines), "\n\n").Trim();
    }
}