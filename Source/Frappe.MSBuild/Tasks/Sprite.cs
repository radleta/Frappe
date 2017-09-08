using Frappe.Sprites;
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
    /// The MSBuild sprite task.
    /// </summary>
    public class Sprite : Task
    {
        /// <summary>
        /// The sprite folders to bundle.
        /// </summary>
        [Required]
        public ITaskItem[] SpriteDirectory { get; set; }

        /// <summary>
        /// The root directory of the web site.
        /// </summary>
        [Required]
        public string WebSiteRootDirectory { get; set; }

        /// <summary>
        /// Gets all the files from a bundle.
        /// </summary>
        /// <returns><c>true</c> when succesful; otherwise, <c>false</c>.</returns>
        public override bool Execute()
        {
            var generator = new MSBuildSpriteGenerator(this, Path.GetFullPath(this.WebSiteRootDirectory));
            var directories = SpriteDirectory.ToList().ConvertAll(input => new DirectoryInfo(input.ItemSpec)).ToList();            
            foreach (var directory in directories)
            {
                this.Log.LogMessage($"Generating sprites. WebSiteRootDirectory: {WebSiteRootDirectory}, SpriteDirectory: {directory}");

                generator.ProcessDirectories(directory.FullName);
            }
            return true;
        }
    }
}
