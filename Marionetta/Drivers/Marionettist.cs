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
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace Marionetta.Drivers;

public sealed class Marionettist : Driver<AnonymousPipeServerStream>
{
    private Process puppetProcess = new();
    private bool puppetProcessExited;

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

        this.puppetProcess.StartInfo.FileName = puppetPath;
        this.puppetProcess.StartInfo.Arguments =
            $"{base.OutStream.GetClientHandleAsString()} {base.InStream.GetClientHandleAsString()} " +
            string.Join(" ", additionalArgs);
        this.puppetProcess.StartInfo.UseShellExecute = false;
        this.puppetProcess.StartInfo.CreateNoWindow = true;
#if !NETSTANDARD1_3 && !NETSTANDARD1_4 && !NETSTANDARD1_5 && !NETSTANDARD1_6
        this.puppetProcess.StartInfo.ErrorDialog = false;
#endif
        this.puppetProcess.StartInfo.WorkingDirectory = workingDirectoryPath;

        this.puppetProcess.Exited += (s, e) => this.puppetProcessExited = true;
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
                    if (this.puppetProcessExited)
                    {
                        Trace.WriteLine($"Marionetta: Puppet terminated, PuppetId={puppetProcess.Id}");
                        puppetProcess.Dispose();
                        return;
                    }

                    Trace.WriteLine($"Marionetta: Waiting puppet termination, PuppetId={puppetProcess.Id}, Count=[{index + 1}/10]");
                    Thread.Sleep(TimeSpan.FromMilliseconds(1000));
                }

                Trace.WriteLine($"Marionetta: Puppet stucked, kill it, PuppetId={puppetProcess.Id}");
                puppetProcess.Kill();
                puppetProcess.Dispose();
            });

            watcher.IsBackground = false;   // Force alives for watcher in process terminating.
            watcher.Start();
        }
    }

    public void Start()
    {
        base.StartReadingAsynchronously();
        this.puppetProcess.Start();
        Trace.WriteLine($"Marionetta: Marionettist started, PuppetId={this.puppetProcess.Id}");
    }
}
