/////////////////////////////////////////////////////////////////////////////////////
//
// Marionetta - Split dirty component into sandboxed outprocess.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
/////////////////////////////////////////////////////////////////////////////////////

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Marionetta.Internal;

public static class Utilities
{
    public static JsonSerializer GetDefaultJsonSerializer()
    {
        var defaultNamingStrategy = new CamelCaseNamingStrategy();
        var serializer = new JsonSerializer
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateParseHandling = DateParseHandling.DateTimeOffset,
            DateTimeZoneHandling = DateTimeZoneHandling.Local,
            NullValueHandling = NullValueHandling.Include,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            ContractResolver = new DefaultContractResolver { NamingStrategy = defaultNamingStrategy, },
        };
        serializer.Converters.Add(new StringEnumConverter(defaultNamingStrategy));
        return serializer;
    }

#if NET35
    internal static IEnumerable<TR> Zip<T1, T2, TR>(
        this IEnumerable<T1> e1, IEnumerable<T2> e2, Func<T1, T2, TR> selector)
    {
        using var er1 = e1.GetEnumerator();
        using var er2 = e2.GetEnumerator();

        while (er1.MoveNext() && er2.MoveNext())
        {
            yield return selector(er1.Current, er2.Current);
        }
    }
#endif

    public static Task<int> SafeReadAsync(
        this Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<int>();
        var cr = ct.Register(() => tcs.TrySetCanceled());

#if NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_5 || NETSTANDARD1_6
        stream.ReadAsync(buffer, offset, count, ct).
            ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    tcs.TrySetException(task.Exception);
                }
                else if (task.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult(task.Result);
                }
            });
#else
        var ar = stream.BeginRead(
            buffer, offset, count, ar =>
            {
                try
                {
                    tcs.TrySetResult(stream.EndRead(ar));
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
                cr.Dispose();
            },
            null);
        if (ar.CompletedSynchronously)
        {
            cr.Dispose();
            try
            {
                tcs.TrySetResult(stream.EndRead(ar));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }
#endif   

        return tcs.Task;
    }

    public static Task SafeWriteAsync(
        this Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<int>();
        var cr = ct.Register(() => tcs.TrySetCanceled());

#if NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_5 || NETSTANDARD1_6 || NETCOREAPP1_0 || NETCOREAPP1_1
        stream.WriteAsync(buffer, offset, count, ct).
            ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    tcs.TrySetException(task.Exception);
                }
                else if (task.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult(0);
                }
            });
#else
        var ar = stream.BeginWrite(
            buffer, offset, count, ar =>
            {
                try
                {
                    stream.EndWrite(ar);
                    tcs.TrySetResult(0);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
                cr.Dispose();
            },
            null);
        if (ar.CompletedSynchronously)
        {
            cr.Dispose();
            try
            {
                stream.EndWrite(ar);
                tcs.TrySetResult(0);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }
#endif

        return tcs.Task;
    }
}
