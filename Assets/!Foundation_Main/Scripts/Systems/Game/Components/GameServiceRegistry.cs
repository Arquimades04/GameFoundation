using System;
using System.Collections.Generic;
using UnityEngine;

namespace Foundation.Systems.Game
{
    public interface IGameService
    {
        public void Shutdown();
    }

    public sealed class GameServiceRegistry
    {
        private readonly Dictionary<Type, object> services = new();

        public void Register<T>(T service) where T : class
        {
            if (service == null || services.ContainsKey(typeof(T)))
                throw new InvalidOperationException(
                    $"Service {typeof(T).Name} is null or already registered.");

            services.Add(typeof(T), service);
        }

        public T Resolve<T>() where T : class
        {
            if (!TryResolve<T>(out var service))
                throw new InvalidOperationException(
                    $"Required service {typeof(T).Name} was not registered.");

            return service;
        }

        public bool TryResolve<T>(out T service) where T : class
        {
            if (services.TryGetValue(typeof(T), out var value))
            {
                service = (T)value;
                return true;
            }

            service = null;
            return false;
        }

        public void Shutdown()
        {
            foreach (var service in services.Values)
            {
                if (service is IGameService gameService)
                {
                    gameService.Shutdown();
                }
            }
            services.Clear();
        }
    }
}