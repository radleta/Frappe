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
    /// A msbuild task to append multiple files together.
    /// </summary>
    public class AppendFiles : Task
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

            if (allFilesExist)
            {
                if (Overwrite
                    && System.IO.File.Exists(File))
                {
                    System.IO.File.Delete(File);
                }

                files.ForEach(f => {
                        var contents = System.IO.File.ReadAllText(f.FullName);
                        if (contents.Length > 0
                            && !contents.EndsWith(Environment.NewLine))
                        {
                            contents += Environment.NewLine;
                        }
                        System.IO.File.AppendAllText(File, contents);
                        Log.LogMessage("Appended file \"{0}\" to \"{1}\".", f.FullName, File);
                    });

                return true;
            }

            return false;
        }
    }
}
