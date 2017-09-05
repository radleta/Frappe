using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using dotless.Core;
using dotless.Core.configuration;
using dotless.Core.Parameters;
using dotless.Core.Plugins;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Text;

namespace Frappe.MSBuild.Tasks
{
    /// <summary>
    /// The MSBuild task for dotless.
    /// </summary>
    /// <remarks>
    /// A port of the dotLess.compiler into MSBuild.
    /// </remarks>
    public class dotlessCompiler : Task, dotless.Core.Loggers.ILogger
    {

        /// <summary>
        /// A <see cref="ContainerFactory"/> implementation to support overrides to the default
        /// to expose the functionality of the <see cref="dotlessCompiler"/> to the <see cref="Engine"/>.
        /// </summary>
        /// <remarks>The primary purpose of this class is to provide the <see cref="dotlessCompiler"/> as 
        /// the <see cref="dotless.Core.Loggers.ILogger"/> to send log messages to msbuild when compiling less files.</remarks>
        public class dotlessCompilerContainerFactory : dotless.Core.ContainerFactory
        {
            /// <summary>
            /// Initializes a new instance of this class.
            /// </summary>
            /// <param name="compiler">The <see cref="dotlessCompiler"/> to use when registering services.</param>
            public dotlessCompilerContainerFactory(dotlessCompiler compiler) : base()
            {
                if (compiler == null)
                {
                    throw new System.ArgumentNullException("compiler");
                }

                this.Compiler = compiler;
            }

            /// <summary>
            /// The <see cref="dotlessCompiler"/> to use when registering services.
            /// </summary>
            protected dotlessCompiler Compiler { get; private set; }

            /// <summary>
            /// Provides our own implementation of the services.
            /// </summary>
            /// <param name="pandora">The IoC instance.</param>
            /// <param name="configuration">The dotless configuration.</param>
            protected override void OverrideServices(Pandora.Fluent.FluentRegistration pandora, DotlessConfiguration configuration)
            {
                base.OverrideServices(pandora, configuration);

                if (configuration.Logger == null)
                {
                    // IF no logger defined 
                    // THEN we're going to use the Compiler as the logger to stream output out

                    pandora.Service<dotless.Core.Loggers.ILogger>().Instance(Compiler);
                }
            }
        }

        /// <summary>
        /// The input arguments for the task.
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// The return code of the task.
        /// </summary>
        public int ReturnCode { get; protected set; }

        /// <summary>
        /// The entry point for the task.
        /// </summary>
        /// <returns></returns>
        public override bool Execute()
        {
            var args = Arguments.SplitCommandLine().ToArray();
            ReturnCode = Main(args);
            return (ReturnCode == 0);
        }
                
        /// <summary>
        /// The main method of the task.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The return code.</returns>
        private int Main(string[] args)
        {
            var arguments = new List<string>();

            arguments.AddRange(args);

            var configuration = GetConfigurationFromArguments(arguments);

            if (configuration.Help)
                return -1;

            if (arguments.Count == 0)
            {
                WriteHelp();
                return -1;
            }

            string inputDirectoryPath, inputFilePattern;

            if (Directory.Exists(arguments[0]))
            {
                inputDirectoryPath = arguments[0];
                inputFilePattern = "*.less";
            }
            else
            {
                inputDirectoryPath = Path.GetDirectoryName(arguments[0]);
                if (string.IsNullOrEmpty(inputDirectoryPath)) inputDirectoryPath = "." + Path.DirectorySeparatorChar;
                inputFilePattern = Path.GetFileName(arguments[0]);
                if (!Path.HasExtension(inputFilePattern)) inputFilePattern = Path.ChangeExtension(inputFilePattern, "less");
            }

            var outputDirectoryPath = string.Empty;
            var outputFilename = string.Empty;
            if (arguments.Count > 1)
            {
                if (Directory.Exists(arguments[1]))
                {
                    outputDirectoryPath = arguments[1];
                }
                else
                {
                    outputDirectoryPath = Path.GetDirectoryName(arguments[1]);
                    outputFilename = Path.GetFileName(arguments[1]);

                    if (!Path.HasExtension(outputFilename))
                        outputFilename = Path.ChangeExtension(outputFilename, "css");
                }
            }

            if (string.IsNullOrEmpty(outputDirectoryPath))
            {
                outputDirectoryPath = inputDirectoryPath;
            }
            else
            {
                Directory.CreateDirectory(outputDirectoryPath);
            }

            if (HasWildcards(inputFilePattern))
            {
                if (!string.IsNullOrEmpty(outputFilename))
                {
                    Log.LogError("Output filename patterns/filenames are not supported when using input wildcards. You may only specify an output directory (end the output in a directory seperator)");
                    return -1;
                }
                outputFilename = string.Empty;
            }

            var filenames = Directory.GetFiles(inputDirectoryPath, inputFilePattern);
            if (filenames.Count() == 0)
            {
                Log.LogError("No files found matching pattern '{0}'", inputFilePattern);
                return -1;
            }
            
            var engineFactory = new EngineFactory(configuration);
            var containerFactory = new dotlessCompilerContainerFactory(this);
            var engine = engineFactory.GetEngine(containerFactory);
            foreach (var filename in filenames)
            {
                var inputFile = new FileInfo(filename);

                var outputFile =
                    string.IsNullOrEmpty(outputFilename) ?
                        Path.Combine(outputDirectoryPath, Path.ChangeExtension(inputFile.Name, "css")) :
                        Path.Combine(outputDirectoryPath, outputFilename);

                var outputFilePath = Path.GetFullPath(outputFile);

                var returnCode = CompileImpl(engine, inputFile.FullName, outputFilePath);
                if (returnCode != 0)
                {
                    // IF not zero
                    // THEN return the failure code
                    return returnCode;
                }
            }           

            return ReturnCode;
        }
        
