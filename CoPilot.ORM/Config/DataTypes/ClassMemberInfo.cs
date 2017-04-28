using System;
using System.Reflection;
using CoPilot.ORM.Helpers;

namespace CoPilot.ORM.Config.DataTypes
{
    public class ClassMemberInfo
    {
        private ClassMemberInfo(MemberInfo memberInfo)
        {
            var type = memberInfo.MemberType == MemberTypes.Field ?
                ((FieldInfo)memberInfo).FieldType : ((PropertyInfo)memberInfo).PropertyType;

            Name = memberInfo.Name;
            DeclaringClassType = memberInfo.DeclaringType;
            MemberType = type;
            DataType = DbConversionHelper.MapToDbDataType(type);
            Type = memberInfo.MemberType;
        }
        
        public static ClassMemberInfo Create(MemberInfo memberInfo)
        {
            return new ClassMemberInfo(memberInfo);
        }

        public string Name { get; }
        public Type DeclaringClassType { get; }
        public Type MemberType { get; }
        public DbDataType DataType { get; set; }
        public MemberTypes Type { get; }
        public object GetDefaultValue()
        {
            return MemberType.IsValueType ? Activator.CreateInstance(MemberType) : null;
        }

        public object GetValue(object obj)
        {
            if (obj == null) throw new ArgumentException("Can't get value without an instance of the declaring object!");
            if (Type == MemberTypes.Field)
            {
                return DeclaringClassType.GetField(Name).GetValue(obj);
            }
            return DeclaringClassType.GetProperty(Name).GetValue(obj);
        }

        public void SetValue(object obj, object value)
        {
            object convertedValue;
            if (ReflectionHelper.ConvertValueToType(MemberType, value, out convertedValue, false))
            {
                if (Type == MemberTypes.Field)
                {
                    DeclaringClassType.GetField(Name).SetValue(obj, convertedValue);
                }
                else
                {
                    DeclaringClassType.GetProperty(Name).SetValue(obj, convertedValue);
                }
            }
        }

        public override string ToString()
        {
            return DeclaringClassType.Name + "." + Name;
        }

        public override int GetHashCode()
        {
            return DeclaringClassType.GetHashCode() ^ Name.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            var other = obj as ClassMemberInfo;

            return other != null && other.GetHashCode() == GetHashCode();
        }
    }
}
