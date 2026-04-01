using DontDillyDally.Data;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    public enum EmergencyEventKind
    {
        None = 0,
        Tray,
        Diagnosis
    }

    public enum EmergencyTriggerSource
    {
        None = 0,
        RecipeFail,
        MiniGameFail,
        Random
    }

    public sealed class EmergencyEventController
    {
        private const float TrayDurationSec = 20f;
        private const float DiagnosisDurationSec = 5f;

        private static readonly CraftedMaterialType[] s_trayTargets =
        {
            CraftedMaterialType.SedativeSyringe,
            CraftedMaterialType.Defibrillator,
            CraftedMaterialType.BloodPack
        };

        private static readonly DiagnosisScanType[] s_diagnosisTargets =
        {
            DiagnosisScanType.Ultrasound,
            DiagnosisScanType.XRay,
            DiagnosisScanType.ECG
        };

        public bool IsActive { get; private set; }
        public EmergencyEventKind CurrentKind { get; private set; }
        public EmergencyTriggerSource CurrentTriggerSource { get; private set; }
        public CraftedMaterialType CurrentTrayTarget { get; private set; }
        public DiagnosisScanType CurrentDiagnosisTarget { get; private set; }
        public float RemainingTime => !IsActive
            ? 0f
            : Mathf.Max(0f, _durationSec - (Time.unscaledTime - _startedAt));

        private float _startedAt;
        private float _durationSec;

        public bool CanBegin(
            EStagePhase currentPhase,
            bool isGameOver,
            bool isWaitingForRecipeSubmission,
            EmergencyTriggerSource triggerSource)
        {
            if (isGameOver)
                return false;

            if (currentPhase != EStagePhase.Playing)
                return false;

            if (IsActive)
                return false;

            if (triggerSource == EmergencyTriggerSource.None)
                return false;

            if (triggerSource == EmergencyTriggerSource.Random && !isWaitingForRecipeSubmission)
                return false;

            return true;
        }

        public bool TryBegin(EmergencyEventKind kind, EmergencyTriggerSource triggerSource)
        {
            if (IsActive)
                return false;

            if (kind == EmergencyEventKind.None)
                return false;

            if (triggerSource == EmergencyTriggerSource.None)
                return false;

            CraftedMaterialType trayTarget = kind == EmergencyEventKind.Tray
                ? s_trayTargets[Random.Range(0, s_trayTargets.Length)]
                : CraftedMaterialType.None;

            DiagnosisScanType diagnosisTarget = kind == EmergencyEventKind.Diagnosis
                ? s_diagnosisTargets[Random.Range(0, s_diagnosisTargets.Length)]
                : DiagnosisScanType.None;

            BeginInternal(kind, triggerSource, trayTarget, diagnosisTarget);
            return true;
        }

        public void SyncBegin(
            EmergencyEventKind kind,
            EmergencyTriggerSource triggerSource,
            CraftedMaterialType trayTarget,
            DiagnosisScanType diagnosisTarget)
        {
            BeginInternal(kind, triggerSource, trayTarget, diagnosisTarget);
        }

        public void End()
        {
            IsActive = false;
            CurrentKind = EmergencyEventKind.None;
            CurrentTriggerSource = EmergencyTriggerSource.None;
            CurrentTrayTarget = CraftedMaterialType.None;
            CurrentDiagnosisTarget = DiagnosisScanType.None;
            _startedAt = 0f;
            _durationSec = 0f;
        }

        public bool CanAcceptNormalTraySubmission()
        {
            return !IsActive;
        }

        public bool CanSubmitTrayToPatient()
        {
            return !IsActive || CurrentKind == EmergencyEventKind.Tray;
        }

        public bool EvaluateTraySubmission(SubmittedTray tray)
        {
            if (!IsActive || CurrentKind != EmergencyEventKind.Tray || tray == null)
                return false;

            if (!tray.IsSterilizedTray || tray.ContainedItemCount != 1)
                return false;

            var materialTypes = tray.GetContainedMaterialTypes();
            return materialTypes.Count == 1 && materialTypes[0] == CurrentTrayTarget;
        }

        public bool HasTimedOut()
        {
            return IsActive && RemainingTime <= 0f;
        }

        public EmergencyResumeResult ResolveSuccess()
        {
            EmergencyResumeResult result = CurrentTriggerSource switch
            {
                EmergencyTriggerSource.RecipeFail => EmergencyResumeResult.AdvanceToNextRecipe,
                EmergencyTriggerSource.MiniGameFail => EmergencyResumeResult.AdvanceToNextRecipe,
                EmergencyTriggerSource.Random => EmergencyResumeResult.ResumeCurrentRecipe,
                _ => EmergencyResumeResult.ResumeCurrentRecipe
            };

            End();
            return result;
        }

        public EmergencyResumeResult ResolveFailure()
        {
            End();
            return EmergencyResumeResult.ResumeCurrentRecipe;
        }

        private void BeginInternal(
            EmergencyEventKind kind,
            EmergencyTriggerSource triggerSource,
            CraftedMaterialType trayTarget,
            DiagnosisScanType diagnosisTarget)
        {
            IsActive = true;
            CurrentKind = kind;
            CurrentTriggerSource = triggerSource;
            CurrentTrayTarget = trayTarget;
            CurrentDiagnosisTarget = diagnosisTarget;
            _startedAt = Time.unscaledTime;
            _durationSec = kind == EmergencyEventKind.Tray ? TrayDurationSec : DiagnosisDurationSec;
        }
    }

    public enum EmergencyResumeResult
    {
        ResumeCurrentRecipe = 0,
        AdvanceToNextRecipe
    }
}
