/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using Marionetta.Drivers;
using System;
using System.IO;
using System.Linq;

namespace Marionetta;

public readonly struct PuppetArguments
{
    public readonly int ParentId;
    public readonly string ReceiveStreamName;
    public readonly string SendStreamName;
    public readonly string[] AdditionalArgs;

    public PuppetArguments(
        int parentId, string receiveStreamName, string sendStreamName, string[] additionalArgs)
    {
        this.ParentId = parentId;
        this.ReceiveStreamName = receiveStreamName;
        this.SendStreamName = sendStreamName;
        this.AdditionalArgs = additionalArgs;
    }

    public void Deconstruct(
        out int parentId, out string receiveStreamName, out string sendStreamName, out string[] additionalArgs)
    {
        parentId = this.ParentId;
        receiveStreamName = this.ReceiveStreamName;
        sendStreamName = this.SendStreamName;
        additionalArgs = this.AdditionalArgs;
    }
}

public readonly struct PuppetPair
{
    public readonly MasterPuppet Master;
    public readonly SlavePuppet Slave;

    public PuppetPair(MasterPuppet master, SlavePuppet slave)
    {
        this.Master = master;
        this.Slave = slave;
    }

    public void Deconstruct(out MasterPuppet master, out SlavePuppet slave)
    {
        master = this.Master;
        slave = this.Slave;
    }
}

public static class DriverFactory
{
    public static PuppetArguments ParsePuppetArguments(string[] args)
    {
        if (args.Length < 3)
        {
            throw new ArgumentException(
                "Invalid puppet process argument format.");
        }

        var parentId = int.Parse(args[0]);
        var inStreamName = args[1];
        var outStreamName = args[2];
        var additionalArgs = args.Skip(3).ToArray();

        return new(parentId, inStreamName, outStreamName, additionalArgs);
    }

    public static Puppet CreatePuppet(PuppetArguments arguments)
    {
        return new Puppet(
            arguments.ParentId,
            arguments.ReceiveStreamName,
            arguments.SendStreamName);
    }

    public static PuppetPair CreatePuppetPair()
    {
        var master = new MasterPuppet();
        var slave = new SlavePuppet(
            master.SendStreamName,  // complementary
            master.ReceiveStreamName);
        return new(master, slave);
    }

    public static Marionettist CreateMarionettist(
        string puppetPath,
        string? workingDirectoryPath = null,
        params string[] additionalArgs)
    {
        if (workingDirectoryPath == null)
        {
            workingDirectoryPath = Path.GetDirectoryName(puppetPath) ??
                Path.DirectorySeparatorChar.ToString();
        }

        return new(puppetPath, workingDirectoryPath, additionalArgs);
    }
}
