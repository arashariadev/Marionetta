/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;

namespace Marionetta.Drivers;

public sealed class Puppet : Driver<AnonymousPipeClientStream>
{
    private Process parentProcess;
    private Process currentProcess;

    public Puppet(int parentId, string receiveStreamName, string sendStreamName) :
        base(
            new(PipeDirection.In, receiveStreamName),
            new(PipeDirection.Out, sendStreamName))
    {
        this.currentProcess = Process.GetCurrentProcess();

        try
        {
            this.parentProcess = Process.GetProcessById(parentId);
        }
        catch (ArgumentException)
        {
            // State 1: Already terminated parent process.
            this.OnExit(this, EventArgs.Empty);
        }

        this.parentProcess!.Exited += this.OnExit!;
        this.parentProcess.EnableRaisingEvents = true;

        // State 2: Covered race condition.
        if (this.parentProcess.HasExited)
        {
            this.OnExit(this, EventArgs.Empty);
        }
    }

    public override void Dispose()
    {
        if (this.parentProcess is { } parentProcess)
        {
            this.parentProcess = null!;
            parentProcess.Exited -= this.OnExit!;
            parentProcess.Dispose();
        }
        if (this.currentProcess is { } currentProcess)
        {
            this.currentProcess = null!;
            currentProcess.Dispose();
        }

        base.Dispose();
    }

    public int MarionettistId =>
        this.parentProcess.Id;

    public event EventHandler? ShutdownRequested
    {
        add => this.messenger.ShutdownRequested += value;
        remove => this.messenger.ShutdownRequested -= value;
    }

    private static void Suicide(Process currentProcess)
    {
        try
        {
            // HACK: Instantaneous suicide Kill() instead Environment.Exit().
            //   Dirty libraries can often get into unrecoverable deadlocks in native libraries.
            //   In such cases, Environment.Exit() may not allow suicide.
            currentProcess.Kill();
        }
        catch
        {
        }

#if !NETSTANDARD1_3 && !NETSTANDARD1_4
        try
        {
            // Failsafe
            Environment.Exit(0);
        }
        catch
        {
        }
#endif
    }

    private void OnExit(object sender, EventArgs e)
    {
        Trace.WriteLine(
            $"Marionetta: Detected termination for parent process: ParentId={this.parentProcess.Id}");
        Thread.Sleep(100);
        Suicide(this.currentProcess);
        Trace.WriteLine($"Marionetta: Gave up suicide...");
    }

    public void Start()
    {
        base.StartReadingAsynchronously();
        Trace.WriteLine($"Marionetta: Puppet started.");
    }
}
