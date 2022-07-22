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
using System.Threading.Tasks;

namespace Marionetta.Internal;

internal class GenericResultExtractor
{
    private static readonly Dictionary<Type, GenericResultExtractor> extractors = new();

    static GenericResultExtractor() =>
        extractors.Add(typeof(Task), new GenericResultExtractor());

    public static Task<object?> UntypeTask(Task task)
    {
        var taskType = task.GetType();
        GenericResultExtractor extractor;
        lock (extractors)
        {
            if (!extractors.TryGetValue(taskType, out extractor!))
            {
                var resultType = taskType.GetGenericArguments()[0];
                extractor = (GenericResultExtractor)Activator.CreateInstance(
                    typeof(GenericResultExtractorT<>).MakeGenericType(resultType))!;
                extractors.Add(taskType, extractor);
            }
        }
        return extractor.Untype(task);
    }

    protected GenericResultExtractor()
    {
    }

    protected virtual async Task<object?> Untype(Task task)
    {
        await task.ConfigureAwait(false);
        return null;
    }

    private sealed class GenericResultExtractorT<T> : GenericResultExtractor
    {
        protected override async Task<object?> Untype(Task task) =>
            await ((Task<T>)task).ConfigureAwait(false);
    }
}
