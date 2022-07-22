/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using Marionetta.Internal;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;

using static NUnit.Framework.Assert;

namespace Marionetta;

[TestFixture]
public sealed class PacketReaderTests
{
    private static byte[]? Extract(Stream? stream)
    {
        if (stream is MemoryStream ms)
        {
            return ms.ToArray();
        }
        else if (stream is { } s)
        {
            ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        }
        else
        {
            return null;
        }
    }

    [Test]
    public async Task Extract1Byte()
    {
        var ms = new MemoryStream(new byte[] { 0x12 });

        var pr = new PacketReader(ms.ReadAsync, 10);
        var actual0 = await pr.ReadPacketAsync(default);

        var expected0 = new byte[] { 0x12 };
        AreEqual(expected0, Extract(actual0));

        var actual1 = await pr.ReadPacketAsync(default);
        IsNull(actual1);
    }

    [Test]
    public async Task Extract2Byte()
    {
        var ms = new MemoryStream(new byte[] { 0x12, 0x34 });

        var pr = new PacketReader(ms.ReadAsync, 10);
        var actual0 = await pr.ReadPacketAsync(default);

        var expected0 = new byte[] { 0x12, 0x34 };
        AreEqual(expected0, Extract(actual0));

        var actual1 = await pr.ReadPacketAsync(default);
        IsNull(actual1);
    }

    [Test]
    public async Task Extract1ByteIn1PacketAfterTermination()
    {
        var ms = new MemoryStream(new byte[] { 0x12, 0x00 });

        var pr = new PacketReader(ms.ReadAsync, 10);
        var actual0 = await pr.ReadPacketAsync(default);

        var expected0 = new byte[] { 0x12 };
        AreEqual(expected0, Extract(actual0));

        var actual1 = await pr.ReadPacketAsync(default);
        IsNull(actual1);
    }

    [Test]
    public async Task Extract1ByteIn1PacketBeforeTermination()
    {
        var ms = new MemoryStream(new byte[] { 0x00, 0x12 });

        var pr = new PacketReader(ms.ReadAsync, 10);
        var actual0 = await pr.ReadPacketAsync(default);

        var expected0 = new byte[] { 0x12 };
        AreEqual(expected0, Extract(actual0));

        var actual1 = await pr.ReadPacketAsync(default);
        IsNull(actual1);
    }

    [Test]
    public async Task Extract2Packets()
    {
        var ms = new MemoryStream(new byte[] { 0x12, 0x00, 0x34 });

        var pr = new PacketReader(ms.ReadAsync, 10);
        var actual0 = await pr.ReadPacketAsync(default);

        var expected0 = new byte[] { 0x12 };
        AreEqual(expected0, Extract(actual0));

        var actual1 = await pr.ReadPacketAsync(default);

        var expected1 = new byte[] { 0x34 };
        AreEqual(expected1, Extract(actual1));

        var actual2 = await pr.ReadPacketAsync(default);
        IsNull(actual2);
    }

    [Test]
    public async Task BufferOverflow1()
    {
        var ms = new MemoryStream(new byte[] { 0x12, 0x34 });

        var pr = new PacketReader(ms.ReadAsync, 1);
        var actual0 = await pr.ReadPacketAsync(default);

        var expected0 = new byte[] { 0x12, 0x34 };
        AreEqual(expected0, Extract(actual0));

        var actual1 = await pr.ReadPacketAsync(default);
        IsNull(actual1);
    }

    [Test]
    public async Task BufferOverflow2()
    {
        var ms = new MemoryStream(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc });

        var pr = new PacketReader(ms.ReadAsync, 3);
        var actual0 = await pr.ReadPacketAsync(default);

        var expected0 = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc };
        AreEqual(expected0, Extract(actual0));

        var actual1 = await pr.ReadPacketAsync(default);
        IsNull(actual1);
    }

    [Test]
    public async Task BufferOverflow3()
    {
        var ms = new MemoryStream(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc });

        var pr = new PacketReader(ms.ReadAsync, 2);
        var actual0 = await pr.ReadPacketAsync(default);

        var expected0 = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc };
        AreEqual(expected0, Extract(actual0));

        var actual1 = await pr.ReadPacketAsync(default);
        IsNull(actual1);
    }

    [Test]
    public async Task BufferOverflow4()
    {
        var ms = new MemoryStream(new byte[] { 0x12, 0x34, 0x00 });

        var pr = new PacketReader(ms.ReadAsync, 1);
        var actual0 = await pr.ReadPacketAsync(default);

        var expected0 = new byte[] { 0x12, 0x34 };
        AreEqual(expected0, Extract(actual0));

        var actual1 = await pr.ReadPacketAsync(default);
        IsNull(actual1);
    }

    [Test]
    public async Task BufferOverflow5()
    {
        var ms = new MemoryStream(new byte[] { 0x00, 0x12, 0x34 });

        var pr = new PacketReader(ms.ReadAsync, 1);
        var actual0 = await pr.ReadPacketAsync(default);

        var expected0 = new byte[] { 0x12, 0x34 };
        AreEqual(expected0, Extract(actual0));

        var actual1 = await pr.ReadPacketAsync(default);
        IsNull(actual1);
    }
}
