using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Web;
using System.Text.RegularExpressions;

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
        /// Matches the extension of the bundle.
        /// </summary>
        private static Regex BundleFileRegex = new Regex(@"(?'Name'.+?)(?'TypeExt'\.(?:css|js))(?:\.bundle)$", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
                        
        /// <summary>
        /// Gets the urls to the bundle for the <c>context</c>.
        /// </summary>
        /// <param name="bundleAppPath">The app path to the bundle. The app path starts with '~/' and is relative to the web app.</param>
        /// <param name="context">The context of the urls. Defaults to the <see cref="System.Web.HttpContext.Current"/>.</param>
        /// <returns>The urls to the file or files within the bundle.</returns>
        public static List<string> GetUrls(string bundleAppPath, System.Web.HttpContextBase context = null)
        {
            var urls = new List<string>();

            // ensure we have the current context
            context = context ?? new System.Web.HttpContextWrapper(System.Web.HttpContext.Current);
            var isSecureRequest = context.Request.IsSecureConnection;
            var server = context.Server;
                
            // ensure the bundle exists
            var bundleFile = server.MapPath(bundleAppPath);
            if (!System.IO.File.Exists(bundleFile))
            {
                throw new System.IO.FileNotFoundException("The bundle file could not be found. The bundle file must exist.", bundleFile);
            }

            // build the root url
            var rootUrl = "";
            if (Settings.Default.AbsolutePath
                || !string.IsNullOrWhiteSpace(Settings.Default.CdnHostName))
            {
                // IF path should be absolute
                //   OR we are using a CDN
                // THEN we're always going to return an absolute url

                if (Settings.Default.ProtocolRelativeUrl)
                {
                    rootUrl = "//";
                }
                else if (isSecureRequest
                    || Settings.Default.ForceSecureUrl)
                {
                    rootUrl = "https://";
                }
                else
                {
                    rootUrl = "http://";
                }

                // determine whether or not to append the cdn host name
                if (!string.IsNullOrWhiteSpace(Settings.Default.CdnHostName))
                {
                    rootUrl += Settings.Default.CdnHostName;
                }
                else
                {
                    rootUrl += context.Request.Url.Host;
                }
            }

            if (Settings.Default.BundleOutput)
            {
                // get the output file
                var bundleOutput = BundleFileRegex.Replace(bundleAppPath, @"${Name}.min${TypeExt}");
                var bundleOutputFile = server.MapPath(bundleOutput);

                if (!System.IO.File.Exists(bundleOutputFile))
                {
                    throw new System.IO.FileNotFoundException(string.Format("The output file could not be found. The output file for the bundle must exist. Bundle: {0}", bundleFile), bundleOutputFile);
                }

                // create the url to the bundle output file
                var bundleOutputUrl = rootUrl + VirtualPathUtility.ToAbsolute(bundleOutput + "?v=" + System.IO.File.GetLastWriteTimeUtc(bundleOutputFile).ToString("yyyyMMddHHmmssfff"));

                urls.Add(bundleOutputUrl);                
            }
            else
            {
                var bundler = new Bundler();
                var webRootDir = server.MapPath("~/").ToLower();
                var webRootUrlPath = VirtualPathUtility.ToAbsolute("/");
                
                foreach (var includeFile in bundler.GetFiles(bundleFile))
                {
                    if (!System.IO.File.Exists(includeFile))
                    {
                        throw new System.IO.FileNotFoundException(string.Format("A include file could not be found. All the files in the bundle must exist. Bundle: {0}", bundleFile), includeFile);
                    }

                    // create the url to the include file
                    var includeUrl = rootUrl + webRootUrlPath + includeFile.ToLower().Replace(webRootDir, "").Replace("\\", "/") + "?v=" + System.IO.File.GetLastWriteTimeUtc(includeFile).ToString("yyyyMMddHHmmssfff");

                    urls.Add(includeUrl);
                }
            }
            return urls;
        }
        
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
        /// Serializes the <c>bundle</c> into a <c>file</c>.
        /// </summary>
        /// <param name="bundle">The bundle to serialize.</param>
        /// <param name="file">The file to store the serialized <c>bundle</c>.</param>
        internal static void Serialize(Bundle bundle, string file)
        {
            using (var writer = System.IO.File.Create(file))
            {
                Serializer.Serialize(writer, bundle);
            }
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

        /// <summary>
        /// Get the output file for the bundle.
        /// </summary>
        /// <returns>The output file for the bundle.</returns>
        public string GetOutputFile()
        {
            if (File == null)
            {
                throw new ArgumentNullException("File");
            }
            else if (File == string.Empty)
            {
                throw new ArgumentOutOfRangeException("File", "Value cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(OutputFile))
            {
                return BundleFileRegex.Replace(File, @"${Name}.min${TypeExt}");
            }
            else
            {
                if (Path.IsPathRooted(OutputFile))
                {
                    return OutputFile;
                }
                else
                {
                    return Path.Combine(Path.GetDirectoryName(Path.GetFullPath(File)), OutputFile);
                }
            }
        }
    }
}
