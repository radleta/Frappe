using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;
using System.Web.Hosting;
using System.Xml;

namespace Frappe.Sprites
{

    /// <summary>
    /// Automates the creation of sprites and base64 inlining for CSS
    /// </summary>
    public class SpriteGenerator
    {
        private static SpriteGenerator __default;
        public static SpriteGenerator Default
        {
            get
            {
                if (__default == null)
                {
                    __default = new SpriteGenerator(HostingEnvironment.MapPath("~"));
                }
                return __default;
            }
        }
        
        public const string TimestampFileName = "timeStamp.dat";
        public const string MD5HashFileName = "hash.dat";
        public const string SettingsFileName = "settings.xml";
        public const string HighCompatibilityCssFileNameFormat = "highCompat-{0}.css";
        public const string HighCompatibilityCssFileNameRegex = @"^highCompat\-.*\.css$";
        public const string LowCompatibilityCssFileNameFormat = "lowCompat-{0}.css";
        public const string LowCompatibilityCssFileNameRegex = @"^lowCompat\-.*\.css$";
        public const string BlankFileName = "blank.gif";
        public const string SpriteDirectoryRelativePath = "~/App_Sprites/";
        public const string GeneratedSpriteFileName = "sprite{0}-{1}";
        public const string SpriteFileNameRegex = @"^sprite[0-9]+\-.+$";
        public const bool SupportIE8MaximalInlineSize = true;
        private static readonly List<string> _extensionsLower = new List<string>() { ".jpg", ".gif", ".png", ".bmp", "*.jpeg" };
        private readonly object _lockObj = new object();
        private const string TransparentGif = "R0lGODlhAQABAIABAP///wAAACH5BAEKAAEALAAAAAABAAEAAAICTAEAOw==";
        public const string InlinedTransparentGif = "data:image/gif;base64," + TransparentGif;
        private const int IE8MaximalInlineSize = 32768;

        public SpriteGenerator(string webSiteRootDirectory)
        {
            if (webSiteRootDirectory == null)
            {
                throw new ArgumentNullException(nameof(webSiteRootDirectory));
            }
            else if (webSiteRootDirectory == string.Empty)
            {
                throw new ArgumentOutOfRangeException(nameof(webSiteRootDirectory), "Value cannot be empty.");
            }

            _webSiteRootDirectory = webSiteRootDirectory;
        }

        protected virtual bool IsLoggingEnabled { get; }

        protected virtual void LogMessage(string message)
        {
        }

        protected virtual void LogError(string message)
        {
        }

        private readonly string _webSiteRootDirectory;

        public bool Initialized { get; set; }

        /// <summary>
        /// Ensures the ImageOptimizations has been initialized.
        /// </summary>
        public void EnsureInitialized()
        {
            if (!this.Initialized)
            {
                throw new InvalidOperationException(ImageOptimizationResources.InitializationErrorMessage);
            }
        }

        /// <summary>
        /// Makes the appropriate CSS ID name for the sprite to be used.
        /// </summary>
        /// <param name="pathToImage">The path to the image</param>
        /// <param name="pathToSpriteDirectory">The path to the directory used to store sprites, used if the path to the image was not relative to the sprites directory</param>
        /// <returns>The CSS class used to reference the optimized image</returns>
        public string MakeCssClassName(string pathToImage, string pathToSpriteDirectory = null)
        {
            if (pathToImage == null)
            {
                throw new ArgumentNullException("pathToImage");
            }

            string cssFilename = TrimPathForCss(pathToImage, pathToSpriteDirectory);
            return CssNameHelper.GenerateClassName(cssFilename);
        }

        public string GetBlankImageSource(HttpBrowserCapabilitiesBase browser)
        {
            if (browser == null)
            {
                return null;
            }

            if (browser.Type.ToUpperInvariant().Contains("IE") && browser.MajorVersion <= 7)
            {
                return FixVirtualPathSlashes(Path.Combine(SpriteDirectoryRelativePath, BlankFileName));
            }

            return InlinedTransparentGif;
        }

