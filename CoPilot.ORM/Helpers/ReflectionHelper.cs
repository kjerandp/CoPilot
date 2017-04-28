using System;
using System.Collections;
using System.Linq;
using System.Reflection;
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
            var members = classType.GetMember(name, MemberTypes.Property | MemberTypes.Field, bFlags);
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
                throw new ArgumentException($"Input value cannot be null for type {targetType.Name}!");
            }
            try
            {
                if (targetType.IsSimpleValueType() != input.GetType().IsSimpleValueType())
                {
                    if (throwOnError)
                    {
                        throw new ArgumentException(
                            $"Cannot convert '{input.GetType().Name}' to target type '{targetType.Name}'");
                    }
                    output = null;
                    return false;
                }

                if (targetType.IsNullable(true) && targetType.GetGenericArguments()[0] == input.GetType())
                {
                    output = input;
                }
                else
                {
                    output = targetType == input.GetType() ? input : Convert.ChangeType(input, targetType);
                }
                return true;
            }
            catch (Exception)
            {
                if (throwOnError) throw;
                
                output = null;
                return false;
            }
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
                        prop.SetValue(entity, convertedValue);
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
            catch (ArgumentException)
            {
                if (throwOnError) throw;
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
                        collection = prop.GetValue(entity) as IList;
                        if (collection == null)
                        {
                            collection = Activator.CreateInstance(member.GetMemberType()) as IList;
                            prop.SetValue(entity, collection);
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
                catch (ArgumentException)
                {
                    if (throwOnError) throw;
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
            var constructor = parameter.GetConstructor(Type.EmptyTypes);
            if (constructor != null)
            {
                return constructor.Invoke(new object[] {});
            }

            return GetDefaultValue(parameter);
        }
        public static object GetDefaultValue(Type parameter)
        {
            var defaultGeneratorType =
              typeof(DefaultGenerator<>).MakeGenericType(parameter);

            return defaultGeneratorType.InvokeMember(
              "GetDefault",
              BindingFlags.Static |
              BindingFlags.Public |
              BindingFlags.InvokeMethod,
              null, null, new object[0]);
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
