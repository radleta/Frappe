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
    /// A msbuild task to copy the last write time of the most recent of files to all the other files.
    /// </summary>
    public class CopyMostRecentCreationAndLastWriteTimeToFiles : Task
    {
        /// <summary>
        /// The files to scan for the most recent last write to use to apply to all of the <c>ToFiles</c>.
        /// </summary>
        [Required]
        public ITaskItem[] FromFiles { get; set; }

        /// <summary>
        /// The files to update with the last write time from the <c>FromFiles</c>.
        /// </summary>
        [Required]
        public ITaskItem[] ToFiles { get; set; }

        /// <summary>
        /// Gets all the files from a bundle.
        /// </summary>
        /// <returns><c>true</c> when succesful; otherwise, <c>false</c>.</returns>
        public override bool Execute()
        {
            var fromFileInfos = FromFiles.ToList().ConvertAll(input => new FileInfo(input.ItemSpec)).ToList();
            var allFromFilesExist = fromFileInfos.TrueForAll(f =>
            {
                if (!f.Exists)
                {
                    this.Log.LogError("Files not found. A file specified in FromFiles does not exist. File: {0}", f.FullName);
                    return false;
                }
                return true;
            });
            if (!allFromFilesExist)
            {
                return false;
            }
                        
            var toFileInfos = ToFiles.ToList().ConvertAll(input => new FileInfo(input.ItemSpec)).ToList();
            var allToFilesExist = toFileInfos.TrueForAll(f =>
            {
                if (!f.Exists)
                {
                    this.Log.LogError("Files not found. A file specified in ToFiles does not exist. File: {0}", f.FullName);
                    return false;
                }
                return true;
            });
            if (!allToFilesExist)
            {
                return false;
            }

            FileInfo mostRecentLastWriteFile = null;
            foreach (var fromFile in fromFileInfos)
            {
                if (mostRecentLastWriteFile == null
                    || mostRecentLastWriteFile.LastWriteTimeUtc < fromFile.LastWriteTimeUtc)
                {
                    mostRecentLastWriteFile = fromFile;
                }
            }
            
            toFileInfos.ForEach(f => {
                f.CreationTimeUtc = mostRecentLastWriteFile.CreationTimeUtc;
                f.LastWriteTimeUtc = mostRecentLastWriteFile.LastWriteTimeUtc;
                this.Log.LogMessageFromText(string.Format("The creation and last write time of \"{0}\" file were applied to \"{1}\" file.", mostRecentLastWriteFile, f.FullName), MessageImportance.Low);
            });

            return true;
        }
    }
}