        /// <summary>
        /// Returns the name of the CSS file containing the best compatibility settings for the user's browser. Returns null if the browser does not support any optimizations. 
        /// </summary>
        /// <param name="browser">The HttpBrowserCapabilities object for the user's browser</param>
        /// <returns>The name of the correct CSS file, or Null if not supported</returns>
        public string LinkCompatibleCssFileName(HttpBrowserCapabilitiesBase browser, string path)
        {
            if (browser == null)
            {
                return null;
            }

            // get the computed md5 for the virtual path
            string computedMD5Hash = SpriteGenerator.Default.GetCssMD5(path);
            if (string.IsNullOrEmpty(computedMD5Hash))
                // if it doesn't exist then return null
                return null;

            if (browser.Type.ToUpperInvariant().Contains("IE"))
            {
                if (browser.MajorVersion < 6)
                {
                    return null;
                }
                else if (browser.MajorVersion <= 7)
                {
                    return SpriteGenerator.Default.CreateLowCompatibilityCssFileName(computedMD5Hash);
                }
            }
            else if (browser.Type.ToUpperInvariant().Contains("FIREFOX"))
            {
                if (browser.MajorVersion < 2)
                {
                    return SpriteGenerator.Default.CreateLowCompatibilityCssFileName(computedMD5Hash);
                }
            }

            return SpriteGenerator.Default.CreateHighCompatibilityCssFileName(computedMD5Hash);
        }

        public string CreateHighCompatibilityCssFileName(string md5Hash)
        {
            return string.Format(CultureInfo.InvariantCulture, HighCompatibilityCssFileNameFormat, md5Hash);
        }

        private bool IsHighCompatibilityCssFile(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                return Regex.IsMatch(Path.GetFileName(path), HighCompatibilityCssFileNameRegex, RegexOptions.IgnoreCase); ;
            }
            else
            {
                return false;
            }
        }

        public string CreateLowCompatibilityCssFileName(string md5Hash)
        {
            return string.Format(CultureInfo.InvariantCulture, LowCompatibilityCssFileNameFormat, md5Hash);
        }

