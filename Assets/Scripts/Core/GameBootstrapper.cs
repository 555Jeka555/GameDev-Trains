using RailSim.Gameplay;
using UnityEngine;

namespace RailSim.Core
{
    public static class GameBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (Object.FindFirstObjectByType<RailGameController>() != null)
            {
                return;
            }

            var root = new GameObject("RailGame");
            root.AddComponent<RailGameController>();
        }
    }
}

