using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Frappe
{
    /// <summary>
    /// The bundler to transform and combine less, js, and css.
    /// </summary>
    public class Bundler
    {
        #region BundleState class

        protected class BundleState
        {
            public bool Transformed = false;
            public bool Bundled = false;
            public Bundle Bundle;
            public FileInfo BundleFile;
            public FileInfo BundleOutputFile;
            public List<IncludeState> Includes;
        }

        #endregion

        #region IncludeState class

        protected class IncludeState
        {
            public bool Transformed = false;
            public Include Include;
            public FileInfo File;
            public FileInfo OutputFile;
            public List<string> Imports;
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="options">Optional. The options for the bundler.</param>
        public Bundler(BundlerOptions options = null)
        {
            Options = options ?? new BundlerOptions();
        }

        /// <summary>
        /// The options for the bundler.
        /// </summary>
        public BundlerOptions Options { get; set; }

        /// <summary>
        /// The state of the bundles.
        /// </summary>        
        private Dictionary<string, BundleState> BundleStateByFile;

        /// <summary>
        /// The state of the includes.
        /// </summary>
        private Dictionary<string, IncludeState> IncludeStateByFile;

        #region ImportFileNotFound Event

        public delegate void ImportFileNotFoundDelegate(Bundler sender, string file, string importFile, string importFileNotFound, string statement);

        public event ImportFileNotFoundDelegate ImportFileNotFound;

        protected virtual void OnImportFileNotFound(string file, string importFile, string importFileNotFound, string statement)
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

        protected virtual void OnFileBundled(string outputFile, string file)
        {
            if (FileBundled != null)
            {
                FileBundled(this, outputFile, file);
            }
        }

        #endregion

        /// <summary>
        /// Compiles a Less file into Css.
        /// </summary>
        /// <param name="lessFile">The less file.</param>
        /// <param name="outputCssFile">The output css file.</param>
        protected virtual void CompileLess(string lessFile, string outputCssFile)
        {
            throw new NotImplementedException("This method has not been implemented.");
        }

        /// <summary>
        /// Minifies the <c>cssFile</c> into the <c>outputMinifiedCssFile</c>.
        /// </summary>
        /// <param name="cssFile">The css file.</param>
        /// <param name="outputMinifiedCssFile">The output minified css file.</param>
        protected virtual void MinifyCss(string cssFile, string outputMinifiedCssFile)
        {
            throw new NotImplementedException("This method has not been implemented.");
        }

        /// <summary>
        /// Minifies the <c>javaScriptFile</c> into the <c>outputMinifiedJavaScriptFile</c>.
        /// </summary>
        /// <param name="javaScriptFile">The javascript file.</param>
        /// <param name="outputMinifiedJavaScriptFile">The output minified javascript file.</param>
        protected virtual void MinifyJavaScript(string javaScriptFile, string outputMinifiedJavaScriptFile)
        {
            throw new NotImplementedException("This method has not been implemented.");
        }

        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The args for the format.</param>
        protected virtual void LogInfo(string format, params object[] args)
        {
            // ignore; can be overridden by inheriting class to emit the log messages
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The args for the format.</param>
        protected virtual void LogWarning(string format, params object[] args)
        {
            // ignore; can be overridden by inheriting class to emit the log messages
        }

        private void Initialize()
        {
            // reset state
            BundleStateByFile = new Dictionary<string, BundleState>(StringComparer.InvariantCultureIgnoreCase);
            IncludeStateByFile = new Dictionary<string, IncludeState>(StringComparer.InvariantCultureIgnoreCase);
        }

        public void Bundle(string bundleFile)
        {
            this.Initialize();

            // open the bundle
            var bundle = Frappe.Bundle.Load(bundleFile);

            Bundle(bundle);
        }

        public void Bundle(IEnumerable<string> bundleFiles)
        {
            this.Initialize();

            foreach (var bundleFile in bundleFiles)
            {
                // open the bundle
                var bundle = Frappe.Bundle.Load(bundleFile);

                Bundle(bundle);
            }
        }

        private void Bundle(Bundle bundle)
        {
            if (bundle == null)
            {
                throw new System.ArgumentNullException("bundle");
            }
            try
            {
                // put the bundle into the state
                var bundleState = CreateBundleState(bundle);

                if (!bundleState.Bundled)
                {
                    LogInfo("Ensuring the bundle \"{0}\" is up-to-date.", bundleState.BundleFile);

                    // transform all includes
                    foreach (var include in bundleState.Includes)
                    {
                        Transform(include);
                    }

                    // transform the bundle
                    Transform(bundleState);

                    // mark the bundle as bundled
                    bundleState.Bundled = true;
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(string.Format("An error occurred while bundling a bundle file. Bundle File: {0}", bundle.File), ex);
            }
        }

        public IEnumerable<string> GetFiles(string bundleFile)
        {
            this.Initialize();

            var bundleState = GetOrCreateBundleState(bundleFile);
            return bundleState.Includes.Select(i => i.File.FullName);
        }

        public IEnumerable<string> GetAllIncludeImportFiles(string bundle)
        {
            this.Initialize();

            return GetOrCreateBundleState(bundle).Includes
                .SelectMany(i => i.Imports)
                .Distinct(StringComparer.InvariantCultureIgnoreCase);
        }

        private void Transform(BundleState bundle)
        {
            if (bundle.Transformed)
            {
                // IF we've already transformed this file
                // THEN bail

                return;
            }
            else if (bundle.Includes.Count == 0)
            {
                // IF nothing to do
                // THEN mark it as transformed and bail

                bundle.Transformed = true;
                return;
            }

            // find the most recent date from all the includes
            var inputMostRecentLastWriteTimeUtc = bundle.Includes
                .Select(include => include.OutputFile.LastWriteTimeUtc)
                .OrderByDescending(d => d)
                .First();

            // get the output file
            var outputFile = bundle.BundleOutputFile;

            // determine whether or not the bundle needs to be transformed
            if (!outputFile.Exists || outputFile.LastWriteTimeUtc < inputMostRecentLastWriteTimeUtc)
            {
                // IF the output file does NOT exist
                //   OR the last write time of the output file is less than the input most recent last write time
                // THEN we need to transform this bundle

                LogInfo("The output \"{0}\" for bundle \"{1}\" does not exist or is out-of-date.", bundle.BundleOutputFile, bundle.BundleFile);

                // concat the bundle together to create the output file
                ConcatBundle(bundle);

                // refresh the file now
                outputFile.Refresh();

                // set the last write time utc to the input because the output is based on the inputs
                outputFile.LastWriteTimeUtc = inputMostRecentLastWriteTimeUtc;

                LogInfo("The output \"{0}\" for bundle \"{1}\" has been updated.", bundle.BundleOutputFile, bundle.BundleFile);
            }

            bundle.Transformed = true;
        }

        private void ConcatBundle(BundleState bundle)
        {
            if (bundle == null)
            {
                throw new System.ArgumentNullException("bundle");
            }

            var outputFileInfo = bundle.BundleOutputFile;
            var outputFile = outputFileInfo.FullName;
            var includeFileInfos = bundle.Includes.Select(include => include.OutputFile).ToList();

            try
            {

                // clear the output file
                File.WriteAllText(outputFile, "");

                // concat all the output of the includes together
                foreach (var includeFileInfo in includeFileInfos)
                {
                    var file = includeFileInfo.FullName;
                    try
                    {
                        if (FileExtension.IsCss(file)
                            || FileExtension.IsLess(file))
                        {
                            var css = Css.CssParser.GetCss(file, true, import =>
                            {
                                LogWarning("An import file could not be found. File: {0}, RelativeTo: {1}, Import: {2}, Statement: {3}", file, import.File, import.ImportFile, import.Statement);
                            });

                            // fix the relative paths
                            css = Css.CssParser.UpdateRelativePaths(css, includeFileInfo.DirectoryName, outputFileInfo.DirectoryName);

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
                throw new ApplicationException(string.Format("Failed to create bundle output. An error occurred trying to create the bundle output. OutputFile: {0}, Files: {1}", outputFile, string.Join(", ", includeFileInfos.Where(s => s != null).Select(s => string.Format("\"{0}\"", s)))), ex);
            }
        }

        private void Transform(IncludeState include)
        {
            if (include == null)
            {
                throw new System.ArgumentNullException("include");
            }

            try
            {

                if (include.Transformed)
                {
                    // IF we've already transformed this file
                    // THEN bail

                    return;
                }

                LogInfo("Ensuring the include \"{0}\" is up-to-date.", include.File.FullName);

                // what are the input files
                var inputFiles = new List<FileInfo>();
                inputFiles.Add(include.File);
                inputFiles.AddRange(include.Imports.Select(s => new FileInfo(s)));

                // what is the most recent last write time utc
                var inputMostRecentLastWriteTimeUtc = inputFiles.Select(f => f.LastWriteTimeUtc)
                    .OrderByDescending(d => d)
                    .First();

                FileInfo inputFile = include.File;
                if (FileExtension.IsLess(include.File.FullName))
                {
                    // IF it's a less file
                    // THEN we need to create the css output file

                    inputFile = new FileInfo(include.File.FullName + ".css");
                    if (!inputFile.Exists || inputFile.LastWriteTimeUtc < inputMostRecentLastWriteTimeUtc)
                    {
                        // IF the input file doesn't exist
                        //   OR the file is older than any of the inputs
                        // THEN re-compile the less file

                        LogInfo("The css output \"{0}\" for less include \"{1}\" does not exist or is out-of-date.", inputFile, include.File.FullName);

                        // compile the less file
                        this.CompileLess(include.File.FullName, inputFile.FullName);

                        // refresh the input file
                        inputFile.Refresh();

                        // set the file last write time to match the inputs since it's generated based on them
                        inputFile.LastWriteTimeUtc = inputMostRecentLastWriteTimeUtc;

                        LogInfo("The css output \"{0}\" for less include \"{1}\" has been updated.", inputFile, include.File.FullName);
                    }
                }

                // do they exist and are they up-to-date relative to the outputs?
                var outputFile = include.OutputFile;

                // tranform the include
                if (!outputFile.Exists || outputFile.LastWriteTimeUtc < inputFile.LastWriteTimeUtc)
                {
                    // IF the output file does NOT exist
                    //   OR the output file is out-of-date
                    // THEN rebuild the output file

                    LogInfo("The minified output \"{0}\" for include \"{1}\" does not exist or is out-of-date.", outputFile, inputFile);

                    if (FileExtension.IsCss(inputFile.FullName))
                    {
                        MinifyCss(inputFile.FullName, outputFile.FullName);
                    }
                    else if (FileExtension.IsJavaScript(inputFile.FullName))
                    {
                        MinifyJavaScript(inputFile.FullName, outputFile.FullName);
                    }
                    else
                    {
                        throw new NotImplementedException(string.Format("Transformation of file has not been implemented. File: {0}", inputFile.FullName));
                    }

                    // update the output file
                    outputFile.Refresh();

                    // set the file last write time to match the input since it's generated based on it
                    outputFile.LastWriteTimeUtc = inputMostRecentLastWriteTimeUtc;

                    LogInfo("The minified output \"{0}\" for include \"{1}\" has been updated.", outputFile, inputFile);
                }

                // update the state
                include.Transformed = true;
                include.OutputFile = outputFile;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(string.Format("An error occurred trying to transform bundle include. Include File: {0}", include.File), ex);
            }
        }
                
        private BundleState GetOrCreateBundleState(string bundleFile)
        {
            var bundleFileFullPath = Path.GetFullPath(bundleFile);
            BundleState state;
            // check to see if this bundle is already bundled
            if (BundleStateByFile.TryGetValue(bundleFileFullPath, out state))
            {
                // IF we've already bundled this bundle
                // THEN return it's state
                return state;
            }
            // we need to create the state and return
            state = CreateBundleState(bundleFile);
            BundleStateByFile.Add(bundleFileFullPath, state);
            return state;
        }

        private BundleState CreateBundleState(string bundleFile)
        {
            var bundle = Frappe.Bundle.Load(bundleFile);
            return CreateBundleState(bundle);
        }

        private BundleState CreateBundleState(Bundle bundle)
        {
            var includes = GetIncludeStates(bundle).ToList();

            return new BundleState()
            {
                Bundle = bundle,
                BundleFile = new FileInfo(bundle.File),
                BundleOutputFile = new FileInfo(bundle.GetOutputFile()),
                Includes = includes,
            };
        }

        public string EnsureFileRooted(string bundleFileDirectory, string file)
        {
            if (bundleFileDirectory == null)
            {
                throw new ArgumentNullException("bundleFileDirectory");
            }
            else if (bundleFileDirectory == string.Empty)
            {
                throw new ArgumentOutOfRangeException("bundleFileDirectory", "Value cannot be empty.");
            }

            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            else if (file == string.Empty)
            {
                throw new ArgumentOutOfRangeException("file", "Value cannot be empty.");
            }

            if (!Path.IsPathRooted(file))
            {
                return Path.GetFullPath(Path.Combine(bundleFileDirectory, file));
            }
            return file;
        }

        private IncludeState CreateIncludeState(string bundleFileDirectory, Include include)
        {
            var includeFileFullPath = new FileInfo(EnsureFileRooted(bundleFileDirectory, include.File));

            return new IncludeState()
            {
                File = includeFileFullPath,
                Include = include,
                Imports = Include.GetImportFiles(includeFileFullPath.FullName)
                    .Select(s => EnsureFileRooted(includeFileFullPath.Directory.FullName, s))
                    .Distinct(StringComparer.InvariantCultureIgnoreCase)
                    .ToList(),
                OutputFile = new FileInfo(EnsureFileRooted(bundleFileDirectory, include.GetOutputFile())),
            };
        }

        /// <summary>
        /// Gets the underlying files included in this bundle.
        /// </summary>
        /// <returns>The files from the bundle and any inner bundles.</returns>
        private List<IncludeState> GetIncludeStates(Bundle bundle)
        {
            var includeStatesByFile = new Dictionary<string, IncludeState>(StringComparer.InvariantCultureIgnoreCase);
            var includes = bundle.Includes;
            if (includes != null)
            {
                string bundleFileDirectory = Path.GetDirectoryName(Path.GetFullPath(bundle.File));
                foreach (var include in includes)
                {
                    if (include is BundleInclude)
                    {
                        // IF this include is another bundle
                        // THEN we need to get all it's files

                        var bundleInclude = (BundleInclude)include;
                        var includeBundleFile = EnsureFileRooted(bundleFileDirectory, bundleInclude.File);
                        var includeBundleState = GetOrCreateBundleState(includeBundleFile);
                        foreach (var includeBundleInclude in includeBundleState.Includes)
                        {
                            if (!includeStatesByFile.ContainsKey(includeBundleInclude.File.FullName))
                            {
                                includeStatesByFile.Add(includeBundleInclude.File.FullName, includeBundleInclude);
                            }
                        }
                    }
                    else
                    {
                        // ELSE just add this include

                        var includeFile = EnsureFileRooted(bundleFileDirectory, include.File);
                        if (!includeStatesByFile.ContainsKey(includeFile))
                        {
                            includeStatesByFile.Add(includeFile, CreateIncludeState(bundleFileDirectory, include));
                        }
                    }
                }
            }
            return includeStatesByFile.Values.ToList();
        }

    }
}
