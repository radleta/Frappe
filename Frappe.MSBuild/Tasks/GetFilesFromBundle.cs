using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Text.RegularExpressions;

namespace Frappe.Tasks
{
    /// <summary>
    /// A msbuild task to load the files from a bundle.
    /// </summary>
    public class GetFilesFromBundle : Task
    {

        /// <summary>
        /// Matches a css file.
        /// </summary>
        private static readonly Regex CssFileRegex = new Regex(@"\.css$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// Matches a less file.
        /// </summary>
        private static readonly Regex LessFileRegex = new Regex(@"\.less$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        /// <summary>
        /// The bundle file.
        /// </summary>
        [Required]
        public string BundleFile { get; set; }

        /// <summary>
        /// The files loaded from the bundle file.
        /// </summary>
        [Output]
        public ITaskItem[] Files { get; set; }

        /// <summary>
        /// The files imported by the files referenced in the bundle.
        /// </summary>
        [Output]
        public ITaskItem[] ImportFiles { get; set; }

        /// <summary>
        /// Gets all the files from a bundle.
        /// </summary>
        /// <returns><c>true</c> when succesful; otherwise, <c>false</c>.</returns>
        public override bool Execute()
        {
            try
            {
                var bundler = new Bundler();

                var bundleFiles = bundler.GetFiles(this.BundleFile).Select(file => new TaskItem()
                {
                    ItemSpec = file,
                });
                this.Files = bundleFiles.ToArray();

                var importFiles = bundler.GetImportFiles(this.BundleFile).Select(file => new TaskItem()
                {
                    ItemSpec = file,
                });
                this.ImportFiles = importFiles.ToArray();

                return true;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex, true, true, BundleFile);
                return false;
            }
        }
    }
}
