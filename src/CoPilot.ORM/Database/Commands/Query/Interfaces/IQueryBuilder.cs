using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Exceptions;

namespace CoPilot.ORM.Database.Commands.Query.Interfaces
{
    public interface IQueryBuilder
    {
        QuerySegments Build(QueryContext ctx);
    }

    public class QuerySegments
    {   
        private readonly Dictionary<QuerySegment, List<string>> _segments = new Dictionary<QuerySegment, List<string>>(6);

        
        public string[] Get(QuerySegment segment)
        {
            return _segments.ContainsKey(segment) ? _segments[segment].ToArray() : null;
        }

        public bool Exist(QuerySegment segment)
        {
            return _segments.ContainsKey(segment) && _segments[segment].Any();
        }

        public string Get(QuerySegment segment, int index)
        {
            return _segments.ContainsKey(segment) ? null : _segments[segment][index];
        }

        public void AddToSegment(QuerySegment segment, params string[] values)
        {
            if (values == null) return;
            if (!_segments.ContainsKey(segment))
            {
                _segments.Add(segment, new List<string>());
            }
            _segments[segment].AddRange(values);
        }

        public string Print(QuerySegment segment, string joinWith = "\n\t", string prefixWith = "\n\t", bool required = false)
        {
            if (Exist(segment))
            {
                return prefixWith + string.Join(joinWith, Get(segment));
            }
            if (required)
            {
                throw new CoPilotRuntimeException($"{segment.ToString().ToUpper()} segment missing!");
            }
            return null;
        }

        public void Remove(QuerySegment segement)
        {
            if (_segments.ContainsKey(segement))
            {
                _segments.Remove(segement);
            }
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
