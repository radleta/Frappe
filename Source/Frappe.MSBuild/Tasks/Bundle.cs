using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Frappe.MSBuild.Tasks
{
    /// <summary>
    /// The MSBuild bundler task.
    /// </summary>
    public class Bundle : Task
    {
        /// <summary>
        /// The bundles to bundle.
        /// </summary>
        [Required]
        public ITaskItem[] Bundles { get; set; }

        /// <summary>
        /// Gets all the files from a bundle.
        /// </summary>
        /// <returns><c>true</c> when succesful; otherwise, <c>false</c>.</returns>
        public override bool Execute()
        {
            var files = Bundles.ToList().ConvertAll(input => new FileInfo(input.ItemSpec)).ToList();
            var allFilesExist = files.TrueForAll(f =>
            {
                if (!f.Exists)
                {
                    this.Log.LogError("File not found. File: {0}", f.FullName);
                    return false;
                }
                return true;
            });

            var bundler = new MSBuildBundler(this);
            bundler.Bundle(files.Select(f => f.FullName));

            return true;
        }
    }
}
