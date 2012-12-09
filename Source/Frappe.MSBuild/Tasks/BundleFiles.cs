using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Frappe.Tasks
{
    /// <summary>
    /// A msbuild task to bundle multiple files together.
    /// </summary>
    public class BundleFiles : Task
    {
        /// <summary>
        /// The file to append.
        /// </summary>
        [Required]
        public string File { get; set; }

        /// <summary>
        /// The files to append to the output.
        /// </summary>
        public ITaskItem[] Inputs { get; set; }

        /// <summary>
        /// Deteremines whether or not to overwrite the file.
        /// </summary>
        public bool Overwrite { get; set; }

        /// <summary>
        /// Gets all the files from a bundle.
        /// </summary>
        /// <returns><c>true</c> when succesful; otherwise, <c>false</c>.</returns>
        public override bool Execute()
        {
            var files = Inputs.ToList().ConvertAll(input => new FileInfo(input.ItemSpec)).ToList();
            var allFilesExist = files.TrueForAll(f =>
            {
                if (!f.Exists)
                {
                    this.Log.LogError("File not found. File: {0}", f.FullName);
                    return false;
                }
                return true;
            });
                        
            var bundler = new Bundler();
            bundler.ImportFileNotFound += bundler_ImportFileNotFound;
            bundler.FileBundled += bundler_FileBundled;
            bundler.Bundle(File, Overwrite, files.Select(f => f.FullName));
            return true;
        }

        /// <summary>
        /// Handler for the bundler to report bundled files.
        /// </summary>
        void bundler_FileBundled(Bundler sender, string outputFile, string file)
        {
            // log message for msbuild
            Log.LogMessage("Appended file \"{0}\" to \"{1}\".", file, outputFile);
        }

        /// <summary>
        /// Handler for the bundler to report missing import files.
        /// </summary>
        void bundler_ImportFileNotFound(Bundler sender, string file, string importFile, string importFileNotFound, string statement)
        {
            Log.LogWarning("An import file could not be found. File: {0}, RelativeTo: {1}, Import: {2}, Statement: {2}, ", file, importFile, importFileNotFound, statement);
        }
    }
}
