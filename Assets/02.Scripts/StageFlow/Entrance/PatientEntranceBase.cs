using DG.Tweening;
using UnityEngine;

public abstract class PatientEntranceBase : MonoBehaviour
{
    public abstract Sequence Play(
        Transform bedTransform,
        Vector3 finalPosition,
        Quaternion finalRotation);

    public abstract void ForceComplete(
        Transform bedTransform,
        Vector3 finalPosition,
        Quaternion finalRotation);

    // Slam door inward, then swing back and forth like a loose door (damped oscillation).
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

        // Phase 1: Slam open inward (bed crashes through).
        Quaternion openA = originalA * Quaternion.Euler(0f, openAngle, 0f);
        Quaternion openB = originalB * Quaternion.Euler(0f, -openAngle, 0f);

        doorSequence.Append(panelA.DOLocalRotateQuaternion(openA, slamDuration).SetEase(Ease.OutQuart));
        doorSequence.Join(panelB.DOLocalRotateQuaternion(openB, slamDuration).SetEase(Ease.OutQuart));

        // Stay open while bed passes through.
        doorSequence.AppendInterval(stayOpenDuration);

        // Phase 2: Damped swinging (loose door feel).
        float swing1Ratio = 0.4f;
        float swing2Ratio = 0.2f;
        float swing3Ratio = 0.08f;
        float singleSwingTime = swingDuration / 4f;

        // Swing outward (rebound).
        Quaternion swingOut1A = originalA * Quaternion.Euler(0f, -openAngle * swing1Ratio, 0f);
        Quaternion swingOut1B = originalB * Quaternion.Euler(0f, openAngle * swing1Ratio, 0f);
        doorSequence.Append(panelA.DOLocalRotateQuaternion(swingOut1A, singleSwingTime).SetEase(Ease.InOutSine));
        doorSequence.Join(panelB.DOLocalRotateQuaternion(swingOut1B, singleSwingTime).SetEase(Ease.InOutSine));

        // Swing inward again (smaller).
        Quaternion swingIn2A = originalA * Quaternion.Euler(0f, openAngle * swing2Ratio, 0f);
        Quaternion swingIn2B = originalB * Quaternion.Euler(0f, -openAngle * swing2Ratio, 0f);
        doorSequence.Append(panelA.DOLocalRotateQuaternion(swingIn2A, singleSwingTime).SetEase(Ease.InOutSine));
        doorSequence.Join(panelB.DOLocalRotateQuaternion(swingIn2B, singleSwingTime).SetEase(Ease.InOutSine));

        // Swing outward (tiny).
        Quaternion swingOut3A = originalA * Quaternion.Euler(0f, -openAngle * swing3Ratio, 0f);
        Quaternion swingOut3B = originalB * Quaternion.Euler(0f, openAngle * swing3Ratio, 0f);
        doorSequence.Append(panelA.DOLocalRotateQuaternion(swingOut3A, singleSwingTime).SetEase(Ease.InOutSine));
        doorSequence.Join(panelB.DOLocalRotateQuaternion(swingOut3B, singleSwingTime).SetEase(Ease.InOutSine));

        // Settle to closed.
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
}