        private bool IsLowCompatibilityCssFile(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                return Regex.IsMatch(Path.GetFileName(path), LowCompatibilityCssFileNameRegex, RegexOptions.IgnoreCase); ;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if an image (passed by path or image name) is a sprite image or CSS file created by the framework
        /// </summary>
        /// <param name="path">The path or filename of the image in question</param>
        /// <returns>True if the image is a sprite, false if it is not</returns>
        public bool IsOutputSprite(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            return Regex.IsMatch(name, SpriteFileNameRegex, RegexOptions.IgnoreCase);
        }

        internal string MakeCssSelector(string pathToImage, string pathToSpriteDirectory = null)
        {
            string cssFilename = TrimPathForCss(pathToImage, pathToSpriteDirectory);
            return CssNameHelper.GenerateSelector(cssFilename);
        }

        /// <summary>
        /// Rebuilds the cache / dependencies for all subdirectories below the specified directory
        /// </summary>
        /// <param name="path">The root directory for the cache rebuild (usually app_sprites)</param>
        /// <param name="rebuildImages">Indicate whether the directories should be rebuilt as well</param>
        public List<string> ProcessDirectories(string spriteRootDirectory)
        {
            List<string> dependencies = new List<string>();
            List<string> spriteDirectories = Directory.GetDirectories(spriteRootDirectory, "*", SearchOption.AllDirectories).ToList();
            spriteDirectories.Add(spriteRootDirectory);
            foreach (string spriteDirectory in spriteDirectories)
            {
                dependencies.AddRange(ProcessDirectory(spriteDirectory));
            }
            return dependencies;
        }

        /// <summary>
        /// Executes the image optimizer on a specific subdirectory of the root image directory (non-recursive)
        /// </summary>
        /// <param name="path">The path to the directory to be rebuilt</param>
        /// <param name="checkIfFilesWereModified">Indicate whether the directory should only be rebuilt if files were modified</param>
        /// <returns>A list of the files which are dependencies of the cache.</returns>
        public List<string> ProcessDirectory(string spriteDirectory)
        {
            // Check if directory was deleted
            if (!Directory.Exists(spriteDirectory))
            {
                return new List<string>(0);
            }

            var dependencies = new List<string>();
            // add the current directory as a dependency so if any files are added the directory will get reprocessed
            dependencies.Add(spriteDirectory);

            var hashedFiles = new List<string>();

            // get the settings file so we can add it to the hashed files
            string settingsFile = LocateSettingFile(spriteDirectory);
            if (File.Exists(settingsFile))
            {
                hashedFiles.Add(settingsFile);

                if (IsLoggingEnabled) LogMessage($"Loaded sprite settings from \"{settingsFile}\" for directory \"{spriteDirectory}\".");
            }

            var allFiles = Directory.GetFiles(spriteDirectory);
            var generatedFiles = new List<string>();
            var imageFiles = new List<string>();
            foreach (var file in allFiles)
            {
                if (IsGeneratedFile(file))
                {
                    generatedFiles.Add(file);
                    hashedFiles.Add(file);
                }
                else if (IsImageFile(file))
                {
                    imageFiles.Add(file);
                    hashedFiles.Add(file);

                    if (IsLoggingEnabled) LogMessage($"Found image \"{file}\" to sprite.");
                }
            }


            // Make sure to not include the hash file
            string md5HashFile = GetMD5HashFile(spriteDirectory);
            string currentDirectoryMD5 = ComputeHash(hashedFiles);
            string savedDirectoryMD5 = GetDirectoryMD5(spriteDirectory);
            if (savedDirectoryMD5 == currentDirectoryMD5)
            {
                // add all the hashed files to the dependencies so if any change we can reload
                dependencies.AddRange(hashedFiles);

                return dependencies;
            }

            // Import settings from settings file
            ImageSettings settings = GetSettings(settingsFile);

            // delete the generated files
            generatedFiles.ForEach(f => File.Delete(f));

            // Create pointer to the CSS output file
            lock (_lockObj)
            {
                string newCssMD5 = ComputeHash(imageFiles);
                string highCompCssFile = Path.Combine(spriteDirectory, CreateHighCompatibilityCssFileName(newCssMD5));
                string lowCompCssFile = Path.Combine(spriteDirectory, CreateLowCompatibilityCssFileName(newCssMD5));

                DateTime mostRecentCreationTimeUtc = DateTime.MinValue;
                DateTime mostRecentLastWriteTimeUtc = DateTime.MinValue;
                using (TextWriter cssHighCompatOutput = new StreamWriter(highCompCssFile, append: false),
                                  cssLowCompatOutput = new StreamWriter(lowCompCssFile, append: false))
                {
                    PerformOptimizations(spriteDirectory, settings, cssHighCompatOutput, cssLowCompatOutput, imageFiles, ref mostRecentCreationTimeUtc, ref mostRecentLastWriteTimeUtc);

                    // Merge with a user's existing CSS file(s)
                    MergeExternalCss(spriteDirectory, cssHighCompatOutput, cssLowCompatOutput, ref mostRecentCreationTimeUtc, ref mostRecentLastWriteTimeUtc);
                }
                if (IsLoggingEnabled) LogMessage($"Saved sprite high compatiability CSS to \"{highCompCssFile}\".");
                if (IsLoggingEnabled) LogMessage($"Saved sprite low compatiability CSS to \"{lowCompCssFile}\".");

                // look at the directory and get all the files we're going to make the new hash from
                // since the directory now contains all the image files and generated files
                hashedFiles = Directory.GetFiles(spriteDirectory).Where(f => IsGeneratedFile(f) || IsImageFile(f)).ToList();

                // add the settings file to the hashed files
                if (File.Exists(settingsFile))
                {
                    hashedFiles.Add(settingsFile);
                }

                // re-compute the hash now that we've generated all the files
                string newDirectoryMD5 = ComputeHash(hashedFiles);

                // add the hashedFiles to the dependencies since we've not update it properly
                dependencies.AddRange(hashedFiles);

                // write the md5s to the hash file
                File.WriteAllLines(md5HashFile, new string[] { newCssMD5, newDirectoryMD5 });
                if (IsLoggingEnabled) LogMessage($"Saved CSS MD5 hash \"{newCssMD5}\" and directory MD5 has \"{newDirectoryMD5}\" to \"{md5HashFile}\".");

                // ensure the modify date is after or equal to the create date
                if (mostRecentCreationTimeUtc > mostRecentLastWriteTimeUtc)
                {
                    mostRecentLastWriteTimeUtc = mostRecentCreationTimeUtc;
                }

                // set the output files to the correct time
                // this ensures cached items relying on these times
                // stay in sync and only change which the underlying
                // is modified
                File.SetCreationTimeUtc(md5HashFile, mostRecentCreationTimeUtc);
                File.SetLastWriteTimeUtc(md5HashFile, mostRecentLastWriteTimeUtc);
                hashedFiles.ForEach(f =>
                {
                    File.SetCreationTimeUtc(f, mostRecentCreationTimeUtc);
                    File.SetLastWriteTimeUtc(f, mostRecentLastWriteTimeUtc);
                });
            }

            return dependencies;
        }

        private bool IsGeneratedFile(string file)
        {
            return IsOutputSprite(file)
                    || IsHighCompatibilityCssFile(file)
                    || IsLowCompatibilityCssFile(file);
        }

        private bool IsImageFile(string file)
        {
            return _extensionsLower.Contains(Path.GetExtension(file).ToLowerInvariant());
        }

        internal string SaveBlankFile(string path)
        {
            string blankFileFullPath = Path.Combine(path, BlankFileName);
            if (!File.Exists(blankFileFullPath))
            {
                using (FileStream blankFile = new FileStream(blankFileFullPath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    byte[] data = Convert.FromBase64String(TransparentGif);
                    blankFile.Write(data, 0, data.Length);
                }
            }
            return blankFileFullPath;
        }

        // Copied from \ndp\fx\src\xsp\System\Web\Util\UrlPath.cs
        // Change backslashes to forward slashes, and remove duplicate slashes
        internal string FixVirtualPathSlashes(string virtualPath)
        {
            // Make sure we don't have any back slashes
            virtualPath = virtualPath.Replace('\\', '/');

            // Replace any double forward slashes
            for (; ; )
            {
                string newPath = virtualPath.Replace("//", "/");

                // If it didn't do anything, we're done
                if ((object)newPath == (object)virtualPath)
                    break;

                virtualPath = newPath;
            }

            return virtualPath;
        }

        private string TrimPathForCss(string pathToImage, string pathToSpriteDirectory)
        {
            pathToSpriteDirectory = pathToSpriteDirectory ?? SpriteGenerator.SpriteDirectoryRelativePath;
            pathToImage = MakePathRelative(pathToImage, pathToSpriteDirectory);

            return pathToImage.Replace('\\', '/');
        }

        private void MergeExternalCss(string path, TextWriter cssHighCompatOutput, TextWriter cssLowCompatOutput, ref DateTime mostRecentCreationTimeUtc, ref DateTime mostRecentLastWriteTimeUtc)
        {
            string[] extraCssFiles = Directory.GetFiles(path, "*.css");

            foreach (string cssFile in extraCssFiles)
            {
                if (IsHighCompatibilityCssFile(cssFile) || IsLowCompatibilityCssFile(cssFile))
                {
                    continue;
                }

                using (TextReader cssRead = new StreamReader(cssFile))
                {
                    string textToBeCopied = cssRead.ReadToEnd();

                    cssHighCompatOutput.Write(textToBeCopied);
                    cssLowCompatOutput.Write(textToBeCopied);
                }

                // ensure the create and modify date time stamps are 
                var creationTimeUtc = File.GetCreationTimeUtc(cssFile);
                if (mostRecentCreationTimeUtc < creationTimeUtc)
                {
                    mostRecentCreationTimeUtc = creationTimeUtc;
                }
                var lastWriteTimeUtc = File.GetLastWriteTimeUtc(cssFile);
                if (mostRecentLastWriteTimeUtc < lastWriteTimeUtc)
                {
                    mostRecentLastWriteTimeUtc = lastWriteTimeUtc;
                }
            }
        }

        /// <summary>
        /// Reads the timestamps of all of the files within a directory, and outputs them in a single sorted string. Used to determine if changes have occured to a directory upon application start.
        /// </summary>
        /// <param name="path">The path to the directory</param>
        /// <returns>A sorted string containing all filenames and last modified timestamps</returns>
        private string ComputeHash(List<string> fileLocations)
        {
            // get all the 
            var bytes = new List<byte>();
            foreach (string file in fileLocations)
            {
                bytes.AddRange(File.ReadAllBytes(file));
            }

            if (bytes.Count > 0)
            {
                // IF we have any bytes
                // THEN lets create one md5 hash of all bytes
                // BECAUSE we want a single md5 to represent them all
                var md5Hasher = System.Security.Cryptography.MD5.Create();
                var md5HashOfAllMD5s = md5Hasher.ComputeHash(bytes.ToArray());
                // Use HttpServerUtility.UrlTokenEncode so it's url safe
                return HttpServerUtility.UrlTokenEncode(md5HashOfAllMD5s);
            }
            else
            {
                return string.Empty;
            }
        }

        public string GetDirectoryMD5(string path)
        {
            var lines = GetMD5HashFileLines(path);
            if (lines != null)
                return GetMD5HashFileLines(path).ElementAtOrDefault(1) ?? string.Empty;
            else
                return string.Empty;
        }

        public string GetCssMD5(string path)
        {
            var lines = GetMD5HashFileLines(path);
            if (lines != null)
                return GetMD5HashFileLines(path).ElementAtOrDefault(0) ?? string.Empty;
            else
                return string.Empty;
        }

        private string[] GetMD5HashFileLines(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }
            string md5HashFile = GetMD5HashFile(path);
            string cacheKey = string.Format("MD5HashFileLines_{0}", md5HashFile);
            string[] md5HashFileLines = (string[])HttpRuntime.Cache[cacheKey];
            if (md5HashFileLines == null)
            {
                if (File.Exists(md5HashFile))
                {
                    md5HashFileLines = File.ReadAllLines(md5HashFile);
                    HttpRuntime.Cache.Add(cacheKey, md5HashFileLines, new CacheDependency(md5HashFile), System.Web.Caching.Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(15), CacheItemPriority.Default, null);
                }
            }
            return md5HashFileLines;
        }

        private string GetMD5HashFile(string path)
        {
            return Path.Combine(path, MD5HashFileName);
        }

        /// <summary>
        /// Checks if the image at the path is a sprite generated by the framework, and deletes it if it was
        /// </summary>
        /// <param name="path">The file path to the image in question</param>
        /// <returns>True if the image was a sprite (and was by extension, deleted)</returns>
        private bool DeleteSpriteFile(string path)
        {
            if (IsOutputSprite(path))
            {
                File.Delete(path);
                return true;
            }

            return false;
        }

        private void PerformOptimizations(string path, ImageSettings settings, TextWriter cssHighCompatOutput, TextWriter cssLowCompatOutput, List<string> imageLocations, ref DateTime mostRecentCreationTimeUtc, ref DateTime mostRecentLastWriteTimeUtc)
        {
            // Create a list containing each image (in Bitmap format), and calculate the total size (in pixels) of final image
            int x = 0;
            int y = 0;
            int imageIndex = 0;
            long size = 0;
            int spriteNumber = 0;
            List<Bitmap> images = new List<Bitmap>();
            try
            {
                foreach (string imagePath in imageLocations)
                {
                    var imageFile = new FileInfo(imagePath);

                    // If the image is growing above the specified max file size, make the sprite with the existing images
                    // and add the new image to the next sprite list
                    if ((imageIndex > 0) && IsSpriteOversized(settings.MaxSize, size, imagePath))
                    {
                        GenerateSprite(path, settings, x, y, spriteNumber, images, cssHighCompatOutput, cssLowCompatOutput);

                        // Clear the existing images
                        foreach (Bitmap image in images)
                        {
                            image.Dispose();
                        }

                        // Reset variables to initial values, and increment the spriteNumber
                        images.Clear();
                        x = 0;
                        y = 0;
                        imageIndex = 0;
                        size = 0;
                        spriteNumber++;
                    }

                    // Add the current image to the list of images that are to be processed
                    images.Add(new Bitmap(imagePath));

                    // Use the image tag to store its name
                    images[imageIndex].Tag = MakeCssSelector(imagePath, SpriteDirectoryRelativePath);

                    // Find the total pixel size of the sprite based on the tiling direction
                    if (settings.TileInYAxis)
                    {
                        y += images[imageIndex].Height;
                        if (x < images[imageIndex].Width)
                        {
                            x = images[imageIndex].Width;
                        }
                    }
                    else
                    {
                        x += images[imageIndex].Width;
                        if (y < images[imageIndex].Height)
                        {
                            y = images[imageIndex].Height;
                        }
                    }

                    // Update the filesize size of the bitmap list
                    size += imageFile.Length;

                    // ensure the create and modify date time stamps are the most recent
                    if (mostRecentCreationTimeUtc < imageFile.CreationTimeUtc)
                    {
                        mostRecentCreationTimeUtc = imageFile.CreationTimeUtc;
                    }
                    if (mostRecentLastWriteTimeUtc < imageFile.LastWriteTimeUtc)
                    {
                        mostRecentLastWriteTimeUtc = imageFile.LastWriteTimeUtc;
                    }

                    imageIndex++;
                }

                // Merge the final list of bitmaps into a sprite
                if (imageIndex != 0)
                {
                    GenerateSprite(path, settings, x, y, spriteNumber, images, cssHighCompatOutput, cssLowCompatOutput);
                }
            }
            finally
            {
                // Close the CSS file and clear the images list
                foreach (Bitmap image in images)
                {
                    image.Dispose();
                }
            }
        }

        private bool IsSpriteOversized(int maxSize, long spriteSize, string imagePath)
        {
            // Estimate the size of the sprite after adding the current image
            long nextSize = spriteSize + new FileInfo(imagePath).Length;

            // Determine of the size is too large
            return nextSize > (1024 * maxSize);
        }

        private string MapPath(string path)
        {
            if (path.IndexOf("~", StringComparison.OrdinalIgnoreCase) == 0)
            {
                path = System.IO.Path.Combine(_webSiteRootDirectory, path.TrimStart('~').Replace('/', '\\'));
            }
            
            return path;
        }

        private string MakePathRelative(string fullPath, string pathToRelativeRoot)
        {
            fullPath = MapPath(fullPath);
            pathToRelativeRoot = MapPath(pathToRelativeRoot);

            fullPath = GetTrimmedPath(fullPath);
            pathToRelativeRoot = GetTrimmedPath(pathToRelativeRoot);

            if (fullPath.ToUpperInvariant().Contains(pathToRelativeRoot.ToUpperInvariant()))
            {
                return fullPath.Remove(0, fullPath.IndexOf(pathToRelativeRoot, StringComparison.OrdinalIgnoreCase) + pathToRelativeRoot.Length + 1);
            }
            else
            {
                return fullPath;
            }
        }

        private class CssImageInfo
        {
            public int XOffset { get; set; }
            public int YOffset { get; set; }
            public Bitmap Image { get; set; }
        }
        
        private void GenerateSprite(string path, ImageSettings settings, int x, int y, int spriteNumber, List<Bitmap> images, TextWriter cssHighCompatOutput, TextWriter cssLowCompatOutput)
        {
            // Create a drawing surface and add the images to it
            // Since we'll be padding each image by 1px later on, we need to increase the sprites's size correspondingly.
            if (settings.TileInYAxis)
            {
                y += images.Count;
            }
            else
            {
                x += images.Count;
            }

            using (Bitmap sprite = new Bitmap(x, y))
            {
                using (Graphics drawingSurface = Graphics.FromImage(sprite))
                {

                    // Set the background to the specs from the settings file
                    drawingSurface.Clear(settings.BackgroundColor);

                    // Make the final sprite and save it
                    int xOffset = 0;
                    int yOffset = 0;
                    var cssImages = new List<CssImageInfo>();
                    foreach (Bitmap image in images)
                    {
                        drawingSurface.DrawImage(image, new Rectangle(xOffset, yOffset, image.Width, image.Height));

                        cssImages.Add(new CssImageInfo()
                        {
                            XOffset = xOffset,
                            YOffset = yOffset,
                            Image = image,
                        });

                        if (settings.TileInYAxis)
                        {
                            // pad each image in the sprite with a 1px margin
                            yOffset += image.Height + 1;
                        }
                        else
                        {
                            // pad each image in the sprite with a 1px margin
                            xOffset += image.Width + 1;
                        }
                    }

                    // Set the encoder parameters and make the image
                    string spriteMD5;
                    using (var spriteMemoryStream = new System.IO.MemoryStream())
                    {
                        string spriteFileExt;
                        try
                        {
                            using (EncoderParameters spriteEncoderParameters = new EncoderParameters(1))
                            {
                                spriteEncoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, settings.Quality);

                                // Attempt to save the image to disk with the specified encoder
                                spriteFileExt = settings.Format;
                                sprite.Save(spriteMemoryStream, GetEncoderInfo(settings.Format), spriteEncoderParameters);
                            }
                        }
                        catch (Exception)
                        {
                            // If errors occur, get the CLI to auto-choose an encoder. Unfortunately this means that the quality settings will be not used.
                            try
                            {
                                spriteFileExt = settings.Format;
                                sprite.Save(spriteMemoryStream, GetImageFormat(settings.Format));
                            }
                            catch (Exception)
                            {
                                // If errors occur again, try to save as a png
                                spriteFileExt = "png";
                                sprite.Save(spriteMemoryStream, ImageFormat.Png);
                            }
                        }

                        // create MD5 of the memory stream

                        // reset the stream to the beginning
                        spriteMemoryStream.Position = 0;

                        // create MD5 of the memory stream
                        // Use HttpServerUtility.UrlTokenEncode so it's url safe
                        spriteMD5 = HttpServerUtility.UrlTokenEncode(System.Security.Cryptography.MD5.Create().ComputeHash(spriteMemoryStream));

                        // reset the stream to the beginning
                        spriteMemoryStream.Position = 0;

                        // save to final file
                        var spriteFile = Path.Combine(path, GenerateSpriteFileName(spriteNumber, spriteMD5, settings.Format));
                        File.WriteAllBytes(spriteFile, spriteMemoryStream.ToArray());
                        
                        if (IsLoggingEnabled) LogMessage($"Saved sprite image \"{spriteFile}\".");
                    }

                    foreach (var cssImage in cssImages)
                    {
                        // Add the CSS data
                        GenerateCss(cssImage.XOffset, cssImage.YOffset, spriteNumber, spriteMD5, settings.Format, settings.Base64, cssImage.Image, cssHighCompatOutput);
                        GenerateCss(cssImage.XOffset, cssImage.YOffset, spriteNumber, spriteMD5, settings.Format, false, cssImage.Image, cssLowCompatOutput);
                    }
                }
            }
        }

