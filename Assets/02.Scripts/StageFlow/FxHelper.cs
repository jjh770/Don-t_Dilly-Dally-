using UnityEngine;

public static class FxHelper
{
    public static void Play(ParticleSystem fx)
    {
        if (fx != null)
        {
            fx.Play();
        }
    }

    public static void Stop(ParticleSystem fx)
    {
        if (fx != null)
        {
            fx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    public static void Clear(ParticleSystem fx)
    {
        if (fx != null)
        {
            fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public static void PlayAll(GameObject fxRoot)
    {
        if (fxRoot == null)
        {
            return;
        }

        foreach (ParticleSystem fx in fxRoot.GetComponentsInChildren<ParticleSystem>())
        {
            fx.Play();
        }
    }

    public static void ClearAll(GameObject fxRoot)
    {
        if (fxRoot == null)
        {
            return;
        }

        foreach (ParticleSystem fx in fxRoot.GetComponentsInChildren<ParticleSystem>())
        {
            fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
