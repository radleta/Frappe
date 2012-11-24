using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Frappe
{
    /// <summary>
    /// A bundle of files.
    /// </summary>
    public class Bundle
    {
        /// <summary>
        /// Serializer for this class.
        /// </summary>
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(Bundle));

        /// <summary>
        /// Loads a bundle from a <c>file</c>.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>A bundle.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <c>file</c> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para>Thrown when <c>file</c> is <c>empty</c>.</para>
        /// <para>Thrown when <c>file</c> does not exist.</para>
        /// </exception>
        public static Bundle Load(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            else if (file == string.Empty)
            {
                throw new ArgumentOutOfRangeException("file", "Value cannot be empty.");
            }
            if (!System.IO.File.Exists(file))
            {
                throw new ArgumentOutOfRangeException("file", file, "Bundle file does not exist.");
            }

            var xml = System.IO.File.ReadAllText(file);

            // deserialize
            var bundle = Deserialize(xml);

            // denote where this bundle was loaded from
            bundle.File = Path.GetFullPath(file);

            return bundle;
        }

        /// <summary>
        /// Deserializes the <c>xml</c> into a <c>bundle</c>.
        /// </summary>
        /// <param name="xml">The xml of the bundle.</param>
        /// <returns>The bundle.</returns>
        internal static Bundle Deserialize(string xml)
        {
            using (var reader = new StringReader(xml))
            {
                return (Bundle)Serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// The file this bundle is stored in.
        /// </summary>
        /// <remarks>
        /// All relative paths in the bundle are relative to the folder of this file.
        /// </remarks>
        [XmlIgnore]
        public string File { get; set; }

        /// <summary>
        /// The output file of the bundle.
        /// </summary>
        public string OutputFile { get; set; }

        /// <summary>
        /// The files in this bundle.
        /// </summary>
        [XmlElement("Include", Type = typeof(Include))]
        [XmlElement("Bundle", Type = typeof(BundleInclude))]
        public List<Include> Includes { get; set; }
    }
}
