using System;
using System.IO;
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
        
        /// <summary>
        /// Compiles a *.js.html file into JavaScript.
        /// </summary>
        /// <param name="jsHtmlFile">The input html file.</param>
        /// <param name="outputJSFile">The output javascript file.</param>
        protected override void CompileJsHtml(string jsHtmlFile, string outputJSFile)
        {
            if (jsHtmlFile == null)
            {
                throw new ArgumentNullException("jsHtmlFile");
            }
            else if (jsHtmlFile == string.Empty)
            {
                throw new ArgumentOutOfRangeException("jsHtmlFile", "Value cannot be empty.");
            }
            if (!File.Exists(jsHtmlFile))
            {
                throw new FileNotFoundException("Unable to locate the .js.html file.", jsHtmlFile);
            }
            // read all the html in
            var html = File.ReadAllText(jsHtmlFile);

            // create the converter
            var converter = new HtmlToJavaScript.HtmlConverter();

            // get the name of the html in javascript
            var name = Path.GetFileName(jsHtmlFile).ToLower();

            // convert the html to js
            var js = converter.ToJavaScript(name, html);
            
            // write the js back to the output file
            File.WriteAllText(outputJSFile, js);
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
            ajaxMinTask.TreatWarningsAsErrors = true;
            ajaxMinTask.BuildEngine = this.Task.BuildEngine;
            ajaxMinTask.CssSourceFiles = new ITaskItem[] { new TaskItem(cssFile) };
            ajaxMinTask.CssCombinedFileName = outputMinifiedCssFile;
            if (ajaxMinTask.Execute())
            {
                // IF success
                // THEN do nothing
            }
            else
            {
                // ELSE failure
                // THEN raise an error
                                
                throw new ApplicationException(string.Format("An error occurred trying to minify css file. Css File: {0}", cssFile));
            }
        }

        protected override void MinifyJavaScript(string javaScriptFile, string outputMinifiedJavaScriptFile)
        {
            var ajaxMinTask = new AjaxMin();
            ajaxMinTask.TreatWarningsAsErrors = true;
            ajaxMinTask.BuildEngine = this.Task.BuildEngine;
            ajaxMinTask.JsSourceFiles = new ITaskItem[] { new TaskItem(javaScriptFile) };
            ajaxMinTask.JsCombinedFileName = outputMinifiedJavaScriptFile;
            if (ajaxMinTask.Execute())
            {
                // IF success
                // THEN do nothing
            }
            else
            {
                // ELSE failure
                // THEN raise an error

                throw new ApplicationException(string.Format("An error occurred trying to minify JavaScript file. JavaScript File: {0}", javaScriptFile, outputMinifiedJavaScriptFile));
            }
        }
    }
}
