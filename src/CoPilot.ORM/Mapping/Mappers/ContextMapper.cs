using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Helpers;

namespace CoPilot.ORM.Mapping.Mappers
{

    /// <summary>
    /// Maps to POCO object mapped with the DbMapper 
    /// </summary>
    public class ContextMapper
    {
        public static ObjectMapper Create(SelectTemplate template)
        {
            ObjectMapper mapper = dataset =>
            {
                if (dataset.Name == null)
                {
                    dataset.Name = template.GetSetNames()[0];
                }
                var records = new MappedRecord[dataset.Records.Length];
                var baseNode = template.GetBaseNode(dataset.Name);
                var columns = template.GetDictionaryFromSet(dataset.Name);
                Parallel.ForEach(dataset.Records, (r, n, i) =>
                {
                    records[i] = MapSingleInstance(baseNode, dataset.FieldNames, r, columns);
                });
                return records;
            };
            return mapper;
        }
        
        private static MappedRecord MapSingleInstance(ITableContextNode baseNode, string[] fields, object[] values, IReadOnlyDictionary<string, ContextColumn> mapping)
        {
            var objects = new Dictionary<string, object>();

            var rec = new MappedRecord()
            {
                UnmappedData = new Dictionary<string, object>(),
                Instance = null
            };

            if (values == null || values.All(r => r is DBNull))
            {
                return rec;
            }

            var basePath = PathHelper.RemoveFirstElementFromPathString(baseNode.Path);

            rec.Instance = ReflectionHelper.CreateInstance(baseNode.MapEntry.EntityType);

            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i].ToLower();
                var value = values[i];

                if (value is DBNull) continue;

                if (!mapping.ContainsKey(field))
                    continue; //throw new CoPilotRuntimeException("Field not found in template mapping!");

                var col = mapping[field];

                if (col.Adapter != null)
                {
                    value = col.Adapter(MappingTarget.Object, value);
                }

                ClassMemberInfo member;
                string objectPath;
                Type entityType;

                if (col.Node is TableContextNode relNode && relNode.Relationship.IsLookupRelationship)
                {
                    member = relNode.Origin.MapEntry.GetMappedMember(relNode.Relationship.ForeignKeyColumn);
                    objectPath = PathHelper.RemoveFirstElementFromPathString(relNode.Origin.Path);
                    entityType = relNode.Origin.MapEntry.EntityType;
                }
                else
                {
                    member = col.MappedMember;
                    objectPath = PathHelper.RemoveFirstElementFromPathString(col.Node.Path);
                    entityType = col.Node.MapEntry.EntityType;
                }

                object instance;

                if (objectPath.Equals(basePath, StringComparison.Ordinal))
                {
                    instance = rec.Instance;

                }
                else
                {
                    if (objects.ContainsKey(objectPath))
                    {
                        instance = objects[objectPath];
                    }
                    else
                    {
                        instance = ReflectionHelper.CreateInstance(entityType);
                        objects.Add(objectPath, instance);
                    }
                }

                if (member != null)
                {
                    member.SetValue(instance, value);
                }
                else
                {
                    rec.UnmappedData.Add(field, value);
                }

            }

            foreach (var obj in objects)
            {
                var splitPath = PathHelper.SplitLastInPathString(obj.Key);

                object parent = null;

                if (splitPath.Item1.Equals(basePath, StringComparison.Ordinal))
                {
                    parent = rec.Instance;
                }
                else if (objects.ContainsKey(splitPath.Item1))
                {
                    parent = objects[splitPath.Item1];
                }

                if (parent == null)
                {
                    parent = CreateParent(splitPath.Item1, objects, rec.Instance);
                }
                var member = parent.GetType().GetClassMember(splitPath.Item2);
                if (member == null) throw new CoPilotRuntimeException("Unable to find member in parent object for mapped instance");

                member.SetValue(parent, obj.Value);
            }

            return rec;
        }

        private static object CreateParent(string path, IReadOnlyDictionary<string, object> objects, object root)
        {
            var splitPath = PathHelper.SplitLastInPathString(path);
            object itsParent;
            if (string.IsNullOrEmpty(splitPath.Item1))
            {
                itsParent = root;
            }
            else if (objects.ContainsKey(splitPath.Item1))
            {
                itsParent = objects[splitPath.Item1];
            }
            else
            {
                itsParent = CreateParent(splitPath.Item1, objects, root);
            }

            var member = itsParent.GetType().GetClassMember(splitPath.Item2);

            if (member == null)
                throw new CoPilotRuntimeException("Unable to link mapped instance to a parent object");

            var parent = ReflectionHelper.CreateInstance(member.MemberType);
            member.SetValue(itsParent, parent);
            return parent;
        }

        public static IEnumerable<T> MapAndMerge<T>(SelectTemplate template, DbRecordSet[] recordSets)
        {
            if (recordSets == null || recordSets.Length <= 0) yield break;
            if (recordSets.Any(r => r.Name == null))
            {
                if (recordSets.Length == 1)
                {
                    recordSets[0].Name = "Base";
                }
                else
                {
                    throw new CoPilotUnsupportedException(
                        @"The context mapper requires the record sets to be named. 
                        If you called a stored procedure, and it contains more than a single record set, 
                        you'll have to provide a proper name for each set.
                        Record sets should be named with the path made up from the propery names, that leads back to the base class, where the data will be merged into.");
                }
            }

            for (var i = 0; i < recordSets.Length; i++)
            {
                var n = PathHelper.SplitFirstInPathString(recordSets[i].Name);
                if (!n.Item1.Equals("Base", StringComparison.Ordinal))
                {
                    recordSets[i].Name = "Base" + (string.IsNullOrEmpty(n.Item2) ? "" : "." + n.Item2);
                }
            }

            var data = new Dictionary<string, MappedRecord[]>();
            var mapper = Create(template);
               
            foreach (var set in recordSets)
            {
                var mapped = mapper.Invoke(set);
                data.Add(set.Name, mapped);
            }

            template.Merge(data);

            
            foreach (var mappedRecord in data["Base"])
            {
                if (template.ShapeFunc != null)
                {
                    yield return (T) template.ShapeFunc.DynamicInvoke(mappedRecord.Instance);
                }
                else
                {
                    yield return (T) mappedRecord.Instance;
                }
            }       
        }
    }
    
}