        private string GenerateSpriteFileName(int spriteNumber, string spriteMD5, string fileExtension)
        {
            return String.Format(CultureInfo.InvariantCulture, GeneratedSpriteFileName, spriteNumber, spriteMD5) + "." + fileExtension;
        }

        private void GenerateCss(int xOffset, int yOffset, int spriteNumber, string spriteMD5, string fileExtension, bool base64, Bitmap image, TextWriter cssOutput)
        {
            cssOutput.WriteLine("." + (string)image.Tag);
            cssOutput.WriteLine("{");
            cssOutput.WriteLine("width:" + image.Width.ToString(CultureInfo.InvariantCulture) + "px;");
            cssOutput.WriteLine("height:" + image.Height.ToString(CultureInfo.InvariantCulture) + "px;");

            if (base64)
            {
                string base64Image = ConvertImageToBase64(image, GetImageFormat(fileExtension));
                if (SupportIE8MaximalInlineSize && base64Image.Length > IE8MaximalInlineSize)
                {
                    GenerateCssBackgroundLow(cssOutput, fileExtension, spriteNumber, spriteMD5, xOffset, yOffset);
                }
                else
                {
                    GenerateCssBackgroundHigh(cssOutput, fileExtension, base64Image);
                }
            }
            else
            {
                GenerateCssBackgroundLow(cssOutput, fileExtension, spriteNumber, spriteMD5, xOffset, yOffset);
            }

            cssOutput.WriteLine("}");
        }

