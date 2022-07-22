/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using Marionetta.Internal;
using Marionetta.Messengers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

using static NUnit.Framework.Assert;

namespace Marionetta;

[TestFixture]
public sealed class MessengerTests
{
    [Test]
    public async Task Send1Packet()
    {
        var s = Utilities.GetDefaultJsonSerializer();

        var message = new Message(
            Guid.NewGuid(),
            MessageTypes.Invoking,
            JToken.FromObject(
                new InvokingMessage(
                    "abc",
                    new[] { JToken.FromObject(123), JToken.FromObject(456) })));

        using var manipulationStream = new MemoryStream();
        var tw = new StreamWriter(manipulationStream, Encoding.UTF8);
        s.Serialize(tw, message);
        tw.Flush();
        manipulationStream.Position = 0;

        using var feedbackStream = new MemoryStream();

        using var messenger = new ActiveMessenger(
            manipulationStream.ReadAsync,
            feedbackStream.WriteAsync);

        static Task<int> abc(int a, int b) =>
            Task.FromResult(a + b);

        messenger.RegisterTarget("abc", abc);

        messenger.Start();

        await Task.Delay(500);  // DIRTY

        feedbackStream.Position = 0;
        var tr = new StreamReader(feedbackStream, Encoding.UTF8);

        var returnMessage = (Message?)s.Deserialize(tr, typeof(Message));

        AreEqual(message.Id, returnMessage?.Id);
        AreEqual(MessageTypes.Result, returnMessage?.Type);
        AreEqual(123 + 456, returnMessage?.Body?.ToObject<int>());
    }

    [Test]
    public async Task Send1PacketAndThrow()
    {
        var s = Utilities.GetDefaultJsonSerializer();

        var message = new Message(
            Guid.NewGuid(),
            MessageTypes.Invoking,
            JToken.FromObject(
                new InvokingMessage(
                    "abc",
                    new[] { JToken.FromObject(123), JToken.FromObject(456) })));

        using var manipulationStream = new MemoryStream();
        var tw = new StreamWriter(manipulationStream, Encoding.UTF8);
        s.Serialize(tw, message);
        tw.Flush();
        manipulationStream.Position = 0;

        using var feedbackStream = new MemoryStream();

        using var messenger = new ActiveMessenger(
            manipulationStream.ReadAsync,
            feedbackStream.WriteAsync);

        static Task<int> abc(int a, int b) =>
            throw new InvalidOperationException("ABC");

        messenger.RegisterTarget("abc", abc);

        messenger.Start();

        await Task.Delay(500);  // DIRTY

        feedbackStream.Position = 0;
        var tr = new StreamReader(feedbackStream, Encoding.UTF8);

        var returnMessage = (Message?)s.Deserialize(tr, typeof(Message));

        AreEqual(message.Id, returnMessage?.Id);
        AreEqual(MessageTypes.Exception, returnMessage?.Type);
        var exceptionMessage = returnMessage?.Body?.ToObject<ExceptionMessage>();
        AreEqual("System.InvalidOperationException", exceptionMessage?.Name);
        AreEqual("ABC", exceptionMessage?.Message);
    }

    [Test]
    public async Task Send2Packets()
    {
        var s = Utilities.GetDefaultJsonSerializer();

        var message0 = new Message(
            Guid.NewGuid(),
            MessageTypes.Invoking,
            JToken.FromObject(
                new InvokingMessage(
                    "abc",
                    new[] { JToken.FromObject(123), JToken.FromObject(456) })));
        var message1 = new Message(
            Guid.NewGuid(),
            MessageTypes.Invoking,
            JToken.FromObject(
                new InvokingMessage(
                    "def",
                    new[] { JToken.FromObject("aaa"), JToken.FromObject("bbb") })));

        using var manipulationStream = new MemoryStream();
        var tw = new StreamWriter(manipulationStream, Encoding.UTF8);

        s.Serialize(tw, message0);
        tw.Flush();
        manipulationStream.Write(new byte[1], 0, 1);

        s.Serialize(tw, message1);
        tw.Flush();
        manipulationStream.Write(new byte[1], 0, 1);

        manipulationStream.Position = 0;

        using var feedbackStream = new MemoryStream();

        using var messenger = new ActiveMessenger(
            manipulationStream.ReadAsync,
            feedbackStream.WriteAsync);

        static Task<int> abc(int a, int b) =>
            Task.FromResult(a + b);
        static Task<string> def(string a, string b) =>
            Task.FromResult(a + b);

        messenger.RegisterTarget("abc", abc);
        messenger.RegisterTarget("def", def);

        messenger.Start();

        await Task.Delay(500);  // DIRTY

        feedbackStream.Position = 0;
        var pr = new PacketReader(feedbackStream.ReadAsync, 1024);

        var tr0 = new StreamReader((await pr.ReadPacketAsync(default))!);
        var returnMessage0 = (Message?)s.Deserialize(tr0, typeof(Message));

        AreEqual(message0.Id, returnMessage0?.Id);
        AreEqual(MessageTypes.Result, returnMessage0?.Type);
        AreEqual(123 + 456, returnMessage0?.Body?.ToObject<int>());

        var tr1 = new StreamReader((await pr.ReadPacketAsync(default))!);
        var returnMessage1 = (Message?)s.Deserialize(tr1, typeof(Message));

        AreEqual(message1.Id, returnMessage1?.Id);
        AreEqual(MessageTypes.Result, returnMessage1?.Type);
        AreEqual("aaabbb", returnMessage1?.Body?.ToObject<string>());
    }

    //[Test]
    public void Send1PacketWithPipes()
    {
        var s = Utilities.GetDefaultJsonSerializer();

        var message = new Message(
            Guid.NewGuid(),
            MessageTypes.Invoking,
            JToken.FromObject(
                new InvokingMessage(
                    "abc",
                    new[] { JToken.FromObject(123), JToken.FromObject(456) })));

        using var serverOutStream = new AnonymousPipeServerStream(
            PipeDirection.Out, HandleInheritability.Inheritable);
        using var serverInStream = new AnonymousPipeServerStream(
            PipeDirection.In, HandleInheritability.Inheritable);

        using var clientInStream = new AnonymousPipeClientStream(
            PipeDirection.In, serverOutStream.GetClientHandleAsString());
        using var clientOutStream = new AnonymousPipeClientStream(
            PipeDirection.Out, serverInStream.GetClientHandleAsString());

        using var messenger = new ActiveMessenger(
            clientInStream.SafeReadAsync,
            clientOutStream.SafeWriteAsync);

        static Task<int> abc(int a, int b) =>
            Task.FromResult(a + b);
        messenger.RegisterTarget("abc", abc);

        messenger.Start();

        var tw = new StreamWriter(serverOutStream, Encoding.UTF8);
        s.Serialize(tw, message);
        tw.Flush();
        serverOutStream.Write(new byte[1], 0, 1);

        var tr = new StreamReader(serverInStream, Encoding.UTF8);

        var returnMessage = (Message?)s.Deserialize(tr, typeof(Message));

        AreEqual(message.Id, returnMessage?.Id);
        AreEqual(MessageTypes.Result, returnMessage?.Type);
        AreEqual(123 + 456, returnMessage?.Body?.ToObject<int>());
    }
}
