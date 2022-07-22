/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Marionetta.Internal;

internal sealed class AsyncLock
{
    public sealed class Unlocker : IDisposable
    {
        private readonly AsyncLock parent;

        public Unlocker(AsyncLock parent) =>
            this.parent = parent;

        public void Dispose() =>
            this.parent.Unlock();
    }

    private readonly struct Waiter
    {
        public readonly TaskCompletionSource<Unlocker> tcs;
        public readonly CancellationTokenRegistration ctr;

        public Waiter(TaskCompletionSource<Unlocker> tcs, CancellationTokenRegistration ctr)
        {
            this.tcs = tcs;
            this.ctr = ctr;
        }
    }

    private readonly Unlocker ul;
    private readonly TaskCompletionSource<Unlocker> unlocker;
    private readonly Queue<Waiter> waiters = new();
    private int count;

    public AsyncLock()
    {
        this.ul = new(this);
        this.unlocker = new();
        this.unlocker.SetResult(this.ul);
    }

    public Task<Unlocker> LockAsync(CancellationToken ct)
    {
        TaskCompletionSource<Unlocker> tcs;
        lock (this.waiters)
        {
            var count = Interlocked.Increment(ref this.count);
            if (count == 1)
            {
                return this.unlocker.Task;
            }

            tcs = new TaskCompletionSource<Unlocker>();
            var ctr = ct.Register(() => tcs.TrySetCanceled());
            this.waiters.Enqueue(new Waiter(tcs, ctr));
        }

        return tcs.Task;
    }

    private void Unlock()
    {
        lock (this.waiters)
        {
            Interlocked.Decrement(ref this.count);
            if (this.waiters.Count >= 1)
            {
                var waiter = this.waiters.Dequeue();
                waiter.ctr.Dispose();
                waiter.tcs.TrySetResult(this.ul);
            }
        }
    }
}
