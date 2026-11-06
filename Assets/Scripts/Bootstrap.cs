using UnityEngine;

// Starts the game automatically in ANY scene when you press Play (or run a build).
public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (IceGame.I != null) return;
        new GameObject("IceWorld3D").AddComponent<IceGame>();
    }
}
