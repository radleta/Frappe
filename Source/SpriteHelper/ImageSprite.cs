using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace Microsoft.Web.Samples {
    public static class Sprite {

        /// <summary>
        /// The host name for the cdn the content is served from within.
        /// </summary>
        public static string CdnHostName { get; set; }

        /// <summary>
        /// Determines whether or not to use protocol relative url when rendering the sprites.
        /// </summary>
        public static bool ProtocolRelativeUrl { get; set; }

        /// <summary>
        /// Determines whether or not to force secure url when rendering sprites.
        /// </summary>
        public static bool ForceSecureUrl { get; set; }

        /// <summary>
        /// Creates the proper CSS link reference within the target CSHTML page's head section
        /// </summary>
        /// <param name="virtualPath">The relative path of the image to be displayed, or its directory</param>
        /// <returns>Link tag if the file is found.</returns>
        public static IHtmlString ImportStylesheet(string virtualPath) {
            ImageOptimizations.EnsureInitialized();

            if (Path.HasExtension(virtualPath)) {
                virtualPath = Path.GetDirectoryName(virtualPath);
            }

            HttpContextBase httpContext = new HttpContextWrapper(HttpContext.Current);

            // Set up fileName and path variables
            string localPath = httpContext.Server.MapPath(virtualPath);

            string cssFileName = ImageOptimizations.LinkCompatibleCssFileName(httpContext.Request.Browser, localPath);
            if (cssFileName == null)
                return null;

            virtualPath = Path.Combine(virtualPath, cssFileName);
            string physicalPath = HttpContext.Current.Server.MapPath(virtualPath);

            if (File.Exists(physicalPath)) {
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
        public static IHtmlString Image(string virtualPath) {
            return Image(virtualPath, htmlAttributes: null);
        }

        /// <summary>
        /// Creates a reference to the sprite / inlined version of the desired image including special attributes.
        /// </summary>
        /// <param name="virtualPath">The relative path of the image to be displayed</param>
        /// <param name="htmlAttributes">Html Attributes of object form</param>
        /// <returns>Image tag.</returns>
        public static IHtmlString Image(string virtualPath, object htmlAttributes) {
            return Image(virtualPath, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        /// <summary>
        /// Creates a reference to the sprite / inlined version of the desired image including special attributes.
        /// </summary>
        /// <param name="virtualPath">The relative path of the image to be displayed</param>
        /// <param name="htmlAttributes">Html Attributes of IDictionary form</param>
        /// <returns>Image tag.</returns>
        public static IHtmlString Image(string virtualPath, IDictionary<string, object> htmlAttributes) {
            ImageOptimizations.EnsureInitialized();

            TagBuilder htmlTag = new TagBuilder("img");
            htmlTag.MergeAttributes(htmlAttributes);

            HttpContextBase httpContext = new HttpContextWrapper(HttpContext.Current);

            string localSpriteDirectory = Path.GetDirectoryName(httpContext.Server.MapPath(virtualPath));
            if (ImageOptimizations.LinkCompatibleCssFileName(httpContext.Request.Browser, localSpriteDirectory) == null)
            {
                htmlTag.MergeAttribute("src", ResolveUrl(virtualPath));
                return new HtmlString(htmlTag.ToString(TagRenderMode.SelfClosing));
            }
            else 
            {
                htmlTag.AddCssClass(ImageOptimizations.MakeCssClassName(virtualPath));
                htmlTag.MergeAttribute("src", ResolveUrl(ImageOptimizations.GetBlankImageSource(httpContext.Request.Browser)));
                return new HtmlString(htmlTag.ToString(TagRenderMode.SelfClosing));
            }
        }

        // REVIEW: Taken from Util.Url in Microsoft.WebPages, is this the best way to do this.
        private static string ResolveUrl(string path) {
            if (path.StartsWith("data:image", System.StringComparison.InvariantCultureIgnoreCase))
                return path;

            var httpContext = new HttpContextWrapper(HttpContext.Current);
            var urlHelper = new UrlHelper(httpContext.Request.RequestContext);
            var cdnHostName = CdnHostName;
            if (!string.IsNullOrWhiteSpace(cdnHostName))
            {
                var url = new System.Text.StringBuilder();
                if (ProtocolRelativeUrl)
                {
                    url.Append("//");
                }
                else if (HttpContext.Current.Request.IsSecureConnection
                    || ForceSecureUrl)
                {
                    url.Append("https://");
                }
                else
                {
                    url.Append("http://");
                }
                url.Append(cdnHostName);
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