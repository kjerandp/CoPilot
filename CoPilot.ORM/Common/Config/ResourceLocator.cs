using System;
using System.Collections.Generic;

namespace CoPilot.ORM.Common.Config
{
    public class ResourceLocator
    {
        private readonly Dictionary<Type, Type> _typeMapping = new Dictionary<Type, Type>();
        private readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
        internal readonly object LockObject = new object();

        internal ResourceLocator(){}

        public void Register<TInterface, TImpl>() 
            where TInterface : class
            where TImpl : class, TInterface
        {
            if (_typeMapping.ContainsKey(typeof(TInterface)))
            {
                _typeMapping[typeof(TInterface)] = typeof(TImpl);
            }
            else
            {
                _typeMapping.Add(typeof(TInterface), typeof(TImpl));
            }
        }

        public void Register<T>(T impl) where T : class
        {
            Register<T, T>(impl);
        }

        public void Register<TInterface, TImpl>(TImpl impl)
            where TInterface : class
            where TImpl : class, TInterface
        {
            lock (LockObject)
            {
                if (_instances.ContainsKey(typeof(TInterface)))
                {
                    _instances[typeof(TInterface)] = impl;
                }
                else
                {
                    _instances.Add(typeof(TInterface), impl);
                }
            }

            if (!_typeMapping.ContainsKey(typeof(TInterface)))
            {
                _typeMapping.Add(typeof(TInterface), impl.GetType());
            }
        }

        public TInterface Get<TInterface>() where TInterface : class
        {
            lock (LockObject)
            {
                if (_instances.ContainsKey(typeof(TInterface)))
                {
                    return _instances[typeof(TInterface)] as TInterface;
                }
            }

            lock (LockObject)
            {
                if (!_typeMapping.ContainsKey(typeof(TInterface))) throw new ArgumentException($"'{typeof(TInterface).Name}' is not a registered type!");

                if (!_instances.ContainsKey(typeof(TInterface)))
                {
                    var type = _typeMapping[typeof(TInterface)];
                    var instance = (TInterface)Activator.CreateInstance(type);

                    _instances.Add(typeof(TInterface), instance);
                }
                return _instances[typeof(TInterface)] as TInterface;
            }

        }
    }
}