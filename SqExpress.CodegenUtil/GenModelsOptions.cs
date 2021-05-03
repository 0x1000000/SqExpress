using CommandLine;

namespace SqExpress.CodeGenUtil
{
    [Verb("genmodels", HelpText = "Generate model classes.")]
    public class GenModelsOptions
    {
        public GenModelsOptions(string inputDir, string outputDir, string @namespace, Verbosity verbosity, bool rwClasses, bool nullRefTypes)
        {
            this.InputDir = inputDir;
            this.OutputDir = outputDir;
            this.Namespace = @namespace;
            this.Verbosity = verbosity;
            this.RwClasses = rwClasses;
            this.NullRefTypes = nullRefTypes;
        }

        [Option('i',"input-dir", Required = false, Default = "", HelpText = "Path to a directory with table descriptors.")]
        public string InputDir { get; }

        [Option('o',"output-dir", Required = false, Default = "", HelpText = "Path to a directory where cs files will be written.")]
        public string OutputDir { get; }

        [Option('n',"namespace", Required = false, Default = "MyCompany.MyApp.Tables", HelpText = "Default namespace for newly crated files.")]
        public string Namespace { get; }

        [Option('v',"verbosity", Required = false, Default = Verbosity.Minimal, HelpText = "Allowed values are quiet, minimal, normal, detailed, and diagnostic. The default is minimal")]
        public Verbosity Verbosity { get; }

        [Option("rw-classes", Required = false, Default = false, HelpText = "Adds Reader and Writer class instances to generated models")]
        public bool RwClasses { get; }

        [Option("null-ref-types", Required = false, Default = false, HelpText = "Adds \"?\" for nullable reference types.")]
        public bool NullRefTypes { get; }
    }
}