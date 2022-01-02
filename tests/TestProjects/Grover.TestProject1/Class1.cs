namespace Grover.TestProject1
{
    using System.Diagnostics.Contracts;
    

    public class Class1
    {
        public int AddTest(int a, int b)
        {
            Contract.Requires(a > b);
            return a + b;
        }
    }
}