using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using CoPilot.ORM.Config.Naming;

namespace CoPilot.ORM.Mapping.Mappers
{
    /// <summary>
    /// Maps to dynamic object and convert column names to camel case (default)
    /// </summary>
    public static class DynamicMapper
    {
        /// <summary>
        /// Create a mapping delegate using the DynamicMapper with default settings
        /// </summary>
        /// <returns>Mapping delegate</returns>
        public static ObjectMapper Create()
        {
            return Create(true, null);
        }

        /// <summary>
        /// Create a mapping delegate using the DynamicMapper with default settings
        /// </summary>
        /// <param name="fieldNameMask">Provide a set of prefixes that should be removed from column names before converting to a property name</param>
        /// <returns>Mapping delegate</returns>
        public static ObjectMapper Create(params string[] fieldNameMask)
        {
            return Create(true, fieldNameMask);
        }

        /// <summary>
        /// Create a mapping delegate using the DynamicMapper with default settings
        /// </summary>
        /// <param name="convertToCamelCase">Toggle the default behaviour of converting column names into camel case for property names</param>
        /// <param name="fieldNameMask">Provide a set of prefixes that should be removed from column names before converting to a property name</param>
        /// <returns>Mapping delegate</returns>
        public static ObjectMapper Create(bool convertToCamelCase = true, params string[] fieldNameMask)
        {
            return Create(convertToCamelCase?new CamelCaseConverter():null, fieldNameMask);
        }

        /// <summary>
        /// Create a mapping delegate using the DynamicMapper with default settings
        /// </summary>
        /// <param name="caseConverter">Choose the case converter that fits your need <see cref="ILetterCaseConverter"/></param>
        /// <param name="fieldNameMask">Provide a set of prefixes that should be removed from column names before converting to a property name</param>
        /// <returns>Mapping delegate</returns>
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
