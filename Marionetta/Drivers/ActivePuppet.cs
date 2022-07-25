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

namespace Marionetta.Drivers;

public sealed class ActivePuppet : Driver<ActiveMessenger>
{
    private readonly AnonymousPipeClientStream clientInStream;
    private readonly AnonymousPipeClientStream clientOutStream;

    public ActivePuppet(string inStreamName, string outStreamName)
    {
        this.clientInStream = new AnonymousPipeClientStream(
            PipeDirection.In, inStreamName);
        this.clientOutStream = new AnonymousPipeClientStream(
            PipeDirection.Out, outStreamName);

        this.messenger = new ActiveMessenger(
            this.clientInStream, this.clientOutStream);
    }

    public override void Dispose()
    {
        base.Dispose();
        this.clientInStream.Dispose();
        this.clientOutStream.Dispose();
    }

    public void Start() =>
        this.messenger.Start();
}
