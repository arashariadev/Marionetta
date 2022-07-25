using System.Threading;
using System.Threading.Tasks;

namespace Marionetta;

public static class Program
{
    public static void Main(string[] args)
    {
        var arguments = DriverFactory.ParsePuppetArguments(args);
        switch (arguments.AdditionalArgs[0])
        {
            case "Passive":
                {
                    using var puppet = DriverFactory.CreatePassivePuppet(arguments);

                    puppet.RegisterTarget("abc", (int a, int b) => Task.FromResult(a + b));
                    puppet.RegisterTarget("def", (string a, string b) => Task.FromResult(a + b));

                    puppet.Run();
                }
                break;
            case "Active":
                {
                    using var puppet = DriverFactory.CreateActivePuppet(arguments);

                    var abort = new ManualResetEvent(false);

                    puppet.RegisterTarget("abc", (int a, int b) => Task.FromResult(a + b));
                    puppet.RegisterTarget("def", (string a, string b) => Task.FromResult(a + b));
                    puppet.RegisterTarget("ghi", () => abort.Set());

                    puppet.Start();

                    abort.WaitOne();
                }
                break;
        }
    }
}
