﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Extensions;

namespace CoPilot.ORM.Helpers
{
    public static class PathHelper
    {
        public static string[] ExpandPaths(params string[] paths)
        {
            var allPaths = new HashSet<string>();

            if (paths != null)
            {
                foreach (var path in paths)
                {
                    var pathParts = path.Split('.');
                    var currentPath = "";
                    foreach (var pathPart in pathParts)
                    {
                        allPaths.Add(currentPath + pathPart);
                        currentPath = currentPath + pathPart + ".";
                    }

                }
            }

            return allPaths.ToArray();
        }

        public static string RemoveFirstElementFromPathString(string path)
        {
            if (path == null) return null;

            var idx = path.IndexOf(".", StringComparison.Ordinal);
            if (idx > 0)
            {
                return path.Substring(idx + 1);
            }
            return string.Empty;
        }

        public static string RemoveLastElementFromPathString(string path)
        {
            if (path == null) return null;

            var idx = path.LastIndexOf(".", StringComparison.Ordinal);
            if (idx > 0)
            {
                return path.Substring(0, idx);
            }
            return string.Empty;
        }

        public static string[] RemoveSimpleTypesFromPaths(Type baseType, params string[] paths)
        {
            var newPaths = new List<string>();
            foreach (var path in paths)
            {               
                var parts = path.Split('.');
                var currentType = baseType;
                var newPath = string.Empty;
                foreach (var part in parts)
                {
                    var member = currentType.GetTypeInfo().GetMember(part, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance).SingleOrDefault();
                    if (member == null) throw new CoPilotRuntimeException($"The path '{path}' is not valid for type '{baseType}'!");
                    var memberType = member.GetMemberType();
                    if (memberType.IsSimpleValueType())
                    {
                        break;
                    }
                    newPath += (string.IsNullOrEmpty(newPath) ? "" : ".") + part;    
                    if (memberType.IsCollection())
                    {
                        memberType = memberType.GetCollectionType();
                    }
                    currentType = memberType; 
                }
                if (!string.IsNullOrEmpty(newPath))
                {
                    newPaths.Add(newPath);
                }
            }
            return newPaths.ToArray();
        }

        
        public static Dictionary<string, Type> GetTypesFromPath(Type baseType, string path, bool excludeSimpleTypes = true)
        {
            var types = new Dictionary<string, Type>();
            var paths = path.Split('.');
            var currentType = baseType;
            foreach (var part in paths)
            {
                var member = currentType.GetTypeInfo().GetMember(part, MemberTypes.Property|MemberTypes.Field, BindingFlags.Public|BindingFlags.Instance).SingleOrDefault();
                if(member == null) throw new CoPilotRuntimeException($"The path '{path}' is not valid for type '{baseType}'!");
                var memberType = member.GetMemberType();
                if(!excludeSimpleTypes || !memberType.IsSimpleValueType())
                    types.Add(part, memberType);
                if (memberType.IsCollection())
                {
                    memberType = memberType.GetCollectionType();
                }
                currentType = memberType;
            }
            return types;
        }

        public static Tuple<string, string> SplitFirstInPathString(string path)
        {
            if (path == null) return null;

            var idx = path.IndexOf(".", StringComparison.Ordinal);
            if (idx > 0)
            {
                return new Tuple<string, string>(path.Substring(0, idx), path.Substring(idx+1));
            }
            return new Tuple<string, string>(path, string.Empty);
        }

        public static Tuple<string, string> SplitLastInPathString(string path)
        {
            if (path == null) return null;

            var idx = path.LastIndexOf(".", StringComparison.Ordinal);
            if (idx > 0)
            {
                return new Tuple<string, string>(path.Substring(0,idx), path.Substring(idx + 1));
            }
            return new Tuple<string, string>(string.Empty, path);
        }

        public static Tuple<string, string, string> SplitFirstAndLastInPathString(string path)
        {
            var splitFirst = SplitFirstInPathString(path);

            if (!string.IsNullOrEmpty(splitFirst.Item2))
            {
                var splitLast = SplitLastInPathString(splitFirst.Item2);
                return new Tuple<string, string, string>(splitFirst.Item1, splitLast.Item1, splitLast.Item2);
            }
            return new Tuple<string, string, string>(splitFirst.Item1, string.Empty, string.Empty);
        }

        public static string GetLastElementFromPathString(string path)
        {
            return SplitLastInPathString(path).Item2;
        }

        public static Tuple<object, MemberInfo> GetReferenceFromPath(object baseInstance, string path)
        {
            var paths = path.Split('.');
            var memberName = paths.Last();
            var currentInstance = baseInstance;
            var currentType = baseInstance.GetType();
            foreach (var part in paths)
            {
                var member = currentType.GetTypeInfo().GetMember(part, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance).SingleOrDefault();
                if (member == null) throw new CoPilotRuntimeException($"The path '{path}' is not valid for type '{baseInstance.GetType()}'!");
                if (part.Equals(memberName, StringComparison.Ordinal))
                {
                    return new Tuple<object, MemberInfo>(currentInstance, member);
                }
                var cm = ClassMemberInfo.Create(member);
                currentInstance = cm.GetValue(currentInstance);
                var memberType = member.GetMemberType();

                if (memberType.IsCollection())
                {
                    throw new CoPilotRuntimeException("Cannot resolve path if path includes a collection reference");
                }
                currentType = memberType;
            }
            throw new CoPilotRuntimeException($"Unable to get a reference from path '{path}' on type '{baseInstance.GetType().Name}'");
        }

        public static MemberInfo GetMemberFromPath(Type baseType, string path, bool ignoreCase = false, bool throwOnError = true)
        {
            var paths = path.Split('.');
            var memberName = paths.Last();
            var currentType = baseType;
            foreach (var part in paths)
            {
                var bindingFlags = (BindingFlags.Public | BindingFlags.Instance);
                if (ignoreCase)
                {
                    bindingFlags |= BindingFlags.IgnoreCase;
                }
                var member = currentType.GetTypeInfo().GetMember(part, MemberTypes.Property | MemberTypes.Field, bindingFlags).SingleOrDefault();
                if (member == null)
                {
                    if(throwOnError) throw new CoPilotRuntimeException($"The path '{path}' is not valid for type '{baseType}'!");
                    return null;
                }
                if (part.Equals(memberName, StringComparison.Ordinal))
                {
                    return member;
                }
                var memberType = member.GetMemberType();
                
                if (memberType.IsCollection())
                {
                    memberType = memberType.GetCollectionType();
                }
                currentType = memberType;
            }
            throw new CoPilotRuntimeException($"Member not found from path '{path}' on type '{baseType.Name}'");
        }


        public static string MaskPath(string path, string mask)
        {
            if (string.IsNullOrEmpty(mask)) return path;

            var index = path.IndexOf(mask, 0, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return path;
            }

            index += mask.Length;
            var masked = path.Substring(index);
            
            if (masked.Length > 1 && masked[0] == '.')
            {
                return masked.Substring(1);
            }
            return masked;
        }

        public static string Combine(string path1, string path2)
        {
            if(string.IsNullOrEmpty(path1)) return path2;
            if (string.IsNullOrEmpty(path2)) return path1;
            return path1+"."+path2;
        }
    }
}
