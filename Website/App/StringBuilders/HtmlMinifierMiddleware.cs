using System.Text.RegularExpressions;
using WebMarkupMin.Core;

namespace Website.App.StringBuilders
{
    public class HtmlMinifyMiddleware
    {
        private readonly RequestDelegate _next;

        public HtmlMinifyMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            var originalBody = context.Response.Body;

            using var newBody = new MemoryStream();
            context.Response.Body = newBody;

            await _next(context); // render the Razor page

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            string content = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            string contentType = context.Response.ContentType ?? string.Empty;

            // Only minify HTML content
            if (contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                string minifiedHtml = MinifyHtml(content);
                context.Response.Body = originalBody;
                await context.Response.WriteAsync(minifiedHtml);
            }
            else
            {
                // Non-HTML (JSON, images, etc.) -> write raw
                context.Response.Body = originalBody;
                await context.Response.WriteAsync(content);
            }
        }
        public static string MinifyHtml(string html)
        {
            HtmlMinifier minifier = new(new HtmlMinificationSettings
            {
                RemoveRedundantAttributes = true,
                RemoveHttpProtocolFromAttributes = true,
                RemoveHttpsProtocolFromAttributes = true,
                RemoveEmptyAttributes = true
            });

            MarkupMinificationResult result = minifier.Minify(html);
            string newHTML = result.MinifiedContent;

            if (string.IsNullOrWhiteSpace(newHTML))
                return string.Empty;

            // Remove HTML comments (but keep conditional ones)
            newHTML = Regex.Replace(newHTML, @"<!--(?!\[if).*?-->", string.Empty, RegexOptions.Singleline);

            // Collapse all whitespace (spaces, tabs, newlines) into a single space
            newHTML = Regex.Replace(newHTML, @"\s{2,}", " ");

            // Remove whitespace between tags
            newHTML = Regex.Replace(newHTML, @">\s+<", "><");

            // Trim any leading/trailing whitespace
            return newHTML.Trim();
        }
    }
}
