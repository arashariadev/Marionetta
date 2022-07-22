/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Marionetta.Internal;

internal enum MessageTypes
{
    Invoking,
    Result,
    Exception,
    AbortRequest,
}

internal readonly struct Message
{
    public readonly Guid Id;
    public readonly MessageTypes Type;
    public readonly JToken? Body;

    [JsonConstructor]
    public Message(Guid id, MessageTypes type, JToken? body)
    {
        this.Id = id;
        this.Type = type;
        this.Body = body;
    }

    public void Deconstruct(out Guid id, out MessageTypes type, out JToken? body)
    {
        id = this.Id;
        type = this.Type;
        body = this.Body;
    }
}

internal readonly struct InvokingMessage
{
    public readonly string Name;
    public readonly JToken?[] Arguments;

    [JsonConstructor]
    public InvokingMessage(string name, JToken?[] arguments)
    {
        this.Name = name;
        this.Arguments = arguments;
    }

    public void Deconstruct(out string name, out JToken?[] arguments)
    {
        name = this.Name;
        arguments = this.Arguments;
    }
}

internal readonly struct ExceptionMessage
{
    public readonly string Name;
    public readonly string Message;

    [JsonConstructor]
    public ExceptionMessage(string name, string message)
    {
        this.Name = name;
        this.Message = message;
    }

    public void Deconstruct(out string name, out string message)
    {
        name = this.Name;
        message = this.Message;
    }
}
