using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Frappe
{
    /// <summary>
    /// A file included in a bundle.
    /// </summary>
    [XmlInclude(typeof(BundleInclude))]
    public class Include
    {
        /// <summary>
        /// Matches the extension of the include.
        /// </summary>
        private static Regex IncludeFileRegex = new Regex(@"(?'Name'.+?)(?'TypeExt'\.(?:css|js))$", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Gets the import files from the file.
        /// </summary>
        /// <param name="file">The file to analyze for imports.</param>
        /// <returns>The imports for the file.</returns>
        public static IEnumerable<string> GetImportFiles(string file)
        {
            if (FileExtension.IsCss(file)
                || FileExtension.IsLess(file))
            {
                return Css.CssParser.GetFileImports(file);
            }
            return new List<string>(0);
        }

        /// <summary>
        /// The path to the file.
        /// </summary>
        [XmlAttribute]
        public string File { get; set; }
        
        /// <summary>
        /// The output file of the include.
        /// </summary>
        public string OutputFile { get; set; }

        /// <summary>
        /// Get the output file for the include.
        /// </summary>
        /// <returns>The output file for the include.</returns>
        public string GetOutputFile()
        {
            if (File == null)
            {
                throw new ArgumentNullException("File");
            }
            else if (File == string.Empty)
            {
                throw new ArgumentOutOfRangeException("File", "Value cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(OutputFile))
            {
                string inputFile = File;
                if (FileExtension.IsMinified(inputFile))
                {
                    return inputFile;
                }
                else
                {
                    if (FileExtension.IsLess(inputFile))
                    {
                        inputFile += ".css";
                    }
                    else if (FileExtension.IsJsHtml(inputFile))
                    {
                        inputFile += ".js";
                    }
                    return IncludeFileRegex.Replace(inputFile, @"${Name}.min${TypeExt}");
                }
            }
            else
            {
                if (Path.IsPathRooted(OutputFile))
                {
                    return OutputFile;
                }
                else
                {
                    return Path.Combine(Path.GetDirectoryName(Path.GetFullPath(File)), OutputFile);
                }
            }
        }
    }
}