        /// <summary>
        /// Attempts to compile a less file into css.
        /// </summary>
        /// <param name="engine">The dotless engine.</param>
        /// <param name="inputFilePath">The input less file.</param>
        /// <param name="outputFilePath">The output css file.</param>
        /// <returns>The return code. 0 is success; otherwise, value is failure.</returns>
        private int CompileImpl(ILessEngine engine, string inputFilePath, string outputFilePath)
        {
            engine = new FixImportPathDecorator(engine);
            var currentDir = Directory.GetCurrentDirectory();
            try
            {
                Log.LogMessage("Compiling less from \"{0}\" to \"{1}\".", inputFilePath, outputFilePath);
                var directoryPath = Path.GetDirectoryName(inputFilePath);
                var fileReader = new dotless.Core.Input.FileReader();
                var source = fileReader.GetFileContents(inputFilePath);
                Directory.SetCurrentDirectory(directoryPath);
                var css = engine.TransformToCss(source, inputFilePath);
                
                File.WriteAllText(outputFilePath, css);

                if (!engine.LastTransformationSuccessful)
                {
                    Log.LogError("Failed: Compiling less from \"{0}\" to \"{1}\".", inputFilePath, outputFilePath);
                    return -5;
                }
                else
                {
                    Log.LogMessage("Success: Compiled less from \"{0}\" to \"{1}\".", inputFilePath, outputFilePath);
                }

                var files = new List<string>();
                files.Add(inputFilePath);
                foreach (var file in engine.GetImports())
                    files.Add(Path.Combine(directoryPath, Path.ChangeExtension(file, "less")));
                engine.ResetImports();
                return 0;
            }
            catch (Exception ex)
            {
                Log.LogError("Failed: Compiling less from \"{0}\" to \"{1}\".", inputFilePath, outputFilePath);
                Log.LogError("Compilation failed: {0}", ex.ToString());
                return -3;
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDir);
            }            
        }
        
        private static bool HasWildcards(string inputFilePattern)
        {
            return System.Text.RegularExpressions.Regex.Match(inputFilePattern, @"[\*\?]").Success;
        }
        
