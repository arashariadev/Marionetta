/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using Marionetta.Internal;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Marionetta.Messengers;

public sealed class PassiveMessenger : Messenger
{
    public PassiveMessenger(
        Func<byte[], int, int, CancellationToken, Task<int>> manipulationIn,
        Func<byte[], int, int, CancellationToken, Task> feedbackOut) :
        base(manipulationIn, feedbackOut)
    {
    }

    public PassiveMessenger(
        Stream manipulationInStream,
        Stream feedbackOutStream) :
        this(manipulationInStream.SafeReadAsync, feedbackOutStream.SafeWriteAsync)
    {
    }

    public override void Dispose()
    {
        if (!this.cts.IsCancellationRequested)
        {
            this.cts.Cancel();
        }
    }

    public void Run(CancellationToken ct)
    {
        using var _ = ct.Register(() => this.cts.Cancel());

        var tcs = new TaskCompletionSource<int>();

        Task.Factory.StartNew(async () =>
        {
            try
            {
                await base.RunAsync();
                tcs.TrySetResult(0);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        tcs.Task.ConfigureAwait(false).
            GetAwaiter().
            GetResult();
    }
}
