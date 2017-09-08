using Frappe.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Build.Tasks;

namespace Frappe.MSBuild
{
    /// <summary>
    /// The MSBuild implementation of the <see cref="SpriteGenerator"/>.
    /// </summary>
    public class MSBuildSpriteGenerator : SpriteGenerator
    {
        public MSBuildSpriteGenerator(Task task, string webSiteRootDirectory) : base(webSiteRootDirectory)
        {
            if (task == null)
            {
                throw new System.ArgumentNullException(nameof(task));
            }

            Task = task;
        }

        /// <summary>
        /// The task which is executing this bundler.
        /// </summary>
        public Task Task { get; private set; }
        
        /// <summary>
        /// Determines whether or not logging is enabled.
        /// </summary>
        protected override bool IsLoggingEnabled
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void LogError(string message)
        {
            Task.Log.LogError(message);
        }

        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void LogMessage(string message)
        {
            Task.Log.LogMessage(message);
        }
    }
}
