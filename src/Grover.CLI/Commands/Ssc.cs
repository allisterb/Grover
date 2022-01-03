namespace Grover.CLI.Commands;

using Grover.SpecSharp;
internal class SscCmd : Runtime
{
    internal static void Compile(string fileName)
    {
        var f = new XmlSpecSharpProject(fileName);
    }
}

