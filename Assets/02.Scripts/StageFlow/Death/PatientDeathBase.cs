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

    protected void PlayFx(ParticleSystem fx) => FxHelper.Play(fx);
    protected void StopFx(ParticleSystem fx) => FxHelper.Stop(fx);
    protected void ClearFx(ParticleSystem fx) => FxHelper.Clear(fx);
    protected void PlayAllFx(GameObject fxRoot) => FxHelper.PlayAll(fxRoot);
    protected void ClearAllFx(GameObject fxRoot) => FxHelper.ClearAll(fxRoot);
}
