using System;
using System.IO;
using System.Web;

namespace Frappe.Sprites
{
    public class SpriteModule : IHttpModule
    {
        private static readonly object _lockObj = new object();

        public void Init(HttpApplication application)
        {
            if (application != null)
            {
                application.BeginRequest += new System.EventHandler(application_BeginRequest);
                this.EnsureSpritesInitialized(new HttpContextWrapper(application.Context));
            }
        }

        /// <summary>
        /// Handle the begin request event of the applicaiton. We need to ensure that the sprites are initialized.
        /// </summary>
        /// <param name="sender">The application.</param>
        /// <param name="e">The event args.</param>
        void application_BeginRequest(object sender, System.EventArgs e)
        {
            HttpApplication application = (HttpApplication)sender;
            this.EnsureSpritesInitialized(new HttpContextWrapper(application.Context));
        }

        /// <summary>
        /// Ensures the sprites are initialized.
        /// </summary>
        /// <param name="context">The context.</param>
        private void EnsureSpritesInitialized(HttpContextBase context)
        {
#if DEBUG
            System.Diagnostics.Trace.TraceInformation("Ensuring sprites are initialized...");
#endif

            const string ImageOptimizationsCacheKey = "ImageOptimizationsCache";
            if (context.Cache[ImageOptimizationsCacheKey] == null)
            {
                // IF the cache key is missing
                // THEN we need to initialize sprites

                // lock so we don't do this multiple times at the same time
                lock (_lockObj)
                {
                    // check again just in case it was already done
                    if (context.Cache[ImageOptimizationsCacheKey] == null)
                    {
#if DEBUG
                        System.Diagnostics.Trace.TraceInformation("Initializing sprites...");
#endif

                        string spriteDirectoryPhysicalPath = context.Server.MapPath(SpriteGenerator.SpriteDirectoryRelativePath);
                        if (Directory.Exists(spriteDirectoryPhysicalPath))
                        {
                            var dependencies = SpriteGenerator.Default.ProcessDirectories(spriteDirectoryPhysicalPath);
                            dependencies.Add(SpriteGenerator.Default.SaveBlankFile(spriteDirectoryPhysicalPath));
                            context.Cache.Add(ImageOptimizationsCacheKey, DateTime.Now, new System.Web.Caching.CacheDependency(dependencies.ToArray()), System.Web.Caching.Cache.NoAbsoluteExpiration, TimeSpan.FromHours(12), System.Web.Caching.CacheItemPriority.High, null);
                            SpriteGenerator.Default.Initialized = true;
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
        }
    }
}