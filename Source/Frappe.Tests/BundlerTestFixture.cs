using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Frappe
{
    [TestFixture]
    public class BundlerTestFixture
    {

        /// <summary>
        /// Test for the <see cref="Bundle.GetFiles"/>.
        /// </summary>
        [Test]
        public void GetFilesTest()
        {
            var bundler = new Bundler();
            var files = bundler.GetFiles(@"Examples\BundleA.bundle").ToList();
            Assert.IsNotNull(files);

            var expectedFiles = new List<string>() { 
                @"Examples\css\foo.css",
                @"Examples\css\goo.css",
                @"Examples\css\SubFolder\too.css",
                @"Examples\css\SubFolder\new.css",
                @"Examples\css\SubFolder\chew.css",
                @"Examples\Less\Local.less",
            }.ConvertAll(s => Path.GetFullPath(s).ToLower());

            Assert.AreEqual(expectedFiles, files.Select(f => f.ToLower()));
        }

        //[Test]
        //public void GetImportFilesTest()
        //{
        //    var bundler = new Bundler();
        //    var importFiles = bundler.GetImportFiles(@"Examples\BundleA.bundle").ToList();
        //    Assert.IsNotNull(importFiles);

        //    var expectedImportFiles = new List<string>() { 
        //        @"examples\Less\subfolder\subfolderlocal.less",
        //        @"examples\Less\include.less",
        //        @"examples\Less\includeinclude.less"
        //    }.Select(s => s.ToLower()).ToList();

        //    Assert.AreEqual(expectedImportFiles, importFiles.Select(file => file.Replace(Path.GetFullPath(@".\").ToLower(), "").ToLower()).ToList());
        //}

    }
}
