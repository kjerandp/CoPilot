using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoPilot.ORM.Config.DataTypes;

namespace CoPilot.ORM.Extensions
{
    public static class TypeExtensions
    {
        public static bool IsSimpleValueType(this Type type)
        {
            return (type.IsPrimitive || type == typeof(string) || type.IsValueType);
        }

        public static bool IsCollection(this Type type)
        {
            return typeof(ICollection).IsAssignableFrom(type);
        }

        public static bool IsReference(this Type type)
        {
            return !IsSimpleValueType(type) && !IsCollection(type);
        }

        public static bool IsNullable(this Type type, bool onlyGenericNullable = false)
        {
            if (!onlyGenericNullable && !type.IsValueType)
            {
                return true;
            }
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static Type GetCollectionType(this Type type)
        {
            if (type.IsGenericType)
            {
                return type.GetGenericArguments().FirstOrDefault();
            }
            if (type.IsArray)
            {
                return type.GetElementType();
            }
            throw new ArgumentException($"Type '{type.Name}' not recognized as a collection");
        }

        public static ClassMemberInfo[] GetClassMembers(this Type classType)
        {
            var members = classType.GetMembers(BindingFlags.Instance | BindingFlags.Public)
                .Where(r => r.MemberType == MemberTypes.Property || r.MemberType == MemberTypes.Field);

            var list = new List<ClassMemberInfo>();
            foreach (var memberInfo in members)
            {
                var prop = ClassMemberInfo.Create(memberInfo);
                if (prop != null)
                    list.Add(prop);

            }
            return list.ToArray();
        }
    }
}
