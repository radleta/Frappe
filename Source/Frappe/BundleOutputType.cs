using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Frappe
{
    /// <summary>
    /// The type of content the bundle outputs.
    /// </summary>
    public enum BundleOutputType
    {
        /// <summary>
        /// The output type could not be determined.
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// The output is JavaScript.
        /// </summary>
        JavaScript = 1,
        /// <summary>
        /// The output is CSS.
        /// </summary>
        Css = 2,
    }
}
