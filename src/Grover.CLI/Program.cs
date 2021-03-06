namespace Grover.CLI;

using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

using Grover;

#region Enums
public enum ExitResult
{
    SUCCESS = 0,
    UNHANDLED_EXCEPTION = 1,
    INVALID_OPTIONS = 2,
    NOT_FOUND = 4,
    SERVER_ERROR = 5,
    ERROR_IN_RESULTS = 6,
    UNKNOWN_ERROR = 7
}
#endregion

class Program : Runtime
{
    #region Constructor
    static Program()
    {
        AppDomain.CurrentDomain.UnhandledException += Program_UnhandledException;
        Interactive = true;
        Console.CancelKeyPress += Console_CancelKeyPress;
        Console.OutputEncoding = Encoding.UTF8;
        foreach (var t in optionTypes)
        {
            OptionTypesMap.Add(t.Name, t);
        }
    }
    #endregion

    #region Entry point
    static void Main(string[] args)
    {
        if (args.Contains("--debug"))
        {
            SetLogger(new SerilogLogger(console: true, debug: true));
            Info("Debug mode set.");
        }
        else
        {
            SetLogger(new SerilogLogger(console: true, debug: false));
        }

        PrintLogo();

        #region Parse options
        ParserResult<object> result = new Parser().ParseArguments<Options, InstallOptions, AssemblyOptions, BoogieOptions, SpecSharpOptions, TranslateOptions>(args);
        result.WithParsed<InstallOptions>(o =>
        {
            ExternalToolsManager.EnsureAllExists();
            if(o.Info)
            {
                foreach(var vi in ExternalToolsManager.GetVersionInfo())
                {
                    Con.WriteLine(vi);
                }
            }
        })
        .WithParsed<AssemblyOptions>(o =>
        {
            if (o.References)
            {
                AssemblyCmd.References(o.File);
                Exit(ExitResult.SUCCESS);
            }
        })
        .WithParsed<BoogieOptions>(o =>
        {
            var ret = RunCmd("boogie", o.Options.Aggregate((a, b) => a + " " + b));
            if (ret is not null)
            {
                Con.Write($"[bold white]{ret.EscapeMarkup()}[/]".ToMarkup());
            }
            else
            {
                Error("Error executing Boogie.");
            }
        })
        .WithParsed<SpecSharpOptions>(o =>
        {
            if (o.Compile)
            {
                Commands.SscCmd.Compile(o.Options.First());
            }
            else
            {
                var ret = RunCmd(Path.Combine(AssemblyLocation, "bin", "ssc"), o.Options.Aggregate((a, b) => a + " " + b), Path.Combine(AssemblyLocation, "bin"));
                if (ret is not null)
                {
                    Con.Write($"[bold white]{ret.EscapeMarkup()}[/]".ToMarkup());
                }
                else
                {
                    Error("Error executing ssc.");
                }
            }
        })
        .WithParsed<TranslateOptions>(o =>
        {
            if (!File.Exists(o.File))
            {
                Error("The file {0} does not exist.", o.File);
                Exit(ExitResult.INVALID_OPTIONS);

            }
            if (o.File.EndsWith(".dll") || o.File.EndsWith(".exe"))
            {
                using (var op = Begin("Translating .NET assembly {0} to Boogie IVL", o.File))
                {
                    var a = new Grover.Metadata.Assembly(o.File);
                    var files = new List<string> ();
                    //files.AddRange(a.References.Where(r => r.ResolverData is not null).Select(r => r.ResolverData!.File.FullName));
                    files.Add(o.File);
                    Info("Assemblies to translate: {0}.", files);
                    var ret = BytecodeTranslator.BCT.TranslateAssemblyAndWriteOutput(files, new BytecodeTranslator.GeneralHeap(), new BytecodeTranslator.Options(), new List<Regex>(), false);
                    op.Complete();
                }
            }
            
        })
        
        #endregion

        #region Print options help
        .WithNotParsed((IEnumerable<Error> errors) =>
        {
            HelpText help = GetAutoBuiltHelpText(result);
            help.Heading = new HeadingInfo("Grover", AssemblyVersion.ToString(3));
            help.Copyright = "";
            if (errors.Any(e => e.Tag == ErrorType.VersionRequestedError))
            {
                help.Heading = new HeadingInfo("Grover", AssemblyVersion.ToString(3));
                help.Copyright = new CopyrightInfo("Allister Beharry", new int[] { 2021 });
                Info(help);
                Exit(ExitResult.SUCCESS);
            }
            else if (errors.Any(e => e.Tag == ErrorType.HelpVerbRequestedError))
            {
                HelpVerbRequestedError error = (HelpVerbRequestedError)errors.First(e => e.Tag == ErrorType.HelpVerbRequestedError);
                if (error.Type != null)
                {
                    help.AddVerbs(error.Type);
                }
                else
                {
                    help.AddVerbs(optionTypes);
                }
                Info(help.ToString().Replace("--", ""));
                Exit(ExitResult.SUCCESS);
            }
            else if (errors.Any(e => e.Tag == ErrorType.HelpRequestedError))
            {
                HelpRequestedError error = (HelpRequestedError)errors.First(e => e.Tag == ErrorType.HelpRequestedError);
                help.AddVerbs(result.TypeInfo.Current);
                help.AddOptions(result);
                help.AddPreOptionsLine($"{result.TypeInfo.Current.Name.Replace("Options", "").ToLower()} options:");
                Info(help);
                Exit(ExitResult.SUCCESS);
            }
            else if (errors.Any(e => e.Tag == ErrorType.NoVerbSelectedError))
            {
                help.AddVerbs(optionTypes);
                Info(help);
                Exit(ExitResult.INVALID_OPTIONS);
            }
            else if (errors.Any(e => e.Tag == ErrorType.MissingRequiredOptionError))
            {
                MissingRequiredOptionError error = (MissingRequiredOptionError)errors.First(e => e.Tag == ErrorType.MissingRequiredOptionError);
                Error("A required option is missing: {0}.", error.NameInfo.NameText);
                Info(help);
                Exit(ExitResult.INVALID_OPTIONS);
            }
            else if (errors.Any(e => e.Tag == ErrorType.UnknownOptionError))
            {
                UnknownOptionError error = (UnknownOptionError)errors.First(e => e.Tag == ErrorType.UnknownOptionError);
                help.AddVerbs(optionTypes);
                Error("Unknown option: {error}.", error.Token);
                Info(help);
                Exit(ExitResult.INVALID_OPTIONS);
            }
            else
            {
                Error("An error occurred parsing the program options: {errors}.", errors);
                help.AddVerbs(optionTypes);
                Info(help);
                Exit(ExitResult.INVALID_OPTIONS);
            }
        });
        #endregion;
    }
    #endregion

