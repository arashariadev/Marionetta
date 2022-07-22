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

namespace Marionetta;

public sealed class Puppet : Driver<PassiveMessenger>
{
    private readonly AnonymousPipeClientStream clientInStream;
    private readonly AnonymousPipeClientStream clientOutStream;

    public Puppet(string[] args)
    {
        this.clientInStream = new AnonymousPipeClientStream(
            PipeDirection.In, args[0]);
        this.clientOutStream = new AnonymousPipeClientStream(
            PipeDirection.Out, args[1]);

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
