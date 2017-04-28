using System;
using System.Collections.Generic;
using System.Linq;

namespace CoPilot.ORM.Extensions
{
    public static class CollectionExtensions
    {
        public static IEnumerable<TResult> LeftJoin<TSource, TInner, TKey, TResult>(this ICollection<TSource> source, IEnumerable<TInner> other, Func<TSource, TKey> func, Func<TInner, TKey> innerkey, Func<TSource, TInner, TResult> res)
        {
            return from f in source
                   join b in other on func.Invoke(f) equals innerkey.Invoke(b) into g
                   from result in g.DefaultIfEmpty()
                   select res.Invoke(f, result);
        }
    }
}
