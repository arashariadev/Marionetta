/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using DupeNukem;
using DupeNukem.Internal;
using Marionetta.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Marionetta.Drivers;

public abstract class Driver<TStream> : IMessenger, IDisposable
    where TStream : Stream
{
    private protected readonly MarionettaMessenger messenger = new();
    private readonly CancellationTokenSource cts = new();
    private Task? reading;

    private protected readonly TStream InStream;
    private protected readonly TStream OutStream;

    private readonly PacketReader inReader;

    private protected Driver(TStream inStream, TStream outStream)
    {
        this.InStream = inStream;
        this.OutStream = outStream;
        this.inReader = new(this.InStream.SafeReadAsync, 1024);

        ////////////////////////////////////////////////////////
        // Setup DupeNukem Messenger.

        this.messenger.SendRequest += (s, e) =>
        {
            var packet = Encoding.UTF8.GetBytes(e.JsonString);
            var ntPacket = new byte[packet.Length + 1];   // null-terminated
            Array.Copy(packet, ntPacket, packet.Length);

            this.OutStream.Write(ntPacket, 0, ntPacket.Length);
        };
    }

    public virtual void Dispose()
    {
        if (this.reading is { } reading)
        {
            this.reading = null;
            this.cts.Cancel();  // Force stop

            reading.Wait(1000);

            this.messenger.Dispose();

            this.InStream.Dispose();
            this.OutStream.Dispose();
        }
    }

    ///////////////////////////////////////////////////////////////

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public bool SendExceptionWithStackTrace
    {
        get => this.messenger.SendExceptionWithStackTrace;
        set => this.messenger.SendExceptionWithStackTrace = value;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public NamingStrategy MemberAccessNamingStrategy =>
        this.messenger.MemberAccessNamingStrategy;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public JsonSerializer Serializer =>
        this.messenger.Serializer;

    public string[] RegisteredMethods =>
        this.messenger.RegisteredMethods;

    public event EventHandler? ErrorDetected
    {
        add => this.messenger.ErrorDetected += value;
        remove => this.messenger.ErrorDetected -= value;
    }

    ///////////////////////////////////////////////////////////////

    private async Task StartReadingAsync()
    {
        try
        {
            while (true)
            {
                var packet = await this.inReader.ReadPacketAsync(this.cts.Token);
                if (packet == null)
                {
                    Trace.WriteLine("Marionetta: Disconnected by peer.");
                    break;
                }

                var jsonString = Encoding.UTF8.GetString(packet);
                this.messenger.ReceivedRequest(jsonString);
            }
        }
        catch (OperationCanceledException)
        {
            Trace.WriteLine("Marionetta: Canceled reading. [1]");
        }
        catch (ObjectDisposedException)
        {
            Trace.WriteLine("Marionetta: Canceled reading. [2]");
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
        }
    }

    protected void StartReadingAsynchronously() =>
        this.reading = this.StartReadingAsync();

    ///////////////////////////////////////////////////////////////

    [EditorBrowsable(EditorBrowsableState.Never)]
    public string RegisterMethod(
        string name, MethodDescriptor method, bool hasSpecifiedName) =>
        this.messenger.RegisterMethod(name, method, hasSpecifiedName);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void UnregisterMethod(
        string name, bool hasSpecifiedName) =>
        this.messenger.UnregisterMethod(name, hasSpecifiedName);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Task InvokePeerMethodAsync(
        CancellationToken ct, string methodName, params object?[] args) =>
        this.messenger.InvokePeerMethodAsync(ct, methodName, args);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Task<TR> InvokePeerMethodAsync<TR>(
        CancellationToken ct, string methodName, params object?[] args) =>
        this.messenger.InvokePeerMethodAsync<TR>(ct, methodName, args);
}
