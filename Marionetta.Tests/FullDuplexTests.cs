/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using Marionetta.Messengers;
using NUnit.Framework;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

using static NUnit.Framework.Assert;

namespace Marionetta;

[TestFixture]
public sealed class FullDuplexTests
{
    [Test]
    public async Task Send1Packet()
    {
        using var serverOutStream = new AnonymousPipeServerStream(
            PipeDirection.Out, HandleInheritability.Inheritable);
        using var serverInStream = new AnonymousPipeServerStream(
            PipeDirection.In, HandleInheritability.Inheritable);

        using var clientInStream = new AnonymousPipeClientStream(
            PipeDirection.In, serverOutStream.GetClientHandleAsString());
        using var clientOutStream = new AnonymousPipeClientStream(
            PipeDirection.Out, serverInStream.GetClientHandleAsString());

        using var serverMessenger = new ActiveMessenger(
            serverInStream, serverOutStream);
        using var clientMessenger = new ActiveMessenger(
            clientInStream, clientOutStream);

        static Task<int> abc(int a, int b) =>
            Task.FromResult(a + b);
        static Task<string> def(string a, string b) =>
            Task.FromResult(a + b);

        clientMessenger.RegisterTarget("abc", abc);
        clientMessenger.RegisterTarget("def", def);

        serverMessenger.Start();
        clientMessenger.Start();

        var result1 = await serverMessenger.InvokeTargetAsync<int>("abc", 123, 456);
        var result2 = await serverMessenger.InvokeTargetAsync<string>("def", "aaa", "bbb");

        AreEqual(123 + 456, result1);
        AreEqual("aaabbb", result2);
    }
}
