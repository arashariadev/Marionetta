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
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Marionetta.Drivers;

public sealed class PuppetDiedEventArgs : EventArgs
{
}

public sealed class Marionettist : Driver<AnonymousPipeServerStream>
{
    private Process puppetProcess = new();
    private CancellationTokenSource exited = new();

    public Marionettist(
        string puppetPath,
        string workingDirectoryPath,
        string[] additionalArgs) :
        base(
            new(PipeDirection.In, HandleInheritability.Inheritable),
            new(PipeDirection.Out, HandleInheritability.Inheritable))
    {
        ////////////////////////////////////////////////////////
        // Initialze child process.

        var (appPath, arg0) =
            Utilities.GetDotNetApplicationInvokingString(puppetPath);
        this.puppetProcess.StartInfo.FileName = appPath;
        this.puppetProcess.StartInfo.Arguments =
            $"{arg0} {Process.GetCurrentProcess().Id} {base.OutStream.GetClientHandleAsString()} {base.InStream.GetClientHandleAsString()} " +
            string.Join(" ", additionalArgs);
        this.puppetProcess.StartInfo.UseShellExecute = false;
        this.puppetProcess.StartInfo.CreateNoWindow = true;
#if !NETSTANDARD1_3 && !NETSTANDARD1_4 && !NETSTANDARD1_5 && !NETSTANDARD1_6
        this.puppetProcess.StartInfo.ErrorDialog = false;
#endif
        this.puppetProcess.StartInfo.WorkingDirectory = workingDirectoryPath;

        this.puppetProcess.Exited += this.OnExited!;
        this.puppetProcess.EnableRaisingEvents = true;
    }

    public override void Dispose()
    {
        if (this.puppetProcess is { } puppetProcess)
        {
            this.puppetProcess = null!;

            base.Dispose();

            var watcher = new Thread(() =>
            {
                for (var index = 0; index < 10; index++)
                {
                    if (this.exited.IsCancellationRequested)
                    {
                        Trace.WriteLine($"Marionetta: Puppet terminated, PuppetId={puppetProcess.Id}");
                        puppetProcess.Dispose();
                        puppetProcess.Exited -= this.OnExited!;
                        return;
                    }

                    Trace.WriteLine($"Marionetta: Waiting puppet termination, PuppetId={puppetProcess.Id}, Count=[{index + 1}/10]");
                    Thread.Sleep(TimeSpan.FromMilliseconds(1000));
                }

                Trace.WriteLine($"Marionetta: Puppet stucked, kill it, PuppetId={puppetProcess.Id}");
                puppetProcess.Kill();
                puppetProcess.Dispose();
                puppetProcess.Exited -= this.OnExited!;
            });

            watcher.IsBackground = true;
            watcher.Start();
        }
    }

    public int PuppetId =>
        this.puppetProcess.Id;

    private void OnExited(object sender, EventArgs e)
    {
        base.messenger.CancelAllSuspending();
        base.OnErrorDetected(this, new PuppetDiedEventArgs());

        this.exited.Cancel();
    }

    public void Start()
    {
        base.StartReadingAsynchronously();
        this.puppetProcess.Start();
        Trace.WriteLine(
            $"Marionetta: Marionettist started, PuppetId={this.puppetProcess.Id}");
    }

    public async Task ShutdownAsync(CancellationToken ct)
    {
        // Non-atomic graceful shutdown
        if (!this.exited.IsCancellationRequested)
        {
            Trace.WriteLine("Marionetta: Send shutdown request to peer.");

            // Atomic graceful shutdown
            using var _ = ct.Register(() => this.exited.Cancel());
            try
            {
                await base.messenger.RequestShutdownToPeerAsync(
                    this.exited.Token).
                    ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // Canceled by caller
                if (ct.IsCancellationRequested)
                {
                    throw;
                }
            }
        }
    }
}
