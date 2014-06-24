using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Frappe
{
    static class FileExtension
    {
        /// <summary>
        /// Matches a minified file. Ends with *.min.*.
        /// </summary>
        private static readonly Regex MinifiedFileRegex = new Regex(@"\.min\.[^\.]+$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// Matches a css file. Ends with *.css.
        /// </summary>
        private static readonly Regex CssFileRegex = new Regex(@"\.css$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// Matches a js file. Ends with *.js.
        /// </summary>
        private static readonly Regex JavaScriptFileRegex = new Regex(@"\.js$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// Matches a less file. Ends with *.less.
        /// </summary>
        private static readonly Regex LessFileRegex = new Regex(@"\.less$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// Matches a less file. Ends with *.js.html.
        /// </summary>
        private static readonly Regex JsHtmlFileRegex = new Regex(@"\.js\.html", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// Determines whether or not the file is minified. The existance of "*.min.*".
        /// </summary>
        /// <param name="file">The name of the file.</param>
        /// <returns><c>true</c> when it's been minified; otherwise, <c>false</c>.</returns>
        public static bool IsMinified(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            else if (file == string.Empty)
            {
                throw new ArgumentOutOfRangeException("file", "Value cannot be empty.");
            }

            return MinifiedFileRegex.IsMatch(file);
        }

        public static bool IsCss(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            else if (file == string.Empty)
            {
                throw new ArgumentOutOfRangeException("file", "Value cannot be empty.");
            }

            return CssFileRegex.IsMatch(file);
        }

        public static bool IsLess(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            else if (file == string.Empty)
            {
                throw new ArgumentOutOfRangeException("file", "Value cannot be empty.");
            }

            return LessFileRegex.IsMatch(file);
        }

        public static bool IsJsHtml(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            else if (file == string.Empty)
            {
                throw new ArgumentOutOfRangeException("file", "Value cannot be empty.");
            }

            return JsHtmlFileRegex.IsMatch(file);
        }

        public static bool IsJavaScript(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            else if (file == string.Empty)
            {
                throw new ArgumentOutOfRangeException("file", "Value cannot be empty.");
            }

            return JavaScriptFileRegex.IsMatch(file);
        }
    }
}
