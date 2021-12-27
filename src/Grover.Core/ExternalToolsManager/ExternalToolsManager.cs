namespace Grover.ExternalTools;

using System;
using Microsoft.Extensions.Configuration;
    

public static class ExternalToolsManager
{
    public static Logger Logger;

    public static ToolManager Solc { get; set; }
    public static ToolManager Z3 { get; private set; }
    public static ToolManager Boogie { get; private set; }
    public static ToolManager Corral { get; private set; }

    static ExternalToolsManager()
    {
        Logger = new ConsoleLogger();
        IConfiguration toolSourceConfig = new ConfigurationBuilder()
            .AddJsonFile("toolsourcesettings.json", true, true)
            .Build();

        var solcSourceSettings = new ToolSourceSettings();
        toolSourceConfig.GetSection("solc").Bind(solcSourceSettings);
        Solc = new SolcManager(solcSourceSettings);

        var z3SourceSettings = new ToolSourceSettings();
        toolSourceConfig.GetSection("z3").Bind(z3SourceSettings);
        Z3 = new DownloadedToolManager(z3SourceSettings);

        var boogieSourceSettings = new ToolSourceSettings();
        toolSourceConfig.GetSection("boogie").Bind(boogieSourceSettings);
        Boogie = new DotnetCliToolManager(boogieSourceSettings);

        var corralSourceSettings = new ToolSourceSettings();
        toolSourceConfig.GetSection("corral").Bind(corralSourceSettings);
        Corral = new DotnetCliToolManager(corralSourceSettings);
    }

    internal static void Log(string v)
    {
        Logger.Debug(v);
    }

    public static void EnsureAllExisted()
    {
        Solc.EnsureExisted();

        Z3.EnsureExisted();

        Boogie.EnsureExisted();
        ((DotnetCliToolManager) Boogie).EnsureLinkedToZ3(Z3);

        Corral.EnsureExisted();
        ((DotnetCliToolManager) Corral).EnsureLinkedToZ3(Z3);
    }
}

