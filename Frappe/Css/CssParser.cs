using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace Frappe.Css
{
    /// <summary>
    /// Parses css files.
    /// </summary>
    public class CssParser
    {
        /// <summary>
        /// Parse the @imports statements. Ex: @import "foo.css";
        /// </summary>
        private static readonly Regex ImportsRegex = new Regex(@"@import\s*(url\()?[""'](?'File'[^""'\n]+)[""']\)?;", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        /// <summary>
        /// Gets all the file imports recursively from a css file.
        /// </summary>
        /// <param name="file">A css file.</param>
        /// <returns></returns>
        public static IEnumerable<string> GetFileImports(string file, Action<CssImportStatement> missingImportFile = null)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            else if (file == string.Empty)
            {
                throw new ArgumentOutOfRangeException("file", "Value cannot be empty.");
            }

            var css = System.IO.File.ReadAllText(file);
            return GetFileImports(file, css, missingImportFile);
        }

        /// <summary>
        /// Gets the file imports from the <c>css</c>. Paths are relative to the <c>file</c>.
        /// </summary>
        /// <param name="file">The file where the css originated. 
        /// Used for relative paths to other files within the css.</param>
        /// <param name="css">The css.</param>
        /// <param name="missingImportFile">Called when an import file could not be found.</param>
        /// <returns>The file imports from the <c>css</c>.</returns>
        public static IEnumerable<string> GetFileImports(string file, string css, Action<CssImportStatement> missingImportFile = null)
        {
            foreach (var import in GetImports(file, css))
            {
                if (File.Exists(import.ImportFile))
                {
                    yield return import.ImportFile;
                }
                else
                {
                    if (missingImportFile != null)
                        missingImportFile(import);
                }
            }
        }

        /// <summary>
        /// Gets the css from the <c>file</c>.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="expandImports">Determines whether or not imported css is expanded or not.</param>
        /// <param name="missingImportFile">Called when an imported file could not be found.</param>
        /// <returns>The css.</returns>
        public static string GetCss(string file, bool expandImports, Action<CssImportStatement> missingImportFile = null)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            else if (file == string.Empty)
            {
                throw new ArgumentOutOfRangeException("file", "Value cannot be empty.");
            }

            var css = System.IO.File.ReadAllText(file);
            if (expandImports)
            {
                GetImports(file, css).Count(import => {
                    if (File.Exists(import.ImportFile))
                    {
                        var importCss = GetCss(import.ImportFile, expandImports, missingImportFile);
                        css = css.Replace(import.Statement, importCss);
                    }
                    else
                    {
                        if (missingImportFile != null)
                            missingImportFile(import);
                    }
                    return true;
                });
            }
            return css;
        }

        /// <summary>
        /// Match paths within the css. Ignores http references and absolute paths.
        /// </summary>
        private static readonly Regex PathRegex = new Regex(@"(?'Pre'(?'Url'url\([""']?)|[""'])(?!https?://|/)(?'Path'[^""'\n)]*)(?'Post'[""']|\))", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Gets the relative paths from the css.
        /// </summary>
        /// <param name="css">The css.</param>
        /// <returns>The relative paths from the css.</returns>
        public static IEnumerable<string> GetRelativePaths(string css)
        {
            var pathMatches = PathRegex.Matches(css);
            if (pathMatches.Count > 0)
            {
                foreach (Match pathMatch in pathMatches)
                {
                    if (pathMatch.Success)
                    {
                        var pathGroup = pathMatch.Groups["Path"];
                        if (pathGroup.Success
                            && pathGroup.Length > 0)
                        {
                            yield return pathGroup.Value;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the relativate paths within the <c>css</c> relative to the source directory to be
        /// relative to the target directory.
        /// </summary>
        /// <param name="css">The css.</param>
        /// <param name="sourceDirectory">The source directory where the paths are relative.</param>
        /// <param name="targetDirectory">The target directory where the paths should be 
        /// updated to be relative.</param>
        /// <returns>The updated css.</returns>
        public static string UpdateRelativePaths(string css, string sourceDirectory, string targetDirectory)
        {
            var sourceDirectoryFull = Path.GetFullPath(sourceDirectory);
            var targetDirectoryFull = Path.GetFullPath(targetDirectory).TrimEnd('\\') + "\\";
            var targetUri = new Uri(targetDirectoryFull);

            var pathMatches = PathRegex.Matches(css);
            if (pathMatches.Count > 0)
            {
                foreach (Match pathMatch in pathMatches)
                {
                    if (pathMatch.Success)
                    {
                        var preGroup = pathMatch.Groups["Pre"];
                        var postGroup = pathMatch.Groups["Post"];
                        var pathGroup = pathMatch.Groups["Path"];
                        var urlGroup = pathMatch.Groups["Url"];
                        if (pathGroup.Success
                            && pathGroup.Length > 0)
                        {
                            try
                            {
                                var sourceFile = Path.Combine(sourceDirectoryFull, pathGroup.Value.Replace("/", @"\"));
                                Uri sourceUri;
                                if ((urlGroup.Success || File.Exists(sourceFile))
                                    && Uri.TryCreate(sourceFile, UriKind.RelativeOrAbsolute, out sourceUri))
                                {
                                    // IF (the path is surrounded by url(...) OR the path matches a physical file)
                                    //   AND we're able to create a valid uri from the path
                                    // THEN update the path
                                    // BECAUSE we need to avoid other string literals within the css

                                    var relativeUri = targetUri.MakeRelativeUri(sourceUri);
                                    css = css.Replace(pathMatch.Value, preGroup.Value + relativeUri.ToString() + postGroup.Value);
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new ApplicationException(string.Format("Relative path update failed. An error occured trying to update the relative paths. Path: {0}", pathMatch.Value), ex);
                            }
                        }
                    }
                }
            }
            return css;
        }

        /// <summary>
        /// Gets the @import statement values from the css relative to the <c>file</c>.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="css">The css from the file.</param>
        /// <returns>The @import statement values.</returns>
        private static IEnumerable<CssImportStatement> GetImports(string file, string css)
        {
            var fileDirectory = Path.GetDirectoryName(file);
            var importMatches = ImportsRegex.Matches(css);
            if (importMatches.Count > 0)
            {
                foreach (Match importMatch in importMatches)
                {
                    if (importMatch.Success)
                    {
                        var fileGroup = importMatch.Groups["File"];
                        Uri fileUri;
                        if (fileGroup.Success
                            && Uri.TryCreate(fileGroup.Value, UriKind.RelativeOrAbsolute, out fileUri)
                            && !fileUri.IsAbsoluteUri)
                        {
                            var childImportFile = Path.Combine(fileDirectory, fileGroup.Value);

                            yield return new CssImportStatement() { 
                                ImportFile = childImportFile,
                                Statement = importMatch.Value,
                                File = file,
                            };

                            // recursively process the imports                            
                            if (File.Exists(childImportFile))
                            {
                                var importCss = System.IO.File.ReadAllText(childImportFile);
                                foreach (var fileImport in GetImports(childImportFile, importCss))
                                {
                                    yield return fileImport;
                                }
                            }
                        }
                    }
                }
            }
        }

    }
}
