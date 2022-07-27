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

// Remove in future release.
#pragma warning disable CS0618

namespace Marionetta.Internal
{
    internal sealed class MarionettaMessenger : Messenger
    {
        public MarionettaMessenger()
        {
        }

        public event EventHandler? ShutdownRequested;

        public void RequestShutdownToPeer() =>
            base.SendControlMessageToPeer("shutdown", null);

        protected override void OnReceivedControlMessage(
            string controlId, JToken? body)
        {
            switch (controlId)
            {
                case "shutdown":
                    this.ShutdownRequested?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }
}
