﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Frappe
{
    /// <summary>
    /// Tests for the <see cref="Bundle"/>.
    /// </summary>
    [TestFixture]
    public class BundleTestFixture
    {
        /// <summary>
        /// Test for the <see cref="Bundle.Load"/>.
        /// </summary>
        [Test]
        public void LoadTest()
        {
            var bundle = Bundle.Load(@"Examples\BundleA.bundle");
            Assert.IsNotNull(bundle);
            Assert.IsNotNull(bundle.Includes);
            Assert.AreEqual(3, bundle.Includes.Count);
            Assert.AreEqual("foo.css", bundle.Includes[0].File);
            Assert.AreEqual("goo.css", bundle.Includes[1].File);
            Assert.AreEqual("SubFolder\\BundleB.bundle", bundle.Includes[2].File);
            Assert.IsTrue(bundle.Includes[2] is BundleInclude);
        }
    }
}
