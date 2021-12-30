using CommandLine;
using CommandLine.Text;

namespace Grover.CLI
{
    public class Options
    {
        [Option('d', "debug", Required = false, HelpText = "Enable debug mode.")]
        public bool Debug { get; set; }

        public static Dictionary<string, object> Parse(string o)
        {
            Dictionary<string, object> options = new Dictionary<string, object>();
            Regex re = new Regex(@"(\w+)\=([^\,]+)", RegexOptions.Compiled);
            string[] pairs = o.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in pairs)
            {
                Match m = re.Match(s);
                if (!m.Success)
                {
                    options.Add("_ERROR_", s);
                }
                else if (options.ContainsKey(m.Groups[1].Value))
                {
                    options[m.Groups[1].Value] = m.Groups[2].Value;
                }
                else
                {
                    options.Add(m.Groups[1].Value, m.Groups[2].Value);
                }
            }
            return options;
        }
    }

    [Verb("install", HelpText = "Install any required external tools.")]
    public class InstallOptions : Options 
    {
        [Option('i', "info", Required = false, HelpText = "Print version information for installed external tools.")]
        public bool Info { get; set; }
    }

    [Verb("boogie", HelpText = "Execute the installed Boogie tool with the specified options.")]
    public class BoogieOptions : Options
    {
        [Value(0, Required = true, HelpText = "The options to pass to Boogie.")]
        public IEnumerable<string> Options { get; set; } = Array.Empty<string>();
    }

    [Verb("bct", HelpText = "Translate a .NET bytecode assembly to Boogie.")]
    public class BctOptions : Options 
    {
        [Option('p', "print", Required = false, HelpText = "Print the Roslyn AST for the specified file(s).")]
        public bool Print { get; set; }
    }
}