        private void GenerateCssBackgroundHigh(TextWriter cssOutput, string fileExtension, string base64Image)
        {
            cssOutput.WriteLine("background:url(data:image/" + fileExtension + ";base64," + base64Image + ") no-repeat 0% 0%;");
        }

        private string GetOffsetPosition(int offset)
        {
            // Offset of 0 doesn't need to have the minus sign
            if (offset == 0)
            {
                return offset.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                return "-" + offset.ToString(CultureInfo.InvariantCulture);
            }
        }

        private void GenerateCssBackgroundLow(TextWriter cssOutput, string fileExtension, int spriteNumber, string spriteMD5, int xOffset, int yOffset)
        {
            string xPosition = GetOffsetPosition(xOffset);
            string yPosition = GetOffsetPosition(yOffset);

            cssOutput.WriteLine("background-image:url(" + GenerateSpriteFileName(spriteNumber, spriteMD5, fileExtension) + ");");
            cssOutput.WriteLine("background-position:" + xPosition + "px " + yPosition + "px;");
        }

        private string ConvertImageToBase64(Bitmap image, ImageFormat format)
        {
            string base64;
            using (MemoryStream memory = new MemoryStream())
            {
                image.Save(memory, format);
                base64 = Convert.ToBase64String(memory.ToArray());
            }

            return base64;
        }

        private ImageFormat GetImageFormat(string fileExtension)
        {
            switch (fileExtension.ToUpperInvariant())
            {
                case "JPG":
                case "JPEG":
                    return ImageFormat.Jpeg;

                case "GIF":
                    return ImageFormat.Gif;

                case "PNG":
                    return ImageFormat.Png;

                case "BMP":
                    return ImageFormat.Bmp;

                default:
                    return ImageFormat.Png;
            }
        }

        private string LocateSettingFile(string path)
        {
            DirectoryInfo settingsDir = new DirectoryInfo(GetTrimmedPath(path));
            if (settingsDir.Exists)
            {
                var settingsFile = Path.Combine(settingsDir.FullName, SettingsFileName);
                if (File.Exists(settingsFile))
                {
                    return settingsFile;
                }

                var rootWebSiteDir = new DirectoryInfo(GetTrimmedPath(_webSiteRootDirectory));
                var rootSpritesDir = new DirectoryInfo(GetTrimmedPath(MapPath(SpriteDirectoryRelativePath)));
                if (settingsDir.FullName.StartsWith(rootSpritesDir.FullName, StringComparison.InvariantCultureIgnoreCase))
                {
                    settingsDir = settingsDir.Parent;
                    while (settingsDir != null
                        && settingsDir.Exists)
                    {
                        settingsFile = Path.Combine(settingsDir.FullName, SettingsFileName);
                        if (File.Exists(settingsFile))
                        {
                            return settingsFile;
                        }
                        if (settingsDir.FullName.Equals(rootSpritesDir.FullName, StringComparison.InvariantCultureIgnoreCase)
                            || settingsDir.FullName.Equals(rootWebSiteDir.FullName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            // IF we've just checked the rootSpritesDir
                            //    OR we're at the web site root
                            // THEN we stop looking as we're as high as we should go
                            break;
                        }
                        settingsDir = settingsDir.Parent;
                    }
                    return null;
                }
            }
            return null;
        }

        private ImageSettings GetSettings(string settingFileLocation)
        {
            ImageSettings settings = new ImageSettings();
            if (settingFileLocation != null)
            {
                XmlTextReader settingsData;

                // Open the settings file. If it fails here, we throw an exception since we expect the file to be there and readable.
                using (settingsData = new XmlTextReader(settingFileLocation))
                {
                    while (settingsData.Read())
                    {
                        if (settingsData.NodeType == XmlNodeType.Element)
                        {
                            string nodeName = settingsData.Name;

                            if (nodeName.Equals("FileFormat", StringComparison.OrdinalIgnoreCase))
                            {
                                settings.Format = settingsData.ReadElementContentAsString().Trim('.');
                            }
                            else if (nodeName.Equals("Quality", StringComparison.OrdinalIgnoreCase))
                            {
                                settings.Quality = settingsData.ReadElementContentAsInt();
                            }
                            else if (nodeName.Equals("MaxSize", StringComparison.OrdinalIgnoreCase))
                            {
                                settings.MaxSize = settingsData.ReadElementContentAsInt();
                            }
                            else if (nodeName.Equals("BackgroundColor", StringComparison.OrdinalIgnoreCase))
                            {
                                string output = settingsData.ReadElementContentAsString();
                                int temp = Int32.Parse(output, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                                settings.BackgroundColor = Color.FromArgb(temp);
                            }
                            else if (nodeName.Equals("Base64Encoding", StringComparison.OrdinalIgnoreCase))
                            {
                                settings.Base64 = settingsData.ReadElementContentAsBoolean();
                            }
                            else if (nodeName.Equals("TileInYAxis", StringComparison.OrdinalIgnoreCase))
                            {
                                settings.TileInYAxis = settingsData.ReadElementContentAsBoolean();
                            }
                        }
                    }
                }

                return settings;
            }

            return settings;
        }

        private string GetTrimmedPath(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            else if (path == string.Empty)
            {
                throw new ArgumentOutOfRangeException(nameof(path), "Value cannot be empty.");
            }

            return path
                .TrimEnd(Path.AltDirectorySeparatorChar)
                .TrimEnd(Path.DirectorySeparatorChar);
        }

        private ImageCodecInfo GetEncoderInfo(string format)
        {
            format = format.ToUpperInvariant();

            // Find the appropriate codec for the specified file extension
            if (format == "JPG")
            {
                format = "JPEG";
            }

            format = "IMAGE/" + format;
            // Get a list of all the available encoders
            ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders();

            // Search the list for the proper encoder
            foreach (ImageCodecInfo encoder in encoders)
            {
                if (encoder.MimeType.ToUpperInvariant() == format)
                {
                    return encoder;
                }
            }

            // If a format cannot be found, throw an exception
            throw new FormatException("Encoder not found! The CLI will attempt to automatically choose an encoder, however image quality settings will be ignored");
        }
    }
}