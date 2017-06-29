using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Extensions;

namespace CoPilot.ORM.Helpers
{
    public static class ReflectionHelper
    {
        private static readonly object LockObj = new object();
        public static MemberInfo GetMemberType(Type classType, string name, bool ignoreCase = false)
        {
            var bFlags = BindingFlags.Instance | BindingFlags.Public;
            if (ignoreCase)
            {
                bFlags |= BindingFlags.IgnoreCase;
            }
            var members = classType.GetTypeInfo().GetMember(name, MemberTypes.Property | MemberTypes.Field, bFlags);
            if (members.Length == 1)
            {
                var memberInfo = members.Single();
                return memberInfo;
            }
            return null;
        }

        public static bool ConvertValueToType(Type targetType, object input, out object output, bool throwOnError = true)
        {
            if (input == null)
            {
                if (targetType.IsNullable())
                {
                    output = null;
                    return true;
                }
                throw new CoPilotUnsupportedException($"Input value cannot be null for type {targetType.Name}!");
            }
            try
            {
                if (targetType.IsSimpleValueType() != input.GetType().IsSimpleValueType())
                {
                    if (throwOnError)
                    {
                        throw new CoPilotUnsupportedException(
                            $"Cannot convert '{input.GetType().Name}' to target type '{targetType.Name}'");
                    }
                    output = null;
                    return false;
                }

                if (targetType.IsNullable(true) && targetType.GetTypeInfo().GetGenericArguments()[0] == input.GetType())
                {
                    output = input;
                }
                else
                {
                    if (targetType == input.GetType())
                    {
                        output = input;
                    }
                    else
                    {
                        if (targetType.GetTypeInfo().IsEnum && IsIntegerType(input))
                        {
                            output = Enum.ToObject(targetType, input);
                        }
                        else
                        {
                            output = Convert.ChangeType(input, targetType);
                        }
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                if (throwOnError) throw new CoPilotRuntimeException($"Unable to convert value to type {targetType.Name}", ex);
                
                output = null;
                return false;
            }
        }

        internal static bool IsIntegerType(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return true;
                default:
                    return false;
            }
        }

        internal static bool IsNumericType(object o)
        {
            if (IsIntegerType(o)) return true;

            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        internal static object CreateInstance(Type type, object[] values)
        {
            return Activator.CreateInstance(type, values);
        }

        public static void SetValueOnMember(MemberInfo member, object entity, object value, bool throwOnError = true)
        {

            try
            {
                object convertedValue;
                if (member.MemberType == MemberTypes.Property)
                {
                    var prop = (PropertyInfo) member;
                    
                    if (ConvertValueToType(prop.PropertyType, value, out convertedValue, throwOnError))
                    {
                        prop.SetValue(entity, convertedValue, null);
                    }
                }
                else
                {
                    var field = (FieldInfo) member;
                    if (ConvertValueToType(field.FieldType, value, out convertedValue, throwOnError))
                    {
                        field.SetValue(entity, convertedValue);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                if (throwOnError) throw new CoPilotRuntimeException($"Unable to set value on member {member.Name}", ex);
            }
        }

        public static void AddValueToMemberCollection(MemberInfo member, object entity, object item, bool throwOnError = true)
        {
            lock (LockObj)
            {
                try
                {
                    IList collection;
                    if (member.MemberType == MemberTypes.Property)
                    {
                        var prop = (PropertyInfo) member;
                        collection = prop.GetValue(entity, null) as IList;
                        if (collection == null)
                        {
                            collection = Activator.CreateInstance(member.GetMemberType()) as IList;
                            prop.SetValue(entity, collection, null);
                        }
                    }
                    else
                    {
                        var field = (FieldInfo) member;
                        collection = field.GetValue(entity) as IList;
                        if (collection == null)
                        {
                            collection = Activator.CreateInstance(member.GetMemberType()) as IList;
                            field.SetValue(entity, collection);
                        }
                    }
                    collection?.Add(item);
                }
                catch (ArgumentException ex)
                {
                    if (throwOnError) throw new CoPilotRuntimeException($"Unable to add value to collection {member.Name}", ex);
                }
            }
        }

        public static void AddValueToMemberCollection(ClassMemberInfo member, object entity, object item, bool throwOnError = true)
        {
            lock (LockObj)
            {
                try
                {
                    var collection = member.GetValue(entity) as IList;
                    if (collection == null)
                    {
                        collection = Activator.CreateInstance(member.MemberType) as IList;
                        member.SetValue(entity, collection);
                    }
                    
                    collection?.Add(item);
                }
                catch (ArgumentException ex)
                {
                    if (throwOnError) throw new CoPilotRuntimeException($"Unable to add value to collection {member.Name}", ex);
                }
            }
        }

        //public static object InvokeGenericMethod(object source, Type genericType, string methodName, params object[] args)
        //{
        //    var method = source.GetType().GetMethod(methodName);
        //    var generic = method.MakeGenericMethod(genericType);
        //    return generic.Invoke(source, args.Any()?args:null);
        //}

        //public static T InvokeGenericMethod<T>(object source, Type genericType, string methodName, params object[] args) where T : class
        //{
        //    return InvokeGenericMethod(source, genericType, methodName, args) as T;
        //}

        //public static object InvokeMethod(object source, string methodName, params object[] args)
        //{
        //    var method = source.GetType().GetMethod(methodName);
        //    return method.Invoke(source, args.Any() ? args : null);
        //}

        //public static T InvokeMethod<T>(object source, string methodName, params object[] args) where T : class
        //{
        //    return InvokeMethod(source, methodName, args) as T;
        //}
        public static object CreateInstance(Type parameter)
        {
            if (parameter == typeof(string))
            {
                return string.Empty;
            }
            
            var constructor = parameter.GetTypeInfo().GetConstructor(Type.EmptyTypes);
            if (constructor != null)
            {
                return constructor.Invoke(new object[] {});
            }
            if (parameter.IsNullable(true))
            {
                return GetDefaultValue(parameter.GenericTypeArguments.Single());
            }
            return GetDefaultValue(parameter);
        }
        public static object GetDefaultValue(Type parameter)
        {
            var defaultGeneratorType =
              typeof(DefaultGenerator<>).MakeGenericType(parameter);

            return defaultGeneratorType.GetTypeInfo().GetDeclaredMethod("GetDefault").Invoke(null, new object[0]);
        }
    }

    public class DefaultGenerator<T>
    {
        public static T GetDefault()
        {
            return default(T);
        }
    }
}
