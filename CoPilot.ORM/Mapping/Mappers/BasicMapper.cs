using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Helpers;

namespace CoPilot.ORM.Mapping.Mappers
{
    public static class BasicMapper
    {
      
        public static ObjectMapper Create(Type type, Dictionary<string, string> columnMapping = null, bool ignoreCase = true)
        {
            columnMapping = columnMapping?.ToDictionary(k => k.Key.ToUpperInvariant(), v => v.Value);
            ObjectMapper mapper = dataset =>
            {
                var result = new MappedRecord[dataset.Records.Length];
                Parallel.ForEach(dataset.Records, (r, n, i) =>
                {
                    var dtoToFill = ReflectionHelper.CreateInstance(type);

                    if (dtoToFill.GetType().IsSimpleValueType())
                    {
                        ReflectionHelper.ConvertValueToType(dtoToFill.GetType(), r[0], out dtoToFill);
                        result[i] = new MappedRecord(dtoToFill);

                    }
                    else
                    {
                        var unmappedValues = new Dictionary<string, object>();

                        for (var f = 0; f < r.Length; f++)
                        {
                            var key = dataset.FieldNames[f];
                            if (columnMapping != null && columnMapping.ContainsKey(key.ToUpperInvariant()))
                            {
                                key = columnMapping[key.ToUpperInvariant()];
                            }
                            var member = PathHelper.GetMemberFromPath(type, key, true, false);
                            if (member != null)
                            {
                                var classMember = ClassMemberInfo.Create(member);
                                classMember.SetValue(dtoToFill, r[f]);
                            }
                            else
                            {
                                unmappedValues.Add(key, r[f]);
                            }
                        }
                        result[i] = new MappedRecord(dtoToFill, unmappedValues);
                    }
                    
                });
                //for (var r = 0; r < dataset.Records.Length; r++)
                //{
                    
                //}

                return result;
            };

            return mapper;
        }
        

       
    }
}
