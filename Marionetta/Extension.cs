/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using Marionetta.Drivers;
using Marionetta.Messengers;
using System.Threading.Tasks;

namespace Marionetta;

public static class Extension
{
    public static Task InvokeTargetAsync(
        this Messenger messenger, string name, params object?[] args) =>
        messenger.InvokeTargetAsync<object?>(name, args);

    public static Task InvokeTargetAsync<TMessenger>(
        this Driver<TMessenger> driver, string name, params object?[] args)
        where TMessenger : Messenger =>
        driver.InvokeTargetAsync<object?>(name, args);
}