    #region Properties
    private static FigletFont Font { get; } = FigletFont.Load("chunky.flf");

    
    static Dictionary<string, Type> OptionTypesMap { get; } = new Dictionary<string, Type>();
    #endregion

    #region Methods

    static void PrintLogo()
    {
        Con.Write(new FigletText(Font, "Grover").LeftAligned().Color(Spectre.Console.Color.Blue));
        Con.Write(new Text($"v{AssemblyVersion.ToString(3)}\n").LeftAligned());
    }

    public static void Exit(ExitResult result)
    {
        if (Cts != null && !Cts.Token.CanBeCanceled)
        {
            Cts.Cancel();
            Cts.Dispose();
        }
        Serilog.Log.CloseAndFlush();
        Environment.Exit((int)result);
    }

    static HelpText GetAutoBuiltHelpText(ParserResult<object> result)
    {
        return HelpText.AutoBuild(result, h =>
        {
            h.AddOptions(result);
            return h;
        },
        e =>
        {
            return e;
        });
    }
    #endregion

    #region Event Handlers
    private static void Program_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Serilog.Log.CloseAndFlush();
        Error("Unhandled runtime error occurred. Grover CLI will now shutdown.");
        Con.WriteException((Exception) e.ExceptionObject);
        Exit(ExitResult.UNHANDLED_EXCEPTION);
    }

    private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        Info("Ctrl-C pressed. Exiting.");
        Cts.Cancel();
        Exit(ExitResult.SUCCESS);
    }
    #endregion

    #region Fields
    static object _uilock = new object();
    static Type[] optionTypes = { typeof(Options), typeof(InstallOptions), typeof(AssemblyOptions), typeof(BoogieOptions), typeof(SpecSharpOptions), typeof(TranslateOptions) };
    #endregion
}

