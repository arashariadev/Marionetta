/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System.Diagnostics;
using System.IO;
using System.IO.Pipes;

namespace Marionetta.Drivers;

public sealed class MasterPuppet : Driver<AnonymousPipeServerStream>
{
    public MasterPuppet() :
        base(
            new(PipeDirection.In, HandleInheritability.Inheritable),
            new(PipeDirection.Out, HandleInheritability.Inheritable))
    {
    }

    public string ReceiveStreamName =>
        base.InStream.GetClientHandleAsString();

    public string SendStreamName =>
        base.OutStream.GetClientHandleAsString();

    public void Start()
    {
        base.StartReadingAsynchronously();
        Trace.WriteLine($"Marionetta: MasterPuppet started, PuppetId={Process.GetCurrentProcess().Id}");
    }

    public void Shutdown()
    {
        Trace.WriteLine("Marionetta: Send shutdown request to peer.");
        this.messenger.RequestShutdownToPeer();
    }
}
