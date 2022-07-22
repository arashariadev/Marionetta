﻿/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using Marionetta.Messengers;
using System;
using System.Threading.Tasks;

namespace Marionetta;

public static class Extension
{
    public static Task InvokeTargetAsync(
        this Messenger messenger, string name, params object?[] args) =>
        messenger.InvokeTargetAsync<object?>(name, args);

    public static Task InvokeTargetAsync(
        this Puppet puppet, string name, params object?[] args) =>
        puppet.InvokeTargetAsync<object?>(name, args);

    public static Task InvokeTargetAsync(
        this Marionettist marionettist, string name, params object?[] args) =>
        marionettist.InvokeTargetAsync<object?>(name, args);
}