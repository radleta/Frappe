using System;
using dotless.Core.configuration;

namespace Frappe.MSBuild.Tasks
{
    internal class CompilerConfiguration : DotlessConfiguration
    {
        public CompilerConfiguration(DotlessConfiguration config)
            : base(config)
        {
            CacheEnabled = false;
            Web = false;
        }

        public bool Help { get; set; }
    }
}
