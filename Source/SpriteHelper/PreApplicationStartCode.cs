using System.ComponentModel;
using System.Web.WebPages.Razor;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;

namespace Microsoft.Web.Samples {
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class PreApplicationStartCode {
        private static bool _startWasCalled;

        public static void Start() {
            // Even though ASP.NET will only call each PreAppStart once, we sometimes internally call one PreAppStart from
            // another PreAppStart to ensure that things get initialized in the right order. ASP.NET does not guarantee the
            // order so we have to guard against multiple calls.
            // All Start calls are made on same thread, so no lock needed here.
            if (_startWasCalled) {
                return;
            }

            _startWasCalled = true;

            DynamicModuleUtility.RegisterModule(typeof(ImageOptimizationModule));

            WebPageRazorHost.AddGlobalImport(typeof(PreApplicationStartCode).Namespace);
        }
    }
}