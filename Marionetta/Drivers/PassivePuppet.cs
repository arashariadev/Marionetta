/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using Marionetta.Messengers;
using System.IO.Pipes;
using System.Threading;

namespace Marionetta.Drivers;

public sealed class PassivePuppet : Driver<PassiveMessenger>
{
    private readonly AnonymousPipeClientStream clientInStream;
    private readonly AnonymousPipeClientStream clientOutStream;

    public PassivePuppet(string inStreamName, string outStreamName)
    {
        this.clientInStream = new AnonymousPipeClientStream(
            PipeDirection.In, inStreamName);
        this.clientOutStream = new AnonymousPipeClientStream(
            PipeDirection.Out, outStreamName);

        this.messenger = new PassiveMessenger(
            this.clientInStream, this.clientOutStream);
    }

    public override void Dispose()
    {
        base.Dispose();
        this.clientInStream.Dispose();
        this.clientOutStream.Dispose();
    }

    public void Run(CancellationToken ct = default) =>
        this.messenger.Run(ct);
}
