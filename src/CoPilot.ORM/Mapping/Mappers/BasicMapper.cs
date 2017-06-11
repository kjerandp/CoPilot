using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Config.Naming;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Helpers;

namespace CoPilot.ORM.Mapping.Mappers
{
    /// <summary>
    /// Maps by converting case and to a best effort to match column names with property names. Can be assisted
    /// by providing a dictionary that contains specific column-to-property mappings
    /// </summary>
    public static class BasicMapper
    {
        /// <summary>
        /// Create a mapping delegate using the BasicMapper
        /// </summary>
        /// <param name="type">The type of the object to map the values to</param>
        /// <param name="columnMapping">Dictionary to define specific column-to-property mappings</param>
        /// <param name="ignoreCase">Set if matching should be done case sensitive or not</param>
        /// <param name="caseConverter">Set specific case converter <see cref="ILetterCaseConverter"/></param>
        /// <returns>Mapping delegate</returns>
        public static ObjectMapper Create(Type type, Dictionary<string, string> columnMapping = null, bool ignoreCase = true, ILetterCaseConverter caseConverter = null)
        {
            columnMapping = columnMapping?.ToDictionary(k => k.Key.ToUpperInvariant(), v => v.Value);
            ObjectMapper mapper = dataset =>
            {
                var result = new MappedRecord[dataset.Records.Length];
                if (type.Namespace == null) //anonymous type
                {
                    return dataset.Records.Select(r => new MappedRecord(ReflectionHelper.CreateInstance(type,r))).ToArray();
                }
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
                            } else if (caseConverter != null)
                            {
                                key = caseConverter.Convert(key);
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
