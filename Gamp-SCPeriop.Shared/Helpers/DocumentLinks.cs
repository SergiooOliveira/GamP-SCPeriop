using System.Text.RegularExpressions;

namespace GamP_SCPeriop.Shared.Helpers
{
    /// <summary>
    /// A component's "document" (PdfFilePath) is either a file uploaded to our server or an external http(s) link.
    /// Anything else (javascript:, data:, relative paths...) is rejected, so it can never run script when clicked.
    /// </summary>
    public static partial class DocumentLinks
    {
        public const string UploadPrefix = "api/files/download/";
        public const long MaxUploadBytes = 10 * 1024 * 1024; // 10 MB

        /// <summary>
        /// Allowed upload types: extension -> (content type, whether the browser may show it inline)
        /// </summary>
        public static readonly IReadOnlyDictionary<string, (string ContentType, bool Inline)> AllowedTypes =
            new Dictionary<string, (string, bool)>(StringComparer.OrdinalIgnoreCase)
            {
                [".pdf"] = ("application/pdf", true),
                [".docx"] = ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", false),
                [".doc"] = ("application/msword", false)
            };

        // Uploaded files are always saved as "<guid>.<ext>"
        [GeneratedRegex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\.(pdf|docx?)$", RegexOptions.IgnoreCase)]
        private static partial Regex StoredFileNameRegex();

        public static bool IsStoredFileName(string? fileName) =>
            !string.IsNullOrEmpty(fileName) && StoredFileNameRegex().IsMatch(fileName);

        public static bool IsUploadedFile(string? link) =>
            link != null
            && link.StartsWith(UploadPrefix, StringComparison.OrdinalIgnoreCase)
            && IsStoredFileName(link[UploadPrefix.Length..]);

        public static bool IsExternalUrl(string? link) =>
            Uri.TryCreate(link, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

        /// <summary>
        /// Empty (no document), one of our uploads, or an http(s) link
        /// </summary>
        public static bool IsAllowed(string? link) =>
            string.IsNullOrWhiteSpace(link) || IsUploadedFile(link.Trim()) || IsExternalUrl(link.Trim());

        public static string GetFileName(string link) => link[UploadPrefix.Length..];
    }
}
