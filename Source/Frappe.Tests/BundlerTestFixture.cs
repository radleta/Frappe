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
                @"Examples\foo.css",
                @"Examples\goo.css",
                @"Examples\SubFolder\too.css",
                @"Examples\SubFolder\new.css",
                @"Examples\SubFolder\chew.css",
            }.ConvertAll(s => s.ToLower());

            Assert.AreEqual(expectedFiles, files.ConvertAll(file => file.Replace(Path.GetFullPath(@".\").ToLower(), "")));
        }

    }
}
