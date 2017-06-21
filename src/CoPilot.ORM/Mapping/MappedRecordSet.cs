using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoPilot.ORM.Helpers;
using System.Reflection;
using CoPilot.ORM.Exceptions;

namespace CoPilot.ORM.Mapping
{
    public class MappedRecordSet
    {
        internal MappedRecordSet(string name, MappedRecord[] records)
        {
            Name = name;
            Records = records;
        }

        public Dictionary<object, MappedRecord> IndexedByKey { get; private set; }
        public IEnumerable<object> KeyValues => IndexedByKey.Keys;
        public MappedRecord[] Records { get; }
        public string Name { get; set; }

        internal void SetKey(Func<MappedRecord,object> keyFunc)
        {
            var dict = new ConcurrentDictionary<object, MappedRecord>();
            Parallel.ForEach(Records, r =>
            {
                var key = keyFunc.Invoke(r);
                if (key != null)
                {
                    var tries = 0;
                    while (!dict.TryAdd(key, r))
                    {
                        tries++;
                        if(tries >= 2) throw new CoPilotRuntimeException("Unable to add to concurrent dictionary after 3 attempts!");
                    }
                }
            });
            IndexedByKey = dict.ToDictionary(k => k.Key, v => v.Value);
            
        }

        public void Merge(MappedRecordSet childSet, string targetName, Func<MappedRecord, object> childKeyFunc)
        {
            var children = childSet.Records
                .Select(r => new {key = childKeyFunc.Invoke(r), instance = r.Instance})
                .Where(r => r.key != null).ToArray();

            var targetMember = Records.First().Instance.GetType().GetTypeInfo().GetMember(targetName).Single();

            if (IndexedByKey == null || !IndexedByKey.Any())
            {
                throw new CoPilotUnsupportedException("Target set must have a key set!");
            }

            var parentType = IndexedByKey.First().Key.GetType();
            if (children.Any())
            {
                var childType = children.First().key.GetType();
                var doConvert = childType != parentType;
                Parallel.ForEach(children, (child) =>
                {
                    object key;
                    if (doConvert)
                    {
                        ReflectionHelper.ConvertValueToType(parentType, child.key, out key, false);
                    }
                    else
                    {
                        key = child.key;
                    }
                    if (IndexedByKey.ContainsKey(key))
                    {
                        var parent = IndexedByKey[key];
                        ReflectionHelper.AddValueToMemberCollection(targetMember, parent.Instance, child.instance);
                    }
                });
            }

            
        }
    }
}