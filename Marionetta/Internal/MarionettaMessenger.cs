/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using DupeNukem;
using DupeNukem.Internal;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

// Remove in future release.
#pragma warning disable CS0618

namespace Marionetta.Internal
{
    internal sealed class MarionettaMessenger : Messenger
    {
        private readonly TaskCompletionSource<int> accepted = new();

        public MarionettaMessenger() =>
            // Made defaulted with stack trace.
            // Because Marionetta is IPC and both sides are supposed to know each other's origins.
            base.SendExceptionWithStackTrace = true;

        public event EventHandler? ShutdownRequested;

        public async Task RequestShutdownToPeerAsync(CancellationToken ct)
        {
            base.CancelAllSuspending();
            base.SendControlMessageToPeer("shutdown", null);

            using var _ = ct.Register(() => this.accepted.TrySetCanceled());

            await this.accepted.Task.
                ConfigureAwait(false);
        }

        protected override async void OnReceivedControlMessage(
            string controlId, JToken? body)
        {
            switch (controlId)
            {
                case "shutdown":
                    base.CancelAllSuspending();
                    try
                    {
                        base.SendControlMessageToPeer("accepted", null);
                    }
                    finally
                    {
                        await this.SynchContext.Bind();
                        this.ShutdownRequested?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case "accepted":
                    this.accepted.TrySetResult(0);
                    break;
            }
        }
    }
}
