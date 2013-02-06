using System.Collections.Generic;
using System.Linq;
using dotless.Core;

namespace Frappe.MSBuild.Tasks
{
    /// <summary>
    /// Used by the <see cref="dotlessCompiler"/>.
    /// </summary>
    /// <remarks>
    /// Imported from: https://github.com/dotless/dotless/blob/master/src/dotless.Compiler/FixImportPathDecorator.cs
    /// </remarks>
    class FixImportPathDecorator : ILessEngine
    {
        private readonly ILessEngine underlying;

        public FixImportPathDecorator(ILessEngine underlying)
        {
            this.underlying = underlying;
        }

        public string TransformToCss(string source, string fileName)
        {
            return underlying.TransformToCss(source, fileName);
        }

        public void ResetImports()
        {
            underlying.ResetImports();
        }

        public IEnumerable<string> GetImports()
        {
            return underlying.GetImports().Select(import => import.Replace("/", "\\"));
        }

        public bool LastTransformationSuccessful
        {
            get
            {
                return underlying.LastTransformationSuccessful;
            }
        }
    }
}
