/////////////////////////////////////////////////////////////////////////////////////
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

public abstract class Driver<TMessenger> : IDisposable
    where TMessenger : Messenger
{
    private protected TMessenger messenger = null!;

    private protected Driver()
    {
    }

    public virtual void Dispose() =>
        this.messenger.Dispose();

    public void RegisterTarget(string name, Delegate target) =>
        this.messenger.RegisterTarget(name, target);

    public Task<TR> InvokeTargetAsync<TR>(
        string name, params object?[] args) =>
        this.messenger.InvokeTargetAsync<TR>(name, args);
}
