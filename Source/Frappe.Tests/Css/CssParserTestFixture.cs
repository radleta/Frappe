using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Frappe.Css
{
    /// <summary>
    /// Test fixture for <see cref="CssParser"/>.
    /// </summary>
    [TestFixture]
    public class CssParserTestFixture
    {

#if DEBUG
        public const string Configuration = "Debug";
#else
        public const string Configuration = "Release";
#endif

        /// <summary>
        /// Test for <see cref="CssParser.GetFileImports"/> method.
        /// </summary>
        [Test]
        public void GetFileImportsTest()
        {
            bool missingCalled = false;
            Assert.AreEqual(new List<string>() { 
                @"Examples\Css\Child.css", 
                @"Examples\Css\SubFolder\SubFolderChild.css"
            }, CssParser.GetFileImports(@"Examples\Css\Master.css", import => {
                Assert.IsNotNull(import);
                Assert.AreEqual("Examples\\Css\\SubFolder\\missing.css", import.ImportFile);
                Assert.AreEqual("@import \"missing.css\";", import.Statement);
                Assert.AreEqual("Examples\\Css\\SubFolder\\SubFolderChild.css", import.File);
                missingCalled = true;
            }).ToList());
            Assert.True(missingCalled, "Missing file import function not called.");
        }

        /// <summary>
        /// Test for <see cref="CssParser.GetFileImports"/> method.
        /// </summary>
        [Test]
        public void GetFileImportsLessTest()
        {
            var expectedFileImports = new List<string>() { 
                @"Examples\Less\SubFolder\SubFolderLocal.less", 
                @"Examples\Less\Include.less", 
                @"Examples\Less\IncludeInclude.less"
            };
            var actualFileImports = CssParser.GetFileImports(@"Examples\Less\Local.less").ToList();
            Assert.AreEqual(expectedFileImports, actualFileImports);
        }
        
        /// <summary>
        /// Test for <see cref="CssParser.GetCss"/> method.
        /// </summary>
        [Test]
        public void GetCssTest()
        {
            bool missingCalled = false;
            Assert.AreEqual("@import \"missing.css\";\r\n.subfolder-child\r\n{\r\n    color: #fff;\r\n}\r\n.child\r\n{\r\n    color: #fff;\r\n}\r\n.master\r\n{\r\n    color: #fff;\r\n}", CssParser.GetCss(@"Examples\Css\Master.css", true, import =>
            {
                Assert.IsNotNull(import);
                Assert.AreEqual("Examples\\Css\\SubFolder\\missing.css", import.ImportFile);
                Assert.AreEqual("@import \"missing.css\";", import.Statement);
                Assert.AreEqual("Examples\\Css\\SubFolder\\SubFolderChild.css", import.File);
                missingCalled = true;
            }));
            Assert.True(missingCalled, "Missing file import function not called.");
        }

        /// <summary>
        /// Test for <see cref="CssParser.UpdateRelativePaths"/> method.
        /// </summary>
        [Test]
        public void UpdateRelativePathsLessTest()
        {
            string exampleLessFile = @"Examples\Css\PathUpdate.less";
            var sourceLess = System.IO.File.ReadAllText(exampleLessFile);
            var less = CssParser.UpdateRelativePaths(sourceLess, System.IO.Path.GetDirectoryName(exampleLessFile), @"..\..\bundles");
            Assert.AreEqual(new List<string>() {
                "../bin/" + Configuration +"/Examples/Css/double-quote.png",
                "../bin/" + Configuration +"/Examples/Css/Images/double-quote.png?v=1",
                "../bin/" + Configuration +"/Examples/double-quote.png",
                "../bin/" + Configuration +"/Examples/Images/double-quote.png",
                "../bin/" + Configuration +"/Examples/Css/single-quote.png",
                "../bin/" + Configuration +"/Examples/Css/Images/single-quote.png",
                "../bin/" + Configuration +"/Examples/single-quote.png",
                "../bin/" + Configuration +"/Examples/Images/single-quote.png?v=1",
            }, CssParser.GetRelativePaths(less).ToList());
        }

        /// <summary>
        /// Test for <see cref="CssParser.UpdateRelativePaths"/> method.
        /// </summary>
        [Test]
        public void UpdateRelativePathsCssTest()
        {
            string exampleCssFile = @"Examples\Css\PathUpdate.css";
            var css = CssParser.UpdateRelativePaths(System.IO.File.ReadAllText(exampleCssFile), System.IO.Path.GetDirectoryName(exampleCssFile), @"..\..\bundles");
            Assert.AreEqual(new List<string>() {
                "../bin/" + Configuration +"/Examples/Css/double-quote.jpg",
                "../bin/" + Configuration +"/Examples/Css/Images/double-quote.jpg?v=1",
                "../bin/" + Configuration +"/Examples/double-quote.jpg",
                "../bin/" + Configuration +"/Examples/Images/double-quote.jpg",
                "../bin/" + Configuration +"/Examples/Css/single-quote.jpg",
                "../bin/" + Configuration +"/Examples/Css/Images/single-quote.jpg",
                "../bin/" + Configuration +"/Examples/single-quote.jpg",
                "../bin/" + Configuration +"/Examples/Images/single-quote.jpg?v=1",
                "../bin/" + Configuration +"/Examples/Css/no-quote.jpg",
                "../bin/" + Configuration +"/Examples/Css/Images/no-quote.jpg",
                "../bin/" + Configuration +"/Examples/no-quote.jpg",
                "../bin/" + Configuration +"/Examples/Images/no-quote.jpg?v=1",
            }, CssParser.GetRelativePaths(css).ToList());
        }

        //[Test]
        //public void UpdateRelativePathsTest1()
        //{
        //    //string file = @"..\..\..\..\Examples\Frappe.Web.Mvc4.Razor.Example\Content\bootstrap-3.0.3-dist\dist\css\bootstrap.min.css";
        //    //var paths = CssParser.GetRelativePaths(System.IO.File.ReadAllText(file));
        //    //foreach (var path in paths)
        //    //{
        //    //    Console.WriteLine("Path: {0}", path);
        //    //}
        //}
    }
}
