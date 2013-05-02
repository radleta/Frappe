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

        [SetUp]
        public void SetUp()
        {
            _fileCount = 0;
            _temporaryDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_temporaryDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            // clean up
            if (Directory.Exists(_temporaryDirectory))
                Directory.Delete(_temporaryDirectory, true);
        }

        private string _temporaryDirectory;
        private int _fileCount;

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

        [Test]
        public void BundleTest()
        {   
            // create bundle
            var bundle = CreateBundle();

            var bundleFile = new FileInfo(Path.Combine(_temporaryDirectory, Guid.NewGuid().ToString() + ".css.bundle"));
            var bundleOutputFile = new System.IO.FileInfo(bundleFile.FullName.Replace(".css.bundle", ".min.css"));
            Bundle.Serialize(bundle, bundleFile.FullName);

            // move the creation and last write back in time to simulate real world
            bundleFile.CreationTimeUtc = DateTime.UtcNow.AddHours(-1);
            bundleFile.LastWriteTimeUtc = bundleFile.CreationTimeUtc;

            Assert.False(bundleOutputFile.Exists);

            // bundle
            var bundler = new NUnitBundler();
            bundler.Bundle(bundleFile.FullName);

            // test to ensure the output file was created
            bundleOutputFile.Refresh();
            Assert.True(bundleOutputFile.Exists);
            var bundleOutputFileLastWriteUtc = bundleOutputFile.LastWriteTimeUtc;

            // create a new bundler and do it again
            bundler = new NUnitBundler();
            bundler.Bundle(bundleFile.FullName);

            // test to ensure the output file was created
            bundleOutputFile.Refresh();
            Assert.True(bundleOutputFile.Exists);

            // ensure it hasn't changed since nothing has changed
            Assert.AreEqual(bundleOutputFileLastWriteUtc, bundleOutputFile.LastWriteTimeUtc, "The file should not have changed.");

            // change the bundle file and do it again
            bundle.Includes.RemoveAt(0);
            Bundle.Serialize(bundle, bundleFile.FullName);

            // create a new bundler and do it again
            bundler = new NUnitBundler();
            bundler.Bundle(bundleFile.FullName);

            // test to ensure the output file was created
            bundleOutputFile.Refresh();
            Assert.True(bundleOutputFile.Exists);

            // ensure the output file HAS changed since we modified the bundle
            Assert.Greater(bundleOutputFile.LastWriteTimeUtc, bundleOutputFileLastWriteUtc, "The file should have changed.");
        }

        private Include CreateLessInclude()
        {
            return CreateInclude(@".less-test
{
    color: #" + _fileCount.ToString("000000") + @";
    .inner-class
    {
        color: #" + _fileCount.ToString("000000") + @";
    }
}", ".less");
        }

        private Include CreateCssInclude()
        {
            return CreateInclude(@".less-test
{
    color: #" + _fileCount.ToString("000000") + @";
}", ".css");
        }

        private Include CreateInclude(string content, string extension)
        {
            _fileCount++;

            var lessFile = new FileInfo(Path.Combine(_temporaryDirectory, Guid.NewGuid().ToString() + extension));
            File.WriteAllText(lessFile.FullName, content);

            // move the creation and last write back in time to simulate real world
            lessFile.CreationTimeUtc = DateTime.UtcNow.AddDays(-1 * _fileCount);
            lessFile.LastWriteTimeUtc = lessFile.CreationTimeUtc;

            var include = new Include();
            include.File = lessFile.FullName;
            return include;
        }

        private Bundle CreateBundle()
        {
            var bundle = new Bundle()
            {
                Includes = new List<Include>()
                {
                    CreateLessInclude(),
                    CreateCssInclude(),
                }
            };
            return bundle;
        }

    }
}
