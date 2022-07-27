/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using DupeNukem;
using System.Threading;
using System.Threading.Tasks;

namespace Marionetta;

public static class Program
{
    public static void Main(string[] args)
    {
        var arguments = DriverFactory.ParsePuppetArguments(args);

        using var puppet = DriverFactory.CreatePuppet(arguments);

        puppet.RegisterFunc("abc", (int a, int b) => Task.FromResult(a + b));
        puppet.RegisterFunc("def", (string a, string b) => Task.FromResult(a + b));

        puppet.Start();

        using var shutdown = new ManualResetEvent(false);
        puppet.ShutdownRequested += (s, e) => shutdown.Set();

        shutdown.WaitOne();
    }
}
