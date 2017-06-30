using System;
using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Mapping;
using CoPilot.ORM.Mapping.Mappers;

namespace CoPilot.ORM.Database
{
    public struct DbResponse
    {
        public DbResponse(DbRecordSet[] results, long elapsedMs)
        {
            RecordSets = results;
            ElapsedMs = elapsedMs;
        }

        public long ElapsedMs { get; }
        public DbRecordSet[] RecordSets { get; }

        public string[] GetPaths()
        {
            if (RecordSets == null || !RecordSets.Any()) return null;
            var includes = new List<string>();

            foreach (var recordSet in RecordSets)
            {
                var paths =
                    recordSet.FieldNames.Select(f => PathHelper.SplitLastInPathString(f).Item1)
                        .Where(f => !string.IsNullOrEmpty(f)).Distinct().ToList();

                var pathFromSetName = PathHelper.SplitFirstInPathString(recordSet.Name ?? "Base").Item2;

                if (!string.IsNullOrEmpty(pathFromSetName))
                    paths.Add(pathFromSetName);

                if (paths.Any())
                    includes.AddRange(paths);
            }

            return includes.Distinct().ToArray();
        }

        public IEnumerable<T> Map<T>(ObjectMapper mapper = null)
        {
            return Map<T>(null, mapper);
        }

        public IEnumerable<T> Map<T>(string name, ObjectMapper mapper = null)
        {
            if (RecordSets == null || !RecordSets.Any()) return new T[0];

            if (mapper == null)
            {
                mapper = (typeof(T) == typeof(object) || typeof(T) == typeof(IDictionary<string, object>)) ?
                    DynamicMapper.Create() :
                    BasicMapper.Create(typeof(T));
            }
            var set = string.IsNullOrEmpty(name) ? RecordSets.First():RecordSets.Single(r => r.Name.Equals(name, StringComparison.Ordinal));
            return mapper.Invoke(set).Select(r => r.Instance).OfType<T>();
            
        }
    }
}