namespace Grover.CLI;

using Spectre.Console;
using Grover.Metadata;


public class AssemblyCmd : Runtime
{
    public static void References(string path)
    {
        if (!File.Exists(path))
        {
            Error("The file {0} does not exist.", path);
            Program.Exit(ExitResult.NOT_FOUND);
        }
        Assembly asm = GetTimed(() => new Assembly(path), "Loading assembly", "Loading assembly {0}", path);
        Info("References:{0}", asm.References.Select(r => r.Ref));
    }
}

