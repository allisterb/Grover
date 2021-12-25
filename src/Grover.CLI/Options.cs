﻿using CommandLine;
using CommandLine.Text;

namespace Grover.CLI
{
    public class Options
    {
        [Option('d', "debug", Required = false, HelpText = "Enable debug mode.")]
        public bool Debug { get; set; }

        [Value(0, Required = true, Max = 1, HelpText = "The source file or project file to analyze.")]
        public string File { get; set; } = string.Empty;

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

    [Verb("roslyn", HelpText = "Translate to Roslyn.")]
    public class RoslynOptions : Options 
    {
        [Option('p', "print", Required = false, HelpText = "Print the Roslyn AST for the specified file(s).")]
        public bool Print { get; set; }
    }
}
