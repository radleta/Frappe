using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Frappe")]
[assembly: AssemblyDescription("The library to support compile time bundling of JavaScript, Css, and Less.")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("Richard Adleta")]
[assembly: AssemblyProduct("Frappe")]
[assembly: AssemblyCopyright("Copyright © Richard Adleta 2012")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]
[assembly: Guid("ad203410-6567-43b6-88b7-4f4780e27cdc")]

[assembly: AssemblyVersion("1.5.0.0")]
[assembly: AssemblyFileVersion("1.5.0.0")]

// allow unit tests to see our internals
[assembly: InternalsVisibleTo("Frappe.Tests")]