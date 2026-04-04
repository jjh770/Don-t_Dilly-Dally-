using DG.Tweening;
using UnityEngine;

public abstract class PatientDeathBase : MonoBehaviour
{
    public abstract Sequence Play(
        Transform patientRoot,
        Transform bedTransform,
        Transform patientTransform);

    public abstract void ForceComplete(
        Transform patientRoot,
        Transform bedTransform,
        Transform patientTransform);

    protected void PlayFx(ParticleSystem fx)
    {
        if (fx != null)
        {
            fx.Play();
        }
    }

    protected void StopFx(ParticleSystem fx)
    {
        if (fx != null)
        {
            fx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    protected void ClearFx(ParticleSystem fx)
    {
        if (fx != null)
        {
            fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    protected void PlayAllFx(GameObject fxRoot)
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

    protected void ClearAllFx(GameObject fxRoot)
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
