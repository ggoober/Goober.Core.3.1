using System;
using System.Collections.Generic;
using System.Linq;

namespace Goober.Core.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> SplitByParts<T>(this IEnumerable<T> list, int parts)
        {
            return list.Select((item, index) => new { index, item })
                       .GroupBy(x => x.index % parts)
                       .Select(x => x.Select(y => y.item));
        }

        public static IEnumerable<List<TSource>> SplitByCount<TSource>(this IEnumerable<TSource> list, int chunkCount)
        {
            if (list == null)
            {
                yield break;
            }

            if (chunkCount == 0)
            {
                throw new NotImplementedException();
            }

            var buffer = new List<TSource>(chunkCount);
            foreach (var source in list)
            {
                if (buffer.Count == chunkCount)
                {
                    yield return buffer;
                    buffer = new List<TSource>(chunkCount);
                }

                buffer.Add(source);
            }

            if (buffer.Count != 0)
            {
                yield return buffer;
            }
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> items)
        {
            return items == null || !items.Any();
        }

        public static bool IsNotEmpty<T>(this IEnumerable<T> items)
        {
            return items != null && items.Any();
        }

        public static bool IsOneOf<T>(this T value, params T[] items)
        {
            return items.Any(o => o.Equals(value));
        }

        public static bool In<TSource>(this TSource instance, params TSource[] set)
        {
            return set.Contains(instance);
        }

        public static bool NotIn<TSource>(this TSource instance, params TSource[] set)
        {
            return !instance.In(set);
        }
    }
}