        private static string GetAssemblyVersion()
        {
            Assembly assembly = typeof(EngineFactory).Assembly;
            var attributes = assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true) as
                             AssemblyFileVersionAttribute[];
            if (attributes != null && attributes.Length == 1)
                return attributes[0].Version;
            return "v.Unknown";
        }

        private static IEnumerable<IPluginConfigurator> _pluginConfigurators = null;
        private static IEnumerable<IPluginConfigurator> GetPluginConfigurators()
        {
            if (_pluginConfigurators == null)
            {
                _pluginConfigurators = PluginFinder.GetConfigurators(true);
            }
            return _pluginConfigurators;
        }

        private void WritePluginList()
        {
            var sb = new StringBuilder();

            sb.AppendLine("List of plugins");
            sb.AppendLine();
            foreach (IPluginConfigurator pluginConfigurator in GetPluginConfigurators())
            {
                sb.AppendFormat("{0}", pluginConfigurator.Name).AppendLine();
                sb.AppendFormat("\t{0}", pluginConfigurator.Description).AppendLine();
                sb.AppendLine("\tParams: ");
                foreach (IPluginParameter pluginParam in pluginConfigurator.GetParameters())
                {
                    sb.Append("\t\t");

                    if (!pluginParam.IsMandatory)
                        sb.Append("[");

                    sb.AppendFormat("{0}={1}", pluginParam.Name, pluginParam.TypeDescription);

                    if (!pluginParam.IsMandatory)
                        sb.AppendLine("]");
                }
                sb.AppendLine();
            }

            Log.LogMessage(sb.ToString());
        }

        private void WriteHelp()
        {
            var sb = new StringBuilder();

            sb.AppendFormat("dotless Compiler {0}", GetAssemblyVersion()).AppendLine();
            sb.AppendLine("\tCompiles .less files to css files.");
            sb.AppendLine();
            sb.AppendLine("Usage: dotless.Compiler.exe [-switches] <inputfile> [outputfile]");
            sb.AppendLine("\tSwitches:");
            sb.AppendLine("\t\t-m --minify - Output CSS will be compressed");
            sb.AppendLine("\t\t-k --keep-first-comment - Keeps the first comment begninning /** when minified");
            sb.AppendLine("\t\t-d --debug  - Print helpful debug comments in output (not compatible with -m)");
            sb.AppendLine("\t\t-h --help   - Displays this dialog");
            sb.AppendLine("\t\t-r --disable-url-rewriting - Disables changing urls in imported files");
            sb.AppendLine("\t\t-a --import-all-less - treats every import as less even if ending in .css");
            sb.AppendLine("\t\t-c --inline-css - Inlines CSS file imports into the output");
            sb.AppendLine("\t\t-v --disable-variable-redefines - Makes variables behave more like less.js, so the last variable definition is used");
            sb.AppendLine("\t\t-DKey=Value - prefixes variable to the less");
            sb.AppendLine("\t\t-l --listplugins - Lists the plugins available and options");
            sb.AppendLine("\t\t-p: --plugin:pluginName[:option=value[,option=value...]] - adds the named plugin to dotless with the supplied options");
            sb.AppendLine("\tinputfile: .less file dotless should compile to CSS");
            sb.AppendLine("\toutputfile: (optional) desired filename for .css output");
            sb.AppendLine("\t\t Defaults to inputfile.css");
            sb.AppendLine("");
            sb.AppendLine("Example:");
            sb.AppendLine("\t-m \"-p:Rtl:forceRtlTransform=true,onlyReversePrefixedRules=true\"");
            sb.AppendLine("\t\tMinify and add the Rtl plugin");

            Log.LogMessage(sb.ToString());
        }

        private CompilerConfiguration GetConfigurationFromArguments(List<string> arguments)
        {
            var dotLessConfig = DotlessConfiguration.GetDefault();
            dotLessConfig.LogLevel = dotless.Core.Loggers.LogLevel.Warn;
            dotLessConfig.Logger = typeof(dotless.Core.Loggers.ConsoleLogger);

            var configuration = new CompilerConfiguration(dotLessConfig);
            
            foreach (var arg in arguments)
            {
                if (arg.StartsWith("-"))
                {
                    if (arg == "-m" || arg == "--minify")
                    {
                        configuration.MinifyOutput = true;
                    }
                    else if (arg == "-k" || arg == "--keep-first-comment")
                    {
                        configuration.KeepFirstSpecialComment = true;
                    }
                    else if (arg == "-d" || arg == "--debug")
                    {
                        configuration.Debug = true;
                    }
                    else if (arg == "-h" || arg == "--help" || arg == @"/?")
                    {
                        WriteHelp();
                        configuration.Help = true;
                        return configuration;
                    }
                    else if (arg == "-l" || arg == "--listplugins")
                    {
                        WritePluginList();
                        configuration.Help = true;
                        return configuration;
                    }
                    else if (arg == "-a" || arg == "--import-all-less")
                    {
                        configuration.ImportAllFilesAsLess = true;
                    }
                    else if (arg == "-c" || arg == "--inline-css")
                    {
                        configuration.InlineCssFiles = true;
                    }
                    else if (arg.StartsWith("-D") && arg.Contains("="))
                    {
                        var split = arg.Substring(2).Split('=');
                        var key = split[0];
                        var value = split[1];
                        ConsoleArgumentParameterSource.ConsoleArguments.Add(key, value);
                    }
                    else if (arg.StartsWith("-r") || arg.StartsWith("--disable-url-rewriting"))
                    {
                        configuration.DisableUrlRewriting = true;
                    }
                    else if (arg.StartsWith("-v") || arg.StartsWith("--disable-variable-redefines"))
                    {
                        configuration.DisableVariableRedefines = true;
                    }
                    else if (arg.StartsWith("-p:") || arg.StartsWith("--plugin:"))
                    {
                        var pluginName = arg.Substring(arg.IndexOf(':') + 1);
                        List<string> pluginArgs = null;
                        if (pluginName.IndexOf(':') > 0)
                        {
                            pluginArgs = pluginName.Substring(pluginName.IndexOf(':') + 1).Split(',').ToList();
                            pluginName = pluginName.Substring(0, pluginName.IndexOf(':'));
                        }

                        var pluginConfig = GetPluginConfigurators()
                            .Where(plugin => String.Compare(plugin.Name, pluginName, true) == 0)
                            .FirstOrDefault();

                        if (pluginConfig == null)
                        {
                            Log.LogWarning("Cannot find plugin {0}.", pluginName);
                            continue;
                        }

                        var pluginParams = pluginConfig.GetParameters();

                        foreach (var pluginParam in pluginParams)
                        {
                            var pluginArg = pluginArgs
                                .Where(argString => argString.StartsWith(pluginParam.Name + "="))
                                .FirstOrDefault();

                            if (pluginArg == null)
                            {
                                if (pluginParam.IsMandatory)
                                {
                                    Log.LogWarning("Missing mandatory argument {0} in plugin {1}.", pluginParam.Name, pluginName);
                                }
                                continue;
                            }
                            else
                            {
                                pluginArgs.Remove(pluginArg);
                            }

                            pluginParam.SetValue(pluginArg.Substring(pluginParam.Name.Length + 1));
                        }

                        if (pluginArgs.Count > 0)
                        {
                            for (int i = 0; i < pluginArgs.Count; i++)
                            {
                                Log.LogWarning("Did not recognise argument '{0}'", pluginArgs[i]);
                            }
                        }

                        pluginConfig.SetParameterValues(pluginParams);
                        configuration.Plugins.Add(pluginConfig);
                    }
                    else
                    {
                        Log.LogWarning("Unknown command switch {0}.", arg);
                    }
                }
            }
            arguments.RemoveAll(p => p.StartsWith("-"));
            return configuration;
        }

        #region dotless.Core.Loggers.ILogger Implementation

        void dotless.Core.Loggers.ILogger.Debug(string message, params object[] args)
        {
            this.Log.LogMessage(MessageImportance.Low, message, args);
        }

        void dotless.Core.Loggers.ILogger.Debug(string message)
        {
            this.Log.LogMessage(MessageImportance.Low, message);
        }

        void dotless.Core.Loggers.ILogger.Error(string message, params object[] args)
        {
            this.Log.LogError(message, args);
        }

        void dotless.Core.Loggers.ILogger.Error(string message)
        {
            this.Log.LogError(message);
        }

        void dotless.Core.Loggers.ILogger.Info(string message, params object[] args)
        {
            this.Log.LogMessage(MessageImportance.Normal, message, args);
        }

        void dotless.Core.Loggers.ILogger.Info(string message)
        {
            this.Log.LogMessage(MessageImportance.Normal, message);
        }

        void dotless.Core.Loggers.ILogger.Log(dotless.Core.Loggers.LogLevel level, string message)
        {
            var logger = (dotless.Core.Loggers.ILogger)this;
            switch (level)
            {
                case dotless.Core.Loggers.LogLevel.Error:
                    logger.Error(message);
                    break;
                case dotless.Core.Loggers.LogLevel.Warn:
                    logger.Warn(message);
                    break;
                case dotless.Core.Loggers.LogLevel.Info:
                    logger.Info(message);
                    break;
                case dotless.Core.Loggers.LogLevel.Debug:
                    logger.Debug(message);
                    break;
                default:
                    throw new NotImplementedException(string.Format("The LogLevel has not been implemented. LogLevel: {0}", level));
            }
        }

        void dotless.Core.Loggers.ILogger.Warn(string message, params object[] args)
        {
            this.Log.LogWarning(message, args);
        }

        void dotless.Core.Loggers.ILogger.Warn(string message)
        {
            this.Log.LogWarning(message);
        }
        
        #endregion
    }
}
