/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using DupeNukem;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;

// Remove in future release.
#pragma warning disable CS0618

namespace Marionetta.Internal
{
    internal sealed class MarionettaMessenger : Messenger
    {
        private readonly ManualResetEvent accepted = new(false);

        public MarionettaMessenger()
        {
        }

        public event EventHandler? ShutdownRequested;

        public void RequestShutdownToPeer(TimeSpan? timeout = default)
        {
            base.SendControlMessageToPeer("shutdown", null);
            this.accepted.WaitOne(timeout is { } ?
                timeout.Value :
                TimeSpan.FromMilliseconds(1000));
        }

        protected override void OnReceivedControlMessage(
            string controlId, JToken? body)
        {
            switch (controlId)
            {
                case "shutdown":
                    try
                    {
                        this.ShutdownRequested?.Invoke(this, EventArgs.Empty);
                    }
                    finally
                    {
                        base.SendControlMessageToPeer("accepted", null);
                    }
                    break;
                case "accept":
                    this.accepted.Set();
                    break;
            }
        }
    }
}
