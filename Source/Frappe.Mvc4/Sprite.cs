using Frappe.Sprites;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace Frappe.Mvc
{
    public static class Sprite
    {
        /// <summary>
        /// Creates the proper CSS link reference within the target CSHTML page's head section
        /// </summary>
        /// <param name="virtualPath">The relative path of the image to be displayed, or its directory</param>
        /// <returns>Link tag if the file is found.</returns>
        public static IHtmlString ImportStylesheet(string virtualPath)
        {
            SpriteGenerator.Default.EnsureInitialized();

            if (Path.HasExtension(virtualPath))
            {
                virtualPath = Path.GetDirectoryName(virtualPath);
            }

            HttpContextBase httpContext = new HttpContextWrapper(HttpContext.Current);

            // Set up fileName and path variables
            string localPath = httpContext.Server.MapPath(virtualPath);

            string cssFileName = SpriteGenerator.Default.LinkCompatibleCssFileName(httpContext.Request.Browser, localPath);
            if (cssFileName == null)
                return null;

            virtualPath = Path.Combine(virtualPath, cssFileName);
            string physicalPath = HttpContext.Current.Server.MapPath(virtualPath);

            if (File.Exists(physicalPath))
            {
                TagBuilder htmlTag = new TagBuilder("link");
                htmlTag.MergeAttribute("href", ResolveUrl(virtualPath));
                htmlTag.MergeAttribute("rel", "stylesheet");
                htmlTag.MergeAttribute("type", "text/css");
                htmlTag.MergeAttribute("media", "all");
                return new HtmlString(htmlTag.ToString(TagRenderMode.SelfClosing));
            }

            return null;
        }

        /// <summary>
        /// Creates a reference to the sprite / inlined version of the desired image.
        /// </summary>
        /// <param name="virtualPath">The relative path of the image to be displayed</param>
        /// <returns>Image tag.</returns>
        public static IHtmlString Image(string virtualPath)
        {
            return Image(virtualPath, htmlAttributes: null);
        }

        /// <summary>
        /// Creates a reference to the sprite / inlined version of the desired image including special attributes.
        /// </summary>
        /// <param name="virtualPath">The relative path of the image to be displayed</param>
        /// <param name="htmlAttributes">Html Attributes of object form</param>
        /// <returns>Image tag.</returns>
        public static IHtmlString Image(string virtualPath, object htmlAttributes)
        {
            return Image(virtualPath, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        /// <summary>
        /// Creates a reference to the sprite / inlined version of the desired image including special attributes.
        /// </summary>
        /// <param name="virtualPath">The relative path of the image to be displayed</param>
        /// <param name="htmlAttributes">Html Attributes of IDictionary form</param>
        /// <returns>Image tag.</returns>
        public static IHtmlString Image(string virtualPath, IDictionary<string, object> htmlAttributes)
        {
            SpriteGenerator.Default.EnsureInitialized();

            TagBuilder htmlTag = new TagBuilder("img");
            htmlTag.MergeAttributes(htmlAttributes);

            HttpContextBase httpContext = new HttpContextWrapper(HttpContext.Current);

            string localSpriteDirectory = Path.GetDirectoryName(httpContext.Server.MapPath(virtualPath));
            if (SpriteGenerator.Default.LinkCompatibleCssFileName(httpContext.Request.Browser, localSpriteDirectory) == null)
            {
                htmlTag.MergeAttribute("src", ResolveUrl(virtualPath));
                return new HtmlString(htmlTag.ToString(TagRenderMode.SelfClosing));
            }
            else
            {
                htmlTag.AddCssClass(SpriteGenerator.Default.MakeCssClassName(virtualPath));
                htmlTag.MergeAttribute("src", ResolveUrl(SpriteGenerator.Default.GetBlankImageSource(httpContext.Request.Browser)));
                return new HtmlString(htmlTag.ToString(TagRenderMode.SelfClosing));
            }
        }
        
        private static string ResolveUrl(string path)
        {
            if (path.StartsWith("data:image", System.StringComparison.InvariantCultureIgnoreCase))
                return path;

            var httpContext = new HttpContextWrapper(HttpContext.Current);
            var urlHelper = new UrlHelper(httpContext.Request.RequestContext);
            var cdnHostName = Frappe.Settings.Default.CdnHostName;
            if (!string.IsNullOrWhiteSpace(cdnHostName))
            {
                var url = new System.Text.StringBuilder();
                if (Frappe.Settings.Default.ProtocolRelativeUrl)
                {
                    url.Append("//");
                }
                else if (HttpContext.Current.Request.IsSecureConnection
                    || Frappe.Settings.Default.ForceSecureUrl)
                {
                    url.Append("https://");
                }
                else
                {
                    url.Append("http://");
                }
                url.Append(cdnHostName);

                // determine whether we should prepend the cdn path prefix to the url
                var cdnPathPrefixTrimmed = Settings.Default.CdnPathPrefix?.Trim()?.Trim('/');
                if (!string.IsNullOrWhiteSpace(cdnPathPrefixTrimmed))
                {
                    url.Append("/").Append(cdnPathPrefixTrimmed);
                }

                url.Append(urlHelper.Content(path));
                return url.ToString();
            }
            else
            {
                return urlHelper.Content(path);
            }
        }
    }
}
