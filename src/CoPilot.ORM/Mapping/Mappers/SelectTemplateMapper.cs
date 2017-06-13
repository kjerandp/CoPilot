using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoPilot.ORM.Config;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Helpers;

namespace CoPilot.ORM.Mapping.Mappers
{

    public static class SelectTemplateMapper
    {
       
        public static ObjectMapper Create(TableContext ctx, Type type)
        {
                
            ObjectMapper mapper = dataset =>
            {
                var result = new MappedRecord[dataset.Records.Length];

                if (ctx.SelectTemplate == null) return result; //throw instead?

                var adapters = new Dictionary<string, ValueAdapter>();

                foreach (var selectItem in ctx.SelectTemplate)
                {
                    ITableContextNode node = ctx;

                    var reference = PathHelper.SplitLastInPathString(selectItem.Key);
                    if (!string.IsNullOrEmpty(reference.Item1))
                    {
                        node = ctx.FindByPath(reference.Item1);
                    }
                    var member = node.MapEntry.GetMemberByName(reference.Item2);                    

                    var adapter = node.MapEntry.GetAdapter(member);

                    if (adapter != null)
                    {
                        adapters.Add(selectItem.Value, adapter);
                    }                   
                }

                Parallel.ForEach(dataset.Records, (r, n, i) =>
                {
                    for (var f = 0; f < r.Length; f++)
                    {
                        var key = dataset.FieldNames[f];
                        var value = r[f];
                        if (adapters.ContainsKey(key))
                        {
                            r[f] = adapters[key].Invoke(MappingTarget.Object, value);  
                        }
                    }
                    if (type.Namespace == null) //anonymous type
                    {
                        result[i] = new MappedRecord(ReflectionHelper.CreateInstance(type, r));
                    }
                    else
                    {
                        var dtoToFill = ReflectionHelper.CreateInstance(type);
                        if (dtoToFill.GetType().IsSimpleValueType())
                        {
                            ReflectionHelper.ConvertValueToType(dtoToFill.GetType(), r[0], out dtoToFill);
                            result[i] = new MappedRecord(dtoToFill);

                        }
                        else
                        {
                            for (var f = 0; f < r.Length; f++)
                            {
                                var key = dataset.FieldNames[f];
                               
                                var member = PathHelper.GetMemberFromPath(type, key);
                                if (member != null)
                                {
                                    var classMember = ClassMemberInfo.Create(member);
                                    classMember.SetValue(dtoToFill, r[f]);
                                }
                                result[i] = new MappedRecord(dtoToFill);
                            }
                        }
                    }

                });
                
                return result;
            };

            return mapper;
        }
    }
}
