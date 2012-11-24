using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Frappe
{
    /// <summary>
    /// A file included in a bundle.
    /// </summary>
    [XmlInclude(typeof(BundleInclude))]
    public class Include
    {
        /// <summary>
        /// The path to the file.
        /// </summary>
        [XmlAttribute]
        public string File { get; set; }
    }
}
