using System;
using System.Collections.Generic;
using UnityEngine;

public static class ServiceLocator
{
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public static void Register<T>(T service)
        {
            Type type = typeof(T);
            if (!_services.ContainsKey(type))
            {
                _services.Add(type, service);
            }
            else
            {
                Debug.LogWarning($"[ServiceLocator] {type.Name} 이미 등록된 서비스입니다.");
            }
        }

        public static T Get<T>()
        {
            Type type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }

            Debug.LogError($"[ServiceLocator] {type.Name} 서비스를 찾을 수 없습니다.");
            return default;
        }

        public static void Unregister<T>()
        {
            Type type = typeof(T);
            if (_services.ContainsKey(type))
            {
                _services.Remove(type);
            }
        }

        public static bool IsRegistered<T>()
        {
            return _services.ContainsKey(typeof(T));
        }

        public static void Clear()
        {
            _services.Clear();
        }
}
