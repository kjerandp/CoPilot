using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using CoPilot.ORM.Config.Naming;

namespace CoPilot.ORM.Mapping.Mappers
{
    public static class DynamicMapper
    {
        public static ObjectMapper Create()
        {
            return Create(true, null);
        }

        public static ObjectMapper Create(params string[] fieldNameMask)
        {
            return Create(true, fieldNameMask);
        }
        
        public static ObjectMapper Create(bool convertToCamelCase = true, params string[] fieldNameMask)
        {
            return Create(convertToCamelCase?new CamelCaseConverter():null, fieldNameMask);
        }
        public static ObjectMapper Create(ILetterCaseConverter caseConverter = null, params string[] fieldNameMask)
        {
            return dataset =>
            {
                var result = new MappedRecord[dataset.Records.Length];
                if (dataset.FieldNames.Length == 1)
                {
                    Parallel.ForEach(dataset.Records, (r, n, i) =>
                    {
                        foreach (var value in r)
                        {
                            result[i] = new MappedRecord(value);
                        }
                    });
                } else {
                    Parallel.ForEach(dataset.Records, (r, n, i) =>
                    {
                        var model = new ExpandoObject() as IDictionary<string, object>;

                        for (var f = 0; f < r.Length; f++)
                        {
                            var propName = string.Empty;
                            var fieldNameParts = dataset.FieldNames[f].Split('.');
                            
                            var isMasked = false;
                            if (fieldNameMask != null)
                            {
                                foreach (var part in fieldNameParts)
                                {
                                    foreach (var mask in fieldNameMask)
                                    {
                                        if (!string.IsNullOrEmpty(propName))
                                        {
                                            propName = "." + propName;
                                        }
                                        if (part.StartsWith(mask, StringComparison.OrdinalIgnoreCase))
                                        {
                                            propName += part.ToUpperInvariant().Replace(mask.ToUpperInvariant(), "");
                                            isMasked = true;
                                            break;
                                        }
                                    }
                                    if (!isMasked)
                                    {
                                        propName += part;
                                    }
                                }
                            }
                            else
                            {
                                propName = dataset.FieldNames[f];
                            }

                            if (caseConverter != null)
                            {
                                propName = caseConverter.Convert(propName);
                            }

                            var value = r[f];
                            model.Add(propName, value);
                        }
                        result[i] = new MappedRecord(model);
                    });
                }
                
                //for (var r = 0; r < dataset.Records.Length; r++)
                //{
                    
                //}
                return result;
            };
        }


    }
}
