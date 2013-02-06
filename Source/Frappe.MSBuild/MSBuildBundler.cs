using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Build.Tasks;
using Microsoft.Ajax.Minifier.Tasks;
using dotless.Core.Parser;

namespace Frappe.MSBuild
{
    /// <summary>
    /// The MSBuild bundler.
    /// </summary>
    public class MSBuildBundler : Bundler
    {
        /// <summary>
        /// Initialies a new instance of this class.
        /// </summary>
        /// <param name="task">The task which is hosting this bundler.</param>
        /// <param name="options">The options for the bundler.</param>
        public MSBuildBundler(Task task, BundlerOptions options = null) : base(options)
        {
            if (task == null)
            {
                throw new System.ArgumentNullException("task");
            }

            Task = task;
        }

        /// <summary>
        /// The task which is executing this bundler.
        /// </summary>
        public Task Task { get; private set; }

        protected override void LogInfo(string format, params object[] args)
        {
            Task.Log.LogMessage(format, args);
        }

        protected override void LogWarning(string format, params object[] args)
        {
            Task.Log.LogWarning(format, args);
        }

        protected override void CompileLess(string lessFile, string outputCssFile)
        {
            var compiler = new Tasks.dotlessCompiler();
            compiler.BuildEngine = this.Task.BuildEngine;
            compiler.Arguments = string.Format(@"""{0}"" ""{1}""", lessFile, outputCssFile);
            compiler.Execute();
        }

        protected override void MinifyCss(string cssFile, string outputMinifiedCssFile)
        {
            var ajaxMinTask = new AjaxMin();
            ajaxMinTask.BuildEngine = this.Task.BuildEngine;
            ajaxMinTask.CssSourceFiles = new ITaskItem[] { new TaskItem(cssFile) };
            ajaxMinTask.CssCombinedFileName = outputMinifiedCssFile;
            ajaxMinTask.Execute();
        }

        protected override void MinifyJavaScript(string javaScriptFile, string outputMinifiedJavaScriptFile)
        {
            var ajaxMinTask = new AjaxMin();
            ajaxMinTask.BuildEngine = this.Task.BuildEngine;
            ajaxMinTask.JsSourceFiles = new ITaskItem[] { new TaskItem(javaScriptFile) };
            ajaxMinTask.JsCombinedFileName = outputMinifiedJavaScriptFile;
            ajaxMinTask.Execute();
        }
    }
}
