using System;
using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Context.Query;

namespace CoPilot.ORM.Database.Commands.SqlWriters.Interfaces
{
    public interface IQueryBuilder
    {
        QuerySegments Build(QueryContext ctx);
    }

    public struct QuerySegments
    {   
        public Dictionary<QuerySegment, List<string>> Segments;
        public List<DbParameter> Parameters;
        public Dictionary<string, object> Arguments;

        public string[] Get(QuerySegment segment)
        {
            return Segments.ContainsKey(segment) ? Segments[segment].ToArray() : null;
        }

        public bool Exist(QuerySegment segment)
        {
            return Segments.ContainsKey(segment) && Segments[segment].Any();
        }

        public string Get(QuerySegment segment, int index)
        {
            return Segments.ContainsKey(segment) ? null : Segments[segment][index];
        }

        public void AddToSegment(QuerySegment segment, params string[] values)
        {
            if (!Segments.ContainsKey(segment))
            {
                Segments.Add(segment, new List<string>());
            }
            Segments[segment].AddRange(values);
        }

        public string Print(QuerySegment segment, string joinWith = "\n\t", string prefixWith = "\n\t", bool required = false)
        {
            if (Exist(segment))
            {
                return prefixWith + string.Join(joinWith, Get(segment));
            }
            if (required)
            {
                throw new ArgumentException($"{segment.ToString().ToUpper()} segment missing!");
            }
            return null;
        }
    }

    public enum QuerySegment
    {
        Select,
        PreSelect,
        PostSelect,
        BaseTable,
        PreBaseTable,
        PostBaseTable,
        PreJoins,
        Joins,
        PostJoins,
        Filter,
        PreFilter,
        PostFilter,
        Ordering,
        PreOrdering,
        PostOrdering
    }
}
