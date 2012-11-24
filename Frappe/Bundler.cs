using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Frappe
{
    internal class BundleState
    {
        public bool Bundled = false;
        public Bundle Bundle;
        public List<IncludeState> Includes;
    }

    internal class IncludeState
    {
        public bool Transformed = false;
        public Bundle Bundle;
        public Include Include;
        public string File;
    }

    public class BundlerOptions
    {
    }

    public class Bundler
    {
        /// <summary>
        /// Matches a css file.
        /// </summary>
        private static readonly Regex CssFileRegex = new Regex(@"\.css$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// Matches a js file.
        /// </summary>
        private static readonly Regex JsFileRegex = new Regex(@"\.js$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// Matches a less file.
        /// </summary>
        private static readonly Regex LessFileRegex = new Regex(@"\.less$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// The bundles loaded by thier file lower case.
        /// </summary>
        private Dictionary<string, Bundle> BundleByFileLower;

        /// <summary>
        /// The bundles already processed by this bundler.
        /// </summary>        
        private Dictionary<string, BundleState> BundleStateByFileLower;

        #region ImportFileNotFound Event

        public delegate void ImportFileNotFoundDelegate(Bundler sender, string file, string importFile, string importFileNotFound, string statement);

        public event ImportFileNotFoundDelegate ImportFileNotFound;

        protected void OnImportFileNotFound(string file, string importFile, string importFileNotFound, string statement)
        {
            if (ImportFileNotFound != null)
            {
                ImportFileNotFound(this, file, importFile, importFileNotFound, statement);
            }
        }

        #endregion

        #region FileBundled event

        public delegate void FileBundledDelegate(Bundler sender, string outputFile, string file);

        public event FileBundledDelegate FileBundled;

        protected void OnFileBundled(string outputFile, string file)
        {
            if (FileBundled != null)
            {
                FileBundled(this, outputFile, file);
            }
        }

        #endregion

        private void Initialize()
        {
            // reset state
            BundleByFileLower = new Dictionary<string, Frappe.Bundle>();
            BundleStateByFileLower = new Dictionary<string, BundleState>();
        }

        public void Bundle(string outputFile, bool overwrite, IEnumerable<string> files)
        {
            try
            {
                this.Initialize();

                if (overwrite)
                {
                    // clear the file
                    File.WriteAllText(outputFile, "");
                }

                foreach (var file in files)
                {
                    try
                    {
                        if (CssFileRegex.IsMatch(file)
                        || LessFileRegex.IsMatch(file))
                        {
                            var css = Css.CssParser.GetCss(file, true, import =>
                            {
                                OnImportFileNotFound(file, import.File, import.ImportFile, import.Statement);
                            });

                            // fix the relative paths
                            css = Css.CssParser.UpdateRelativePaths(css, Path.GetDirectoryName(file), Path.GetDirectoryName(outputFile));

                            // append the css to the file
                            File.AppendAllText(outputFile, css);
                        }
                        else
                        {
                            File.AppendAllText(outputFile, File.ReadAllText(file));
                        }

                        OnFileBundled(outputFile, file);
                    }
                    catch (Exception ex)
                    {
                        throw new ApplicationException(string.Format("Failed to append include to bundle output. An error occurred trying to append the include to the bundle output.  File: {0}", file), ex);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(string.Format("Failed to create bundle output. An error occurred trying to create the bundle output. OutputFile: {0}, Files: {1}", outputFile, string.Join(", ", files.Where(s => s != null).Select(s => string.Format("\"{0}\"", s)))), ex);
            }
        }

        public IEnumerable<string> GetFiles(string bundle)
        {
            this.Initialize();

            var bundleState = GetOrCreateBundleState(bundle);
            foreach (var includeFile in bundleState.Includes.Select(i => i.File.ToLower()).Distinct())
            {
                yield return includeFile;
            }
        }

        public IEnumerable<string> GetImportFiles(string bundle)
        {
            this.Initialize();

            var bundleState = GetOrCreateBundleState(bundle);
            foreach (var include in bundleState.Includes)
            {
                if (CssFileRegex.IsMatch(include.File)
                    || LessFileRegex.IsMatch(include.File))
                {
                    foreach (var importFile in Css.CssParser.GetFileImports(include.File))
                    {
                        yield return importFile;
                    }
                }
            }
        }

        private BundleState GetOrCreateBundleState(string bundleFile)
        {
            var bundleFileFullLower = Path.GetFullPath(bundleFile).ToLower();
            BundleState state;
            // check to see if this bundle is already bundled
            if (BundleStateByFileLower.TryGetValue(bundleFileFullLower, out state))
            {
                // IF we've already bundled this bundle
                // THEN return it's state
                return state;
            }
            // we need to create the state and return
            state = CreateBundleState(bundleFile);
            BundleStateByFileLower.Add(bundleFile, state);
            return state;
        }

        private Bundle GetOrLoadBundle(string bundleFile)
        {
            var bundleFileFullLower = Path.GetFullPath(bundleFile).ToLower();
            Bundle bundle;
            if (BundleByFileLower.TryGetValue(bundleFileFullLower, out bundle))
            {
                return bundle;
            }
            bundle = Frappe.Bundle.Load(bundleFileFullLower);
            BundleByFileLower.Add(bundleFileFullLower, bundle);
            return bundle;
        }

        private BundleState CreateBundleState(string bundleFile)
        {
            var bundle = GetOrLoadBundle(bundleFile);
            return new BundleState()
            {
                Bundle = bundle,
                Includes = GetIncludes(bundle).ToList(),
            };
        }

        /// <summary>
        /// Gets the underlying files included in this bundle.
        /// </summary>
        /// <returns>The files from the bundle and any inner bundles.</returns>
        private IEnumerable<IncludeState> GetIncludes(Bundle bundle)
        {
            // filesLower is hashset of files in lower case to ensure no dups
            var filesLower = new HashSet<string>();
            // files is the result original case and in order of load
            var files = new List<string>();
            if (bundle.Includes != null)
            {
                var bundleDirectory = Path.GetDirectoryName(bundle.File);
                var getRootedFile = new Func<string, string>(file =>
                {
                    if (!Path.IsPathRooted(file))
                    {
                        file = Path.GetFullPath(Path.Combine(bundleDirectory, file));
                    }
                    return file;
                });

                var bundlesLower = new HashSet<string>();
                foreach (var include in bundle.Includes)
                {
                    if (include is BundleInclude)
                    {
                        // IF this include is another bundle
                        // THEN we need to get all it's files

                        var bundleInclude = (BundleInclude)include;
                        var bundleIncludeFile = getRootedFile(bundleInclude.File);
                        var bundleIncludeFileLower = bundleIncludeFile.ToLower();
                        if (bundlesLower.Contains(bundleIncludeFileLower))
                        {
                            // IF we already have it
                            // THEN we can skip loading it
                            // BECAUSE we don't do recursive loading of bundles
                            continue;
                        }
                        else
                        {
                            bundlesLower.Add(bundleIncludeFileLower);
                        }

                        var includeBundleState = GetOrCreateBundleState(bundleIncludeFile);
                        foreach (var includeBundleInclude in includeBundleState.Includes)
                        {
                            yield return includeBundleInclude;
                        }
                    }
                    else
                    {
                        // ELSE just add this include

                        yield return new IncludeState()
                        {
                            File = getRootedFile(include.File),
                            Bundle = bundle,
                            Include = include,
                        };
                    }
                }
            }
        }
    }
}
