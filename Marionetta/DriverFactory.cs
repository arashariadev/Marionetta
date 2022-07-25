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
    public readonly string InStreamName;
    public readonly string OutStreamName;
    public readonly string[] AdditionalArgs;

    public PuppetArguments(
        string inStreamName, string outStreamName, string[] additionalArgs)
    {
        this.InStreamName = inStreamName;
        this.OutStreamName = outStreamName;
        this.AdditionalArgs = additionalArgs;
    }
}


public static class DriverFactory
{
    public static PuppetArguments ParsePuppetArguments(string[] args)
    {
        var inStreamName = args[0];
        var outStreamName = args[1];
        var additionalArgs = args.Skip(2).ToArray();
        return new PuppetArguments(inStreamName, outStreamName, additionalArgs);
    }

    public static ActivePuppet CreateActivePuppet(PuppetArguments arguments) =>
        new ActivePuppet(arguments.InStreamName, arguments.OutStreamName);

    public static PassivePuppet CreatePassivePuppet(PuppetArguments arguments) =>
        new PassivePuppet(arguments.InStreamName, arguments.OutStreamName);

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

        return new Marionettist(puppetPath, workingDirectoryPath, additionalArgs);
    }
}
