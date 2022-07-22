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

public sealed class ActiveMessenger : Messenger
{
    private Thread? thread;

    public ActiveMessenger(
        Func<byte[], int, int, CancellationToken, Task<int>> manipulationIn,
        Func<byte[], int, int, CancellationToken, Task> feedbackOut) :
        base(manipulationIn, feedbackOut)
    {
    }

    public ActiveMessenger(
        Stream manipulationInStream,
        Stream feedbackOutStream) :
        this(manipulationInStream.SafeReadAsync, feedbackOutStream.SafeWriteAsync)
    {
    }

    public override void Dispose()
    {
        if (this.thread != null &&
            !this.cts.IsCancellationRequested)
        {
            this.cts.Cancel();
            this.thread.Join();
        }
    }

    public void Start()
    {
        if (this.thread == null)
        {
            this.thread = new Thread(async () =>
            {
                await this.RunAsync().
                    ConfigureAwait(false);
                await this.SendAbortRequestAsync().
                    ConfigureAwait(false);
            });
            this.thread.IsBackground = true;
            this.thread.Start();
        }
    }
}
