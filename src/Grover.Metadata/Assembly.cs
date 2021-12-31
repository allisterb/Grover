namespace Grover.Metadata;

using System.Reflection;
using Microsoft.Cci;
using Microsoft.Cci.MutableContracts;

using Nuclear.Assemblies.Runtimes;
using Nuclear.Assemblies.Resolvers;
using Nuclear.Assemblies.Resolvers.Internal;
public readonly record struct AssemblyReference(IAssemblyReference Ref, IAssembly Resolved, string path);
public class Assembly : Runtime
{
    #region Constructors
    public Assembly(string assemblyPath)
    {
        Host = new CodeContractAwareHostEnvironment(new string[] { new FileInfo(assemblyPath).DirectoryName! }, true, true);
        Module = (IModule)Host.LoadUnitFrom(assemblyPath);

        References = Module.AssemblyReferences.Select(a => new AssemblyReference(a, a.ResolvedAssembly, a.ResolvedAssembly.Location));

        //NugetResolver.
    }
    #endregion

    #region Properties
    public CodeContractAwareHostEnvironment Host { get; init; }
    public IModule Module { get; init; }

    public IEnumerable<AssemblyReference> References { get; init; }

    internal static DefaultResolver DefaultResolver { get; } = new DefaultResolver(VersionMatchingStrategies.Strict, SearchOption.AllDirectories);
    internal static ICoreNugetResolver NugetResolver {get; } = new NugetResolver(VersionMatchingStrategies.Strict, VersionMatchingStrategies.Strict).CoreResolver;
    #endregion
}

