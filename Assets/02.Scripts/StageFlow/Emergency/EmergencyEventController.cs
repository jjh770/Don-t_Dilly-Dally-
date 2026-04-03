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
        private static readonly CraftedMaterialType[] s_trayTargets =
        {
            CraftedMaterialType.SedativeSyringe,
            CraftedMaterialType.Defibrillator,
            CraftedMaterialType.BloodPack
        };

        private static readonly DiagnosisScanType[] s_diagnosisTargets =
        {
            DiagnosisScanType.Ultrasound,
            DiagnosisScanType.Radiograph,
            DiagnosisScanType.Encephalograph
        };

        private readonly StageEmergencySettings _settings;

        public bool IsActive { get; private set; }
        public EmergencyEventKind CurrentKind { get; private set; }
        public EmergencyTriggerSource CurrentTriggerSource { get; private set; }
        public CraftedMaterialType CurrentTrayTarget { get; private set; }
        public DiagnosisScanType CurrentDiagnosisTarget { get; private set; }
        public bool IsDiagnosisOperating { get; private set; }
        public float DiagnosisOperationRemainingTime => !IsDiagnosisOperating
            ? 0f
            : Mathf.Max(0f, _settings.DiagnosisOperationDurationSec - (Time.unscaledTime - _diagnosisOperationStartedAt));
        public float RemainingTime => !IsActive
            ? 0f
            : Mathf.Max(0f, _durationSec - (Time.unscaledTime - _startedAt));

        private float _startedAt;
        private float _durationSec;
        private float _diagnosisOperationStartedAt;

        public EmergencyEventController(StageEmergencySettings settings)
        {
            _settings = settings != null
                ? new StageEmergencySettings(settings)
                : new StageEmergencySettings();
        }

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
            IsDiagnosisOperating = false;
            _startedAt = 0f;
            _durationSec = 0f;
            _diagnosisOperationStartedAt = 0f;
        }

        public bool CanAcceptNormalTraySubmission()
        {
            return !IsActive;
        }

        public bool CanSubmitTrayToPatient()
        {
            return !IsActive || CurrentKind == EmergencyEventKind.Tray;
        }

        public bool EvaluateEmergencyMaterialSubmission(CraftedMaterialType materialType)
        {
            if (!IsActive || CurrentKind != EmergencyEventKind.Tray)
                return false;

            return materialType != CraftedMaterialType.None && materialType == CurrentTrayTarget;
        }

        public bool HasTimedOut()
        {
            return IsActive && !IsDiagnosisOperating && RemainingTime <= 0f;
        }

        public EmergencyDiagnosisOperationResult TryBeginDiagnosisOperation(DiagnosisScanType diagnosisType)
        {
            if (!IsActive || CurrentKind != EmergencyEventKind.Diagnosis)
                return EmergencyDiagnosisOperationResult.Invalid;

            if (IsDiagnosisOperating)
                return EmergencyDiagnosisOperationResult.Invalid;

            if (diagnosisType == DiagnosisScanType.None)
                return EmergencyDiagnosisOperationResult.Invalid;

            if (diagnosisType != CurrentDiagnosisTarget)
                return EmergencyDiagnosisOperationResult.Failed;

            IsDiagnosisOperating = true;
            _diagnosisOperationStartedAt = Time.unscaledTime;
            return EmergencyDiagnosisOperationResult.Started;
        }

        public bool TryBeginDiagnosisOperationLocally(DiagnosisScanType diagnosisType)
        {
            if (!IsActive || CurrentKind != EmergencyEventKind.Diagnosis)
                return false;

            if (IsDiagnosisOperating)
                return false;

            if (diagnosisType == DiagnosisScanType.None || diagnosisType != CurrentDiagnosisTarget)
                return false;

            IsDiagnosisOperating = true;
            _diagnosisOperationStartedAt = Time.unscaledTime;
            return true;
        }

        public EmergencyResumeResult ResolveSuccess()
        {
            End();
            return EmergencyResumeResult.ResumeCurrentRecipe;
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
            IsDiagnosisOperating = false;
            _startedAt = Time.unscaledTime;
            _durationSec = kind == EmergencyEventKind.Tray
                ? _settings.TrayDurationSec
                : _settings.DiagnosisDurationSec;
            _diagnosisOperationStartedAt = 0f;
        }
    }

    public enum EmergencyDiagnosisOperationResult
    {
        Invalid = 0,
        Started,
        Failed
    }

    public enum EmergencyResumeResult
    {
        ResumeCurrentRecipe = 0,
        AdvanceToNextRecipe
    }
}
