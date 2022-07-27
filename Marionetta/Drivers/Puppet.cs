/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System.Diagnostics;
using System.IO.Pipes;

namespace Marionetta.Drivers;

public sealed class Puppet : Driver<AnonymousPipeClientStream>
{
    public Puppet(string receiveStreamName, string sendStreamName) :
        base(
            new(PipeDirection.In, receiveStreamName),
            new(PipeDirection.Out, sendStreamName))
    {
    }

    public void Start()
    {
        base.StartReadingAsynchronously();
        Trace.WriteLine($"Marionetta: Puppet started, PuppetId={Process.GetCurrentProcess().Id}");
    }
}
