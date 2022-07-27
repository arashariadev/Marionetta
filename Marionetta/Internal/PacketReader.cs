/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Marionetta.Internal;

internal sealed class PacketReader
{
    private readonly Func<byte[], int, int, CancellationToken, Task<int>> reader;
    private readonly byte[] buffer;
    private int currentStartIndex;
    private int currentIndex;
    private int currentSize;
    private MemoryStream? currentStream;

    public PacketReader(
        Func<byte[], int, int, CancellationToken, Task<int>> reader, int bufferSize)
    {
        this.reader = reader;
        this.buffer = new byte[bufferSize];
    }

    public async Task<byte[]?> ReadPacketAsync(CancellationToken ct)
    {
        while (true)
        {
            // EOB
            if (this.currentIndex >= this.currentSize)
            {
                if ((this.currentIndex - this.currentStartIndex) >= 1)
                {
                    if (this.currentStream == null)
                    {
                        this.currentStream = new MemoryStream();
                    }

                    this.currentStream.Write(
                        this.buffer,
                        this.currentStartIndex,
                        this.currentIndex - this.currentStartIndex);
                    this.currentStartIndex = this.currentIndex;
                }

                var read = await this.reader(
                    this.buffer, 0, this.buffer.Length, ct).
                    ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                this.currentStartIndex = 0;
                this.currentIndex = 0;
                this.currentSize = read;
            }

            // EOP
            if (this.buffer[this.currentIndex] == 0x00)
            {
                if ((this.currentIndex - this.currentStartIndex) >= 1)
                {
                    if (this.currentStream != null)
                    {
                        var ms = this.currentStream;
                        this.currentStream = null;
                        ms.Write(
                            this.buffer,
                            this.currentStartIndex,
                            this.currentIndex - this.currentStartIndex);
                        ms.Seek(0, SeekOrigin.Begin);
                        this.currentStartIndex = this.currentIndex;
                        return ms.ToArray();
                    }
                    else
                    {
                        var ms = new MemoryStream(
                            this.buffer,
                            this.currentStartIndex,
                            this.currentIndex - this.currentStartIndex);
                        this.currentStartIndex = this.currentIndex;
                        return ms.ToArray();
                    }
                }

                this.currentStartIndex = this.currentIndex + 1;
            }

            this.currentIndex++;
        }

        if ((this.currentIndex - this.currentStartIndex) >= 1)
        {
            if (this.currentStream != null)
            {
                var ms = this.currentStream;
                this.currentStream = null;
                ms.Write(
                    this.buffer,
                    this.currentStartIndex,
                    this.currentIndex - this.currentStartIndex);
                ms.Seek(0, SeekOrigin.Begin);
                this.currentStartIndex = this.currentIndex;
                return ms.ToArray();
            }
            else
            {
                var ms = new MemoryStream(
                    this.buffer,
                    this.currentStartIndex,
                    this.currentIndex - this.currentStartIndex);
                this.currentStartIndex = this.currentIndex;
                return ms.ToArray();
            }
        }
        else
        {
            var ms = this.currentStream;
            if (ms != null)
            {
                ms.Seek(0, SeekOrigin.Begin);
                this.currentStream = null;
                return ms.ToArray();
            }
            else
            {
                return null;
            }
        }
    }
}
