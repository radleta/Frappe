using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Frappe
{
    public class NUnitBundler : Bundler
    {
        protected override void CompileLess(string lessFile, string outputCssFile)
        {
            System.IO.File.Copy(lessFile, outputCssFile);
        }

        protected override void CompileJsHtml(string jsHtmlFile, string outputJSFile)
        {
            System.IO.File.Copy(jsHtmlFile, outputJSFile);
        }

        protected override void LogInfo(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        protected override void LogWarning(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        protected override void MinifyCss(string cssFile, string outputMinifiedCssFile)
        {
            System.IO.File.Copy(cssFile, outputMinifiedCssFile);
        }

        protected override void MinifyJavaScript(string javaScriptFile, string outputMinifiedJavaScriptFile)
        {
            System.IO.File.Copy(javaScriptFile, outputMinifiedJavaScriptFile);   
        }
    }
}
