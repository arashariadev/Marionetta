/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using Marionetta.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Marionetta.Messengers;

public abstract class Messenger : IDisposable
{
    private readonly JsonSerializer serializer = Utilities.GetDefaultJsonSerializer();

    private readonly Func<byte[], int, int, CancellationToken, Task<int>> manipulationIn;
    private readonly Func<byte[], int, int, CancellationToken, Task> feedbackOut;

    private readonly AsyncLock feedbackOutLocker = new();

    private readonly Dictionary<string, Delegate> invokingTargets = new();
    private readonly Dictionary<Guid, TaskCompletionSource<JToken?>> waitings = new();

    private protected readonly CancellationTokenSource cts = new();

    private protected Messenger(
        Func<byte[], int, int, CancellationToken, Task<int>> manipulationIn,
        Func<byte[], int, int, CancellationToken, Task> feedbackOut)
    {
        this.manipulationIn = manipulationIn;
        this.feedbackOut = feedbackOut;

    }

    public abstract void Dispose();

    public void RegisterTarget(string name, Delegate target)
    {
        lock (this.invokingTargets)
        {
            this.invokingTargets.Add(name, target);
        }
    }

    public void UnregisterTarget(string name)
    {
        lock (this.invokingTargets)
        {
            this.invokingTargets.Remove(name);
        }
    }

    private static async Task CopyToAsync(
        MemoryStream stream,
        Func<byte[], int, int, CancellationToken, Task> to,
        CancellationToken ct)
    {
        var buffer = new byte[4096];
        while (true)
        {
            var read = stream.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }
            await to(buffer, 0, read, ct).
                ConfigureAwait(false);
        }
    }

    public async Task<TR> InvokeTargetAsync<TR>(
        string name, params object?[] args)
    {
        var invokingMessage = new InvokingMessage(
            name,
            args.Select(arg => arg != null ? JToken.FromObject(arg) : null).ToArray());
        var message = new Message(
            Guid.NewGuid(),
            MessageTypes.Invoking,
            JToken.FromObject(invokingMessage));

        using var ms = new MemoryStream();
        var tw = new StreamWriter(ms, Encoding.UTF8);
        this.serializer.Serialize(tw, message);
        tw.Flush();
        ms.Write(new byte[1], 0, 1);
        ms.Position = 0;

        var tcs = new TaskCompletionSource<JToken?>();

        lock (this.waitings)
        {
            this.waitings.Add(message.Id, tcs);
        }

        try
        {
            using (var locker = await this.feedbackOutLocker.LockAsync(default).
                ConfigureAwait(false))   // TODO: ct
            {
                await CopyToAsync(ms, this.feedbackOut, default(CancellationToken)).   // TODO: ct
                    ConfigureAwait(false);
            }

            var result = await tcs.Task.
                ConfigureAwait(false);

            return result != null ? result.ToObject<TR>()! : default(TR)!;
        }
        finally
        {
            lock (this.waitings)
            {
                this.waitings.Remove(message.Id);
            }
        }
    }

    private protected async Task SendAbortRequestAsync()
    {
        var message = new Message(
            Guid.NewGuid(),
            MessageTypes.AbortRequest,
            null);

        using var ms = new MemoryStream();
        var tw = new StreamWriter(ms, Encoding.UTF8);
        this.serializer.Serialize(tw, message);
        tw.Flush();
        ms.Write(new byte[1], 0, 1);
        ms.Position = 0;

        try
        {
            using (var locker = await this.feedbackOutLocker.LockAsync(default).
                ConfigureAwait(false))
            {
                await CopyToAsync(ms, this.feedbackOut, default(CancellationToken)).
                    ConfigureAwait(false);
            }
        }
        catch
        {
        }
    }

    private protected async Task RunAsync()
    {
        var reader = new PacketReader(this.manipulationIn, 65536);

        while (true)
        {
            try
            {
                using var ps = await reader.ReadPacketAsync(this.cts.Token).
                    ConfigureAwait(false);
                if (ps == null)
                {
                    break;
                }

                var tr = new StreamReader(ps, Encoding.UTF8, true);
                var message = (Message?)this.serializer.Deserialize(tr, typeof(Message))!;

                Delegate? TryGetTarget(string name)
                {
                    lock (this.invokingTargets)
                    {
                        return this.invokingTargets.TryGetValue(
                            name, out var target) ? target : null;
                    }
                }
                TaskCompletionSource<JToken?>? TryGetWaiting(Guid id)
                {
                    lock (this.waitings)
                    {
                        return this.waitings.TryGetValue(
                            id, out var waiting) ? waiting : null;
                    }
                }

                switch (message)
                {
                    case (var id, MessageTypes.Invoking, var body):
                        if (body?.ToObject<InvokingMessage>() is (var name1, var arguments) &&
                            TryGetTarget(name1) is { } target &&
                            target.Method.GetParameters() is { } parameters &&
                            arguments.Length == parameters.Length)
                        {
                            async void InvokeAsynchronously()
                            {
                                using var ms = new MemoryStream();
                                var tw = new StreamWriter(ms, Encoding.UTF8);

                                try
                                {
                                    var boundArguments = arguments.
                                        Zip(parameters, (a, p) => a?.ToObject(p.ParameterType)).
                                        ToArray();

                                    var result = target.DynamicInvoke(boundArguments);

                                    if (result is Task task)
                                    {
                                        result = await GenericResultExtractor.UntypeTask(task).
                                            ConfigureAwait(false);
                                    }

                                    var returnMessage = new Message(
                                        id,
                                        MessageTypes.Result,
                                        result != null ? JToken.FromObject(result) : null);
                                    this.serializer.Serialize(tw, returnMessage);
                                }
                                catch (TargetInvocationException ex)
                                {
                                    var resultMessage = new ExceptionMessage(
                                        ex.InnerException!.GetType().FullName!,
                                        ex.InnerException!.Message);
                                    var returnMessage = new Message(
                                        id,
                                        MessageTypes.Exception,
                                        JToken.FromObject(resultMessage));
                                    this.serializer.Serialize(tw, returnMessage);
                                }

                                tw.Flush();
                                ms.Write(new byte[1], 0, 1);
                                ms.Position = 0;

                                using (var locker = await this.feedbackOutLocker.LockAsync(this.cts.Token).
                                    ConfigureAwait(false))
                                {
                                    await CopyToAsync(ms, this.feedbackOut, this.cts.Token).
                                        ConfigureAwait(false);
                                }
                            }

                            InvokeAsynchronously();
                        }
                        break;

                    case (var id, MessageTypes.Result, var body):
                        if (TryGetWaiting(id) is { } waiting1)
                        {
                            waiting1.TrySetResult(body);
                        }
                        break;

                    case (var id, MessageTypes.Exception, var body):
                        if (body?.ToObject<ExceptionMessage>() is (var name2, var em) &&
                            TryGetWaiting(id) is { } waiting2)
                        {
                            var ex = new FailureInvokingException(id, name2, em);
                            waiting2.TrySetException(ex);
                        }
                        break;
                    case (var id, MessageTypes.AbortRequest, _):
                        return;
                }
            }
            catch (OperationCanceledException)
            {
                if (this.cts.Token.IsCancellationRequested)
                {
                    break;
                }
            }
            catch
            {
            }
        }
    }
}
