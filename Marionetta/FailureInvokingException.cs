/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System;

namespace Marionetta;

public sealed class FailureInvokingException : Exception
{
    public readonly Guid Id;
    public readonly string Name;

    public FailureInvokingException(Guid id, string name, string message) :
        base(message)
    {
        this.Id = id;
        this.Name = name;
    }

    public override string ToString() =>
        $"{this.Name}: {this.Message}";
}