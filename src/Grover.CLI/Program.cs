namespace Grover.CLI;

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
        Console.CancelKeyPress += Console_CancelKeyPress;
        Console.OutputEncoding = Encoding.UTF8;
        foreach (var t in OptionTypes)
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

        if (args.Length == 1 && args.Contains("install"))
        {
            args = args.Append("dummy").ToArray(); //Use a dummy file for install option
        }
        #region Parse options
        ParserResult<object> result = new Parser().ParseArguments<Options, InstallOptions, BctOptions>(args);
        result.WithParsed<InstallOptions>(o =>
        {
            ExternalToolsManager.EnsureAllExists();
        })
        .WithParsed<BctOptions>(o =>
        {
            var asm = @"C:\Projects\Grover\src\TestProjects\Grover.TestProject1\bin\Debug\net6.0\Grover.TestProject1.dll";
            BytecodeTranslator.BCT.Main(asm);
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
                    help.AddVerbs(OptionTypes);
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
                help.AddVerbs(OptionTypes);
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
                help.AddVerbs(OptionTypes);
                Error("Unknown option: {error}.", error.Token);
                Info(help);
                Exit(ExitResult.INVALID_OPTIONS);
            }
            else
            {
                Error("An error occurred parsing the program options: {errors}.", errors);
                help.AddVerbs(OptionTypes);
                Info(help);
                Exit(ExitResult.INVALID_OPTIONS);
            }
        });
        #endregion;
    }
    #endregion

    #region Properties
    private static Version AssemblyVersion { get; } = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!;
    private static FigletFont Font { get; } = FigletFont.Load("chunky.flf");

    static Type[] OptionTypes = { typeof(Options), typeof(InstallOptions), typeof(BctOptions)};
    static Dictionary<string, Type> OptionTypesMap { get; } = new Dictionary<string, Type>();
    #endregion

    #region Methods

    static void PrintLogo()
    {
        Con.Write(new FigletText(Font, "Grover").LeftAligned().Color(Color.Blue));
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
    private static object _uilock = new object();
    #endregion
}

