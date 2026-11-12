using UnityEngine;
using System;
using System.Collections.Generic;

namespace Foundation.Systems.Game
{
    public sealed class GameServicesHost : MonoBehaviour
    {
        #region PUBLIC PROPERTIES
        public GameServiceRegistry Services => services;
        #endregion

        #region PRIVATE PROPERTIES
        private GameServiceRegistry services;
        private readonly List<IGameService> startupOrder = new();
        private bool shuttingDown;
        #endregion

        #region UNITY METHODS
        private void Awake()
        {
            services = new GameServiceRegistry();

            try
            {
                var debug = gameObject.AddComponent<DebugController>();
                StartService(debug, debug.Initialize);

                var pause = gameObject.AddComponent<PauseController>();
                StartService(pause, () => pause.Initialize(debug));

                var game = gameObject.AddComponent<GameController>();
                StartService(game, () => game.Initialize(pause, debug));
                services.Register<IBehaviourScheduler>(game);

                var performance = gameObject.AddComponent<PerformanceTracker>();
                StartService(performance, () => performance.Initialize(debug));

                var commands = new DebugCommandRegistry();
                StartService(commands, () => commands.Initialize(debug));

                var console = gameObject.AddComponent<DebugConsole>();
                StartService(console, () => console.Initialize(debug, commands, pause));

                debug.Log(DebugController.DebugCategory.General, DebugController.DebugPersistence.RuntimeOnly, "All services successfully initialized", "GameServicesHost");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                ShutdownServices();
                Destroy(gameObject);
            }
        }
        #endregion

        #region PRIVATE METHODS

        private void StartService<T>(T service, Action initialize)
            where T : class, IGameService
        {
            initialize();

            startupOrder.Add(service);
            services.Register(service);
        }

        private void OnApplicationQuit() => ShutdownServices();

        private void OnDestroy() => ShutdownServices();

        private void ShutdownServices()
        {
            if (shuttingDown)
                return;

            shuttingDown = true;

            for (int i = startupOrder.Count - 1; i >= 0; i--)
            {
                try
                {
                    startupOrder[i].Shutdown();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            startupOrder.Clear();
            services = null;
        }
        #endregion

    }
}
