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
using System.Threading.Tasks;

using static NUnit.Framework.Assert;

namespace Marionetta;

[TestFixture]
public sealed class InMemoryFullDuplexTests
{
    [Test]
    public async Task Send1Packet()
    {
        using var masterPuppet = new MasterPuppet();
        using var slavePuppet = new Puppet(
            masterPuppet.SendStreamName, masterPuppet.ReceiveStreamName);

        var tcs = new TaskCompletionSource<bool>();
        slavePuppet.ShutdownRequested += (s, e) => tcs.TrySetResult(true);

        static Task<int> abc(int a, int b) =>
            Task.FromResult(a + b);
        static Task<string> def(string a, string b) =>
            Task.FromResult(a + b);

        slavePuppet.RegisterFunc<int, int, int>("abc", abc);
        slavePuppet.RegisterFunc<string, string, string>("def", def);

        slavePuppet.Start();
        masterPuppet.Start();

        var result1 = await masterPuppet.InvokePeerMethodAsync<int>("abc", 123, 456);
        var result2 = await masterPuppet.InvokePeerMethodAsync<string>("def", "aaa", "bbb");

        AreEqual(123 + 456, result1);
        AreEqual("aaabbb", result2);

        await Task.WhenAll(
            masterPuppet.ShutdownAsync(default),
            tcs.Task);
    }
}
