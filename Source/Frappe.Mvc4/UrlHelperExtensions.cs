using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Text.RegularExpressions;

namespace Frappe.Mvc
{
    /// <summary>
    /// The extensions for the <see cref="HtmlHelper"/>.
    /// </summary>
    public static class HtmlHelperExtensions
    {
        /// <summary>
        /// Emits the html for the css bundle. Should be emitted within the Head of an Html document.
        /// </summary>
        /// <param name="helper">The helper to extend.</param>
        /// <param name="bundle">The virtual path to the bundle file to emit.</param>
        /// <returns>The html for the css bundle.</returns>
        public static IHtmlString CssBundle(this HtmlHelper helper, string bundle)
        {
            return GenericBundle(helper, bundle, bundleOutputUrl => LinkCss(helper, bundleOutputUrl));
        }

        /// <summary>
        /// Emits the html for the JavaScript bundle. Should be emitted within the Head of an Html document.
        /// </summary>
        /// <param name="helper">The helper to extend.</param>
        /// <param name="bundle">The virtual path to the bundle file to emit.</param>
        /// <returns>The html for the JavaScript bundle.</returns>
        public static IHtmlString JavaScriptBundle(this HtmlHelper helper, string bundle)
        {
            return GenericBundle(helper, bundle, bundleOutputUrl => ScriptJavaScript(helper, bundleOutputUrl));
        }

        /// <summary>
        /// Emits the html for the bundle.
        /// </summary>
        /// <param name="helper">The helper to extend.</param>
        /// <param name="bundle">The virtual path to the bundle.</param>
        /// <param name="getIHtmlString">The method to create the html for the bundle.</param>
        /// <returns></returns>
        /// <remarks>
        /// Generic method for now since the process of generating a bundle
        /// for either css or js is almost the same.
        /// </remarks>
        private static IHtmlString GenericBundle(this HtmlHelper helper, string bundle, Func<string, IHtmlString> getIHtmlString)
        {
            var context = helper.ViewContext.HttpContext;
            var isSecureRequest = context.Request.IsSecureConnection;
            var cache = context.Cache;
            var key = string.Format("Frappe.Mvc_{0}_{1}_{2}_{3}_{4}", isSecureRequest, Settings.Default.BundleOutput, Settings.Default.CdnHostName, bundle, context.Request.Url.Host);
            var result = (IHtmlString)cache[key];
            if (result == null)
            {
                var htmls = Frappe.Bundle.GetUrls(bundle, helper.ViewContext.HttpContext)
                    .Select(url => getIHtmlString(url));
                result = new MvcHtmlString(string.Concat(htmls));
                
                // cache it absolutely so we dont waste time re-doing this multiple times
                cache.Add(key,
                    result,
                    new System.Web.Caching.CacheDependency(context.Server.MapPath(bundle)),
                    DateTime.Now.AddMinutes(15),
                    System.Web.Caching.Cache.NoSlidingExpiration,
                    System.Web.Caching.CacheItemPriority.Default,
                    null);
            }
            return result;
        }

        /// <summary>
        /// Emits a link element for a css stylesheet.
        /// </summary>
        /// <param name="helper">The helper to extend.</param>
        /// <param name="href">The url to the css stylesheet.</param>
        /// <returns>The link element.</returns>
        private static IHtmlString LinkCss(this HtmlHelper helper, string href)
        {
            return new MvcHtmlString(string.Format("<link rel=\"stylesheet\" type=\"text/css\" href=\"{0}\" />\r\n", helper.Encode(href)));
        }

        /// <summary>
        /// Emits a script element for a JavaScript file.
        /// </summary>
        /// <param name="helper">The helper to extend.</param>
        /// <param name="href">The url to the JavaScript file.</param>
        /// <returns>The script element.</returns>
        private static IHtmlString ScriptJavaScript(this HtmlHelper helper, string href)
        {
            return new MvcHtmlString(string.Format("<script type=\"text/javascript\" src=\"{0}\"></script>\r\n", helper.Encode(href)));
        }
    }
}
