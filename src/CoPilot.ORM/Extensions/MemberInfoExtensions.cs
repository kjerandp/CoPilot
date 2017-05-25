using System;
using System.Reflection;

namespace CoPilot.ORM.Extensions
{
    public static class MemberInfoExtensions
    {
        public static Type GetMemberType(this MemberInfo memberInfo)
        {
            if (memberInfo.MemberType == MemberTypes.TypeInfo)
            {
                var ti = memberInfo as TypeInfo;
                return ti?.AsType();

            }
            if (memberInfo.MemberType == MemberTypes.Property)
            {
                var prop = (PropertyInfo)memberInfo;
                return prop.PropertyType;

            }

            var field = (FieldInfo)memberInfo;
            return field.FieldType;

        }

        
    }
}
