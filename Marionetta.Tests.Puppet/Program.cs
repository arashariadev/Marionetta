using System.Threading.Tasks;

namespace Marionetta;

public static class Program
{
    public static void Main(string[] args)
    {
        using var puppet = new Puppet(args);

        puppet.RegisterTarget("abc", (int a, int b) => Task.FromResult(a + b));
        puppet.RegisterTarget("def", (string a, string b) => Task.FromResult(a + b));

        puppet.Run();
    }
}
