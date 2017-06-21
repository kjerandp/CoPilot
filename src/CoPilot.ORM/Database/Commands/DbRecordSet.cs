using System;
using System.Collections.Generic;
using System.Linq;

namespace CoPilot.ORM.Database.Commands
{
    public struct DbRecordSet
    {
        public DbRecordSet(Dictionary<string, object> data)
        {
            Name = "Recordset";
            FieldNames = data.Keys.ToArray();
            FieldTypes = data.Values.Select(r => r?.GetType()).ToArray();
            Records = new[] {data.Values.ToArray()};
        }

        public string Name { get; set; }
        public string[] FieldNames { get; set; }
        public Type[] FieldTypes { get; set; }
        public object[][] Records { get; set; }

        public int GetIndex(string fieldName)
        {
            return Array.IndexOf(FieldNames, fieldName);
        }
        
        public Dictionary<string, object[]> ToDictionary()
        {
            var ds = this;
            var dict = ds.FieldNames.ToDictionary(k => k, v => new object[ds.Records.Length]);

            for (var r = 0; r < Records.Length; r++)
            {
                for (var f = 0; f < FieldNames.Length; f++)
                {
                    dict[FieldNames[f]][r] = Records[r][f];
                }
            }
            return dict;
        }

        public Dictionary<string, object> ToDictionary(int index)
        {
            var rec = new Dictionary<string, object>();
            for (var i = 0; i < FieldNames.Length; i++)
            {
                rec.Add(FieldNames[i], Records[index][i]);
            }
            return rec;
        }
        public IEnumerable<Dictionary<string, object>> AsEnumerable()
        {
            var ds = this;
            return Records.Select((r, i) => ds.ToDictionary(i));

        }

        public override string ToString()
        {
            return Name ?? "(Unnamed record set)";
        }

        public object[] Vector(int fieldIndex)
        {
            return Records.Select(t => t[fieldIndex]).ToArray();
        }

        
    }

    
}