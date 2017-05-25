using System.Collections.Generic;

namespace CoPilot.ORM.Mapping
{
    public struct MappedRecord
    {
        public MappedRecord(object instance, Dictionary<string, object> unmapped = null)
        {
            Instance = instance;
            UnmappedData = unmapped;
        }
        public object Instance { get; set; }

        public Dictionary<string, object> UnmappedData { get; set; }
    }
}