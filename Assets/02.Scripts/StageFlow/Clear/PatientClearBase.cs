using DG.Tweening;
using UnityEngine;

public abstract class PatientClearBase : MonoBehaviour
{
    // 문 감쇠 스윙 비율 (열린 각도 대비).
    private const float DOOR_SWING_1ST_RATIO = 0.4f;
    private const float DOOR_SWING_2ND_RATIO = 0.2f;
    private const float DOOR_SWING_3RD_RATIO = 0.08f;
    private const int DOOR_SWING_PHASE_COUNT = 4;

    public abstract Sequence Play(
        Transform patientRoot,
        Transform bedTransform,
        Transform patientTransform);

    public abstract void ForceComplete(
        Transform patientRoot,
        Transform bedTransform,
        Transform patientTransform);

    // ── Door Helpers ─────────────────────────────────────────────

    protected Sequence CreateDoorSequence(
        Transform doorParent,
        float openAngle,
        float slamDuration,
        float stayOpenDuration,
        float swingDuration)
    {
        if (doorParent == null || doorParent.childCount < 2)
        {
            return DOTween.Sequence();
        }

        Transform panelA = doorParent.GetChild(0);
        Transform panelB = doorParent.GetChild(1);

        Quaternion originalA = panelA.localRotation;
        Quaternion originalB = panelB.localRotation;

        Sequence doorSequence = DOTween.Sequence();

        Quaternion openA = originalA * Quaternion.Euler(0f, openAngle, 0f);
        Quaternion openB = originalB * Quaternion.Euler(0f, -openAngle, 0f);

        doorSequence.Append(panelA.DOLocalRotateQuaternion(openA, slamDuration).SetEase(Ease.OutQuart));
        doorSequence.Join(panelB.DOLocalRotateQuaternion(openB, slamDuration).SetEase(Ease.OutQuart));

        doorSequence.AppendInterval(stayOpenDuration);

        float singleSwingTime = swingDuration / DOOR_SWING_PHASE_COUNT;

        Quaternion swingOut1A = originalA * Quaternion.Euler(0f, -openAngle * DOOR_SWING_1ST_RATIO, 0f);
        Quaternion swingOut1B = originalB * Quaternion.Euler(0f, openAngle * DOOR_SWING_1ST_RATIO, 0f);
        doorSequence.Append(panelA.DOLocalRotateQuaternion(swingOut1A, singleSwingTime).SetEase(Ease.InOutSine));
        doorSequence.Join(panelB.DOLocalRotateQuaternion(swingOut1B, singleSwingTime).SetEase(Ease.InOutSine));

        Quaternion swingIn2A = originalA * Quaternion.Euler(0f, openAngle * DOOR_SWING_2ND_RATIO, 0f);
        Quaternion swingIn2B = originalB * Quaternion.Euler(0f, -openAngle * DOOR_SWING_2ND_RATIO, 0f);
        doorSequence.Append(panelA.DOLocalRotateQuaternion(swingIn2A, singleSwingTime).SetEase(Ease.InOutSine));
        doorSequence.Join(panelB.DOLocalRotateQuaternion(swingIn2B, singleSwingTime).SetEase(Ease.InOutSine));

        Quaternion swingOut3A = originalA * Quaternion.Euler(0f, -openAngle * DOOR_SWING_3RD_RATIO, 0f);
        Quaternion swingOut3B = originalB * Quaternion.Euler(0f, openAngle * DOOR_SWING_3RD_RATIO, 0f);
        doorSequence.Append(panelA.DOLocalRotateQuaternion(swingOut3A, singleSwingTime).SetEase(Ease.InOutSine));
        doorSequence.Join(panelB.DOLocalRotateQuaternion(swingOut3B, singleSwingTime).SetEase(Ease.InOutSine));

        doorSequence.Append(panelA.DOLocalRotateQuaternion(originalA, singleSwingTime).SetEase(Ease.InOutSine));
        doorSequence.Join(panelB.DOLocalRotateQuaternion(originalB, singleSwingTime).SetEase(Ease.InOutSine));

        return doorSequence;
    }

    protected void ResetDoor(Transform doorParent)
    {
        if (doorParent == null || doorParent.childCount < 2)
        {
            return;
        }

        Transform panelA = doorParent.GetChild(0);
        Transform panelB = doorParent.GetChild(1);

        panelA.DOKill();
        panelB.DOKill();
    }

    // ── FX Helpers ───────────────────────────────────────────────

    protected void PlayFx(ParticleSystem fx) => FxHelper.Play(fx);
    protected void StopFx(ParticleSystem fx) => FxHelper.Stop(fx);
    protected void ClearFx(ParticleSystem fx) => FxHelper.Clear(fx);
    protected void PlayAllFx(GameObject fxRoot) => FxHelper.PlayAll(fxRoot);
    protected void ClearAllFx(GameObject fxRoot) => FxHelper.ClearAll(fxRoot);
}
