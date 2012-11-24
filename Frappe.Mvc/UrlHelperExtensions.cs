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
            return Bundle(helper, bundle, bundleOutputUrl => LinkCss(helper, bundleOutputUrl));
        }

        /// <summary>
        /// Emits the html for the JavaScript bundle. Should be emitted within the Head of an Html document.
        /// </summary>
        /// <param name="helper">The helper to extend.</param>
        /// <param name="bundle">The virtual path to the bundle file to emit.</param>
        /// <returns>The html for the JavaScript bundle.</returns>
        public static IHtmlString JavaScriptBundle(this HtmlHelper helper, string bundle)
        {
            return Bundle(helper, bundle, bundleOutputUrl => ScriptJavaScript(helper, bundleOutputUrl));
        }

        /// <summary>
        /// Matches the extension of the bundle.
        /// </summary>
        private static Regex BundleFileRegex = new Regex(@"(?'Name'.+?)(?'TypeExt'\.(?:css|js))(?:\.bundle)$", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
        private static IHtmlString Bundle(this HtmlHelper helper, string bundle, Func<string, IHtmlString> getIHtmlString)
        {
            var context = helper.ViewContext.HttpContext;
            var isSecureRequest = context.Request.IsSecureConnection;
                
            if (Settings.Default.BundleOutput)
            {
                var cache = context.Cache;
                var key = string.Format("Frappe.Mvc_{0}_{1}_{2}", isSecureRequest, Settings.Default.CdnHostName, bundle);
                var result = (IHtmlString)cache[key];
                if (result == null)
                {
                    var server = helper.ViewContext.HttpContext.Server;

                    // ensure the bundle exists
                    var bundleFile = server.MapPath(bundle);
                    if (!File.Exists(bundleFile))
                    {
                        throw new System.IO.FileNotFoundException("The bundle file could not be found. The bundle file must exist.", bundleFile ?? bundle);
                    }

                    // get the output file
                    var bundleOutput = BundleFileRegex.Replace(bundle, @"${Name}.min${TypeExt}");
                    var bundleOutputFile = server.MapPath(bundleOutput);

                    if (!File.Exists(bundleOutputFile))
                    {
                        throw new System.IO.FileNotFoundException(string.Format("The output file could not be found. The output file for the bundle must exist. Bundle: {0}", bundleFile), bundleOutputFile);
                    }

                    // create the url to the bundle output file

                    string bundleOutputUrl = string.Empty;

                    // determine whether or not to append the cdn host name
                    if (!string.IsNullOrWhiteSpace(Settings.Default.CdnHostName))
                    {
                        if (isSecureRequest)
                        {
                            bundleOutputUrl += "https://";
                        }
                        else
                        {
                            bundleOutputUrl += "http://";
                        }

                        bundleOutputUrl += Settings.Default.CdnHostName;
                    }
                    
                    var urlHelper = new UrlHelper(helper.ViewContext.RequestContext);
                    bundleOutputUrl += urlHelper.Content(bundleOutput + "?v=" + File.GetLastWriteTimeUtc(bundleOutputFile).ToString("yyyyMMddHHmmssfff"));                    

                    // create the final html
                    result = getIHtmlString(bundleOutputUrl);

                    // cache it absolutely so we dont waste time re-doing this multiple times
                    cache.Add(key,
                        result,
                        new System.Web.Caching.CacheDependency(bundleOutputFile),
                        DateTime.Now.AddMinutes(15),
                        System.Web.Caching.Cache.NoSlidingExpiration,
                        System.Web.Caching.CacheItemPriority.Default,
                        null);
                }
                return result;
            }
            else
            {
                var server = context.Server;
                
                // ensure the bundle exists
                var bundleFile = server.MapPath(bundle);
                if (!File.Exists(bundleFile))
                {
                    throw new System.IO.FileNotFoundException("The bundle file could not be found. The bundle file must exist.", bundleFile);
                }

                var bundler = new Bundler();
                var urlHelper = new UrlHelper(helper.ViewContext.RequestContext);
                var webRootDir = server.MapPath("~/").ToLower();
                var webRootUrl = urlHelper.Content("~/");
                return new MvcHtmlString(string.Concat(bundler.GetFiles(bundleFile).Select(includeFile =>
                {
                    if (!File.Exists(includeFile))
                    {
                        throw new System.IO.FileNotFoundException(string.Format("A include file could not be found. All the files in the bundle must exist. Bundle: {0}", bundleFile), includeFile);
                    }
                                        
                    // create the url to the include file

                    string includeUrl;

                    // determine whether or not to append the cdn host name
                    if (!string.IsNullOrWhiteSpace(Settings.Default.CdnHostName))
                    {
                        if (isSecureRequest)
                        {
                            includeUrl = "https://";
                        }
                        else
                        {
                            includeUrl = "http://";
                        }

                        includeUrl += Settings.Default.CdnHostName;
                    }
                    else
                    {
                        includeUrl = string.Empty;
                    }

                    includeUrl += webRootUrl + includeFile.ToLower().Replace(webRootDir, "").Replace("\\", "/") + "?v=" + File.GetLastWriteTimeUtc(includeFile).ToString("yyyyMMddHHmmssfff");

                    return getIHtmlString(includeUrl).ToHtmlString();
                })));
            }     
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
