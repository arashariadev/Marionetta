/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using DupeNukem;
using Marionetta.Drivers;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using static NUnit.Framework.Assert;

namespace Marionetta;

[TestFixture]
public sealed class OutProcessTests
{
#if DEBUG
    private static readonly string configuration = "Debug";
#else
    private static readonly string configuration = "Release";
#endif
#if NETFRAMEWORK
    private static readonly string fileName = "Marionetta.Tests.Puppet.exe";
#else
    private static readonly string fileName = "Marionetta.Tests.Puppet.dll";
#endif

    private static readonly string puppetPath = Path.GetFullPath(Path.Combine(
        Path.GetDirectoryName(typeof(OutProcessTests).Assembly.Location) ?? Path.DirectorySeparatorChar.ToString(),
        $@"..\..\..\..\Marionetta.Tests.Puppet\bin\{configuration}\{ThisAssembly.AssemblyMetadata.TargetFramework}\{fileName}"));

    [Test]
    public async Task OutProcessPuppet()
    {
        using var marionettist = DriverFactory.CreateMarionettist(puppetPath);

        static Task<int> abc(int a, int b) =>
            Task.FromResult(a + b);
        static Task<string> def(string a, string b) =>
            Task.FromResult(a + b);

        marionettist.RegisterFunc<int, int, int>("abc", abc);
        marionettist.RegisterFunc<string, string, string>("def", def);

        marionettist.Start();

        var result1 = await marionettist.InvokePeerMethodAsync<int>("abc", 123, 456);
        var result2 = await marionettist.InvokePeerMethodAsync<string>("def", "aaa", "bbb");

        AreEqual(123 + 456, result1);
        AreEqual("aaabbb", result2);

        await marionettist.ShutdownAsync(default);
    }

    [Test]
    public async Task OutProcessDyingPuppet()
    {
        using var marionettist = DriverFactory.CreateMarionettist(puppetPath);

        static Task<int> abc(int a, int b) =>
            Task.FromResult(a + b);
        static Task<string> def(string a, string b) =>
            Task.FromResult(a + b);

        marionettist.RegisterFunc<int, int, int>("abc", abc);
        marionettist.RegisterFunc<string, string, string>("def", def);

        EventArgs? re = null;
        marionettist.ErrorDetected += (s, e) => re = e;

        marionettist.Start();

        var result1Task = marionettist.InvokePeerMethodAsync<int>("abc", 123, 456);

        var p = Process.GetProcessById(marionettist.PuppetId);
        p.Kill();

        try
        {
            await result1Task;
            Fail();
        }
        catch (TaskCanceledException)
        {
        }

        await marionettist.ShutdownAsync(default);

        IsInstanceOf<PuppetDiedEventArgs>(re);
    }
}
