using System;
using System.Linq;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Filtering.Decoders.Interfaces;
using CoPilot.ORM.Helpers;

namespace CoPilot.ORM.Filtering.Decoders.DecodedNodeTypes
{
    public class DecodedReference : IDecodedNode
    {
        public DecodedReference(Type type, string path)
        {
            BaseType = type;
            Path = path;

            Validate();
        }

        private void Validate()
        {
            var types = PathHelper.GetTypesFromPath(BaseType, Path, false);
            var path = string.Empty;
            if (types != null && types.Any())
            {
                var current = types.First();
                path = current.Key;
                ReferencedType = current.Value;

                foreach (var item in types.Skip(1))
                {
                    if (current.Value.IsSimpleValueType())
                    {
                        ReferencedTypeMemberAccess = item.Key;
                        break;
                    }
                    path += "." + item.Key;
                    ReferencedType = item.Value;
                }
            }
            Path = path;
        }

        public Type BaseType { get; }
        public Type ReferencedType { get; private set; }
        public string Path { get; private set; }
        public string ReferencedTypeMemberAccess { get; private set; }
        public string ReferencedTypeMethodCall { get; internal set; }
        public object[] ReferenceTypeMethodCallArgs { get; set; }
        public bool IsInverted { get; set; }
    }
}