namespace GamP_SCPeriop.Helpers
{
    /// <summary>
    /// The test dashboard lives on its own host (https://dashboard.dev.localhost:7038) next to the app (https://localhost:7038).
    /// *.dev.localhost is covered by the .NET development HTTPS certificate and resolves to this machine.
    /// </summary>
    public static class DevDashboardUrls
    {
        private const string DashboardPrefix = "dashboard.";

        public static bool IsDashboardHost(string baseUri) =>
            new Uri(baseUri).Host.StartsWith(DashboardPrefix, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// https://localhost:7038/ -> https://dashboard.dev.localhost:7038/
        /// </summary>
        public static string GetDashboardUrl(string baseUri)
        {
            var uri = new UriBuilder(baseUri);
            uri.Host = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                ? $"{DashboardPrefix}dev.localhost"
                : DashboardPrefix + uri.Host;
            return uri.Uri.ToString();
        }

        /// <summary>
        /// https://dashboard.dev.localhost:7038/ -> https://localhost:7038/
        /// </summary>
        public static string GetAppUrl(string baseUri)
        {
            var uri = new UriBuilder(baseUri);
            var host = uri.Host.Substring(DashboardPrefix.Length);
            uri.Host = host.EndsWith("localhost", StringComparison.OrdinalIgnoreCase) ? "localhost" : host;
            return uri.Uri.ToString();
        }
    }
}
