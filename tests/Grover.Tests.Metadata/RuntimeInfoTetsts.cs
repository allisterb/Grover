using Xunit;
using Nuclear.Assemblies;
namespace Grover.Tests.Metadata
{
    public class RuntimeInfoTests
    {
        [Fact]
        public void Test1()
        {
            RuntimesHelper.TryGetCurrentRuntime(out var runtime);
            Assert.Equal(Nuclear.Assemblies.Runtimes.FrameworkIdentifiers.NETCoreApp, runtime.Framework);
        }
    }
}