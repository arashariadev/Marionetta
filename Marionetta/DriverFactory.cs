/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using Marionetta.Drivers;
using System.IO;
using System.Linq;

namespace Marionetta;

public readonly struct PuppetArguments
{
    public readonly string ReceiveStreamName;
    public readonly string SendStreamName;
    public readonly string[] AdditionalArgs;

    public PuppetArguments(
        string receiveStreamName, string sendStreamName, string[] additionalArgs)
    {
        this.ReceiveStreamName = receiveStreamName;
        this.SendStreamName = sendStreamName;
        this.AdditionalArgs = additionalArgs;
    }

    public void Deconstruct(
        out string receiveStreamName, out string sendStreamName, out string[] additionalArgs)
    {
        receiveStreamName = this.ReceiveStreamName;
        sendStreamName = this.SendStreamName;
        additionalArgs = this.AdditionalArgs;
    }
}

public readonly struct PuppetPair
{
    public readonly MasterPuppet Master;
    public readonly Puppet Slave;

    public PuppetPair(MasterPuppet master, Puppet slave)
    {
        this.Master = master;
        this.Slave = slave;
    }

    public void Deconstruct(out MasterPuppet master, out Puppet slave)
    {
        master = this.Master;
        slave = this.Slave;
    }
}

public static class DriverFactory
{
    public static PuppetArguments ParsePuppetArguments(string[] args)
    {
        var inStreamName = args[0];
        var outStreamName = args[1];
        var additionalArgs = args.Skip(2).ToArray();
        return new(inStreamName, outStreamName, additionalArgs);
    }

    public static Puppet CreatePuppet(PuppetArguments arguments) =>
        new Puppet(arguments.ReceiveStreamName, arguments.SendStreamName);

    public static PuppetPair CreatePuppetPair()
    {
        var master = new MasterPuppet();
        var slave = new Puppet(master.SendStreamName, master.ReceiveStreamName);
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
