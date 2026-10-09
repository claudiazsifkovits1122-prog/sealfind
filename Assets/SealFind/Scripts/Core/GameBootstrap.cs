using UnityEngine;

namespace SealFind
{
    /// <summary>
    /// Startet das Spiel automatisch, sobald irgendeine Szene geladen wird.
    /// Dadurch funktioniert "Play" sofort – auch in einer leeren Szene, ohne Prefabs oder Inspector-Setup.
    /// Zum Abschalten (z.B. für eigene Testszenen) <see cref="AutoStart"/> auf false setzen.
    /// </summary>
    public static class GameBootstrap
    {
        public static bool AutoStart = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!AutoStart || GameManager.Instance != null) return;
#if UNITY_2023_1_OR_NEWER
            if (Object.FindAnyObjectByType<GameManager>() != null) return;
#else
            if (Object.FindObjectOfType<GameManager>() != null) return;
#endif
            new GameObject("SealFind").AddComponent<GameManager>();
        }
    }
}
