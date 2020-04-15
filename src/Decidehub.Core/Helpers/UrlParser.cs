namespace Decidehub.Core.Helpers
{
    public static class UrlParser
    {
        public static string GetSubDomain(string url)
        {
            var firstDot = url.IndexOf('.');
            var lastDot = url.LastIndexOf('.');
            if (firstDot < 0 || firstDot == lastDot)
                return "";

            return url.Substring(0, firstDot);
        }
    }
}
