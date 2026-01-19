namespace MyThuatShop.Helpers
{
    public static class ImageUrlHelper
    {
        public static string ToAbsolute(string? url, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(url)) return "";

            url = url.Trim();

            // absolute url
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return url;

            if (!url.StartsWith("/")) url = "/" + url;

            return baseUrl.TrimEnd('/') + url;
        }
    }
}
