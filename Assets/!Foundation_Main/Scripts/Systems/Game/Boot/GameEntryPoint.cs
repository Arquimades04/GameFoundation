using UnityEngine;


namespace Foundation.Systems.Game
{
    public static class GameEntryPoint
    {
        public static GameServicesHost host;

        public static bool TryResolve<T>(out T service) where T : class
        {
            if (host == null)
            {
                service = null;
                return false;
            }

            return host.Services.TryResolve(out service);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            host = null;
        }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (host != null) return;

            host = Object.FindFirstObjectByType<GameServicesHost>();

            if (host != null) return;

            var gameServicesHost = new GameObject("GameServicesHost");
            Object.DontDestroyOnLoad(gameServicesHost);
            host = gameServicesHost.AddComponent<GameServicesHost>();

        }
    }
}
