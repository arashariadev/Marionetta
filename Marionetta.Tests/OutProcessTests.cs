/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;

using static NUnit.Framework.Assert;

#pragma warning disable CS0436 // Type conflicts with imported type

namespace Marionetta;

#if NET48

[TestFixture]
public sealed class OutProcessTests
{
    [Test]
    public async Task OutProcess1()
    {
#if DEBUG
        var configuration = "Debug";
#else
        var configuration = "Release";
#endif

        var puppetPath = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(this.GetType().Assembly.Location),
            $@"..\..\..\..\Marionetta.Tests.Puppet\bin\{configuration}\{ThisAssembly.AssemblyMetadata.TargetFramework}\Marionetta.Tests.Puppet.exe"));

        using var marionettist = new Marionettist(puppetPath);

        static Task<int> abc(int a, int b) =>
            Task.FromResult(a + b);
        static Task<string> def(string a, string b) =>
            Task.FromResult(a + b);

        marionettist.RegisterTarget("abc", abc);
        marionettist.RegisterTarget("def", def);

        marionettist.Start();

        var result1 = await marionettist.InvokeTargetAsync<int>("abc", 123, 456);
        var result2 = await marionettist.InvokeTargetAsync<string>("def", "aaa", "bbb");

        AreEqual(123 + 456, result1);
        AreEqual("aaabbb", result2);
    }
}

#endif
