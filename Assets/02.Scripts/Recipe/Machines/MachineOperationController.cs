using System;
using UnityEngine;

namespace DontDillyDally.Data
{
    public sealed class MachineOperationController
    {
        private readonly MachineDoor _door;
        private readonly ActionTimer _actionTimer;
        private readonly RunningMotion _runningMotion;
        private readonly SFXKey _loopSound;
        private readonly SFXKey _openSound;
        private readonly SFXKey _closeSound;
        private readonly SFXKey _completeSound;

        private AudioSource _loopSource;

        public MachineOperationController(
            MachineDoor door,
            ActionTimer actionTimer,
            RunningMotion runningMotion,
            SFXKey loopSound,
            SFXKey openSound,
            SFXKey closeSound,
            SFXKey completeSound)
        {
            _door = door;
            _actionTimer = actionTimer;
            _runningMotion = runningMotion;
            _loopSound = loopSound;
            _openSound = openSound;
            _closeSound = closeSound;
            _completeSound = completeSound;
        }

        public bool IsRunning => _actionTimer != null && _actionTimer.IsRunning;
        public bool IsDoorOpen => _door == null || _door.IsOpen;

        public void StartLocal(float duration, Action onCompleted)
        {
            _door?.LockClosed();
            _runningMotion?.TryStart();

            if (_actionTimer == null)
            {
                onCompleted?.Invoke();
                return;
            }

            if (_actionTimer.TryStart(duration, onCompleted))
            {
                StartLoop();
            }
        }

        public void StartRemote(float duration)
        {
            _door?.LockClosed();
            _runningMotion?.TryStart();
            _actionTimer?.TryStart(duration, StopRunningFeedback);
            StartLoop();
        }

        public void CompleteLocal()
        {
            _actionTimer?.Cancel();
            StopRunningFeedback();
        }

        public void CompleteRemote()
        {
            _actionTimer?.Cancel();
            StopRunningFeedback();
        }

        public void UnlockDoor()
        {
            _door?.Unlock();
        }

        public bool TryOpenDoor(bool playSound = true)
        {
            bool changed = _door == null || _door.TryOpen();
            if (changed && playSound)
            {
                PlayOneShot(_openSound);
            }

            return changed;
        }

        public bool TryCloseDoor(bool playSound = true)
        {
            bool changed = _door == null || _door.TryClose();
            if (changed && playSound)
            {
                PlayOneShot(_closeSound);
            }

            return changed;
        }

        public void PlayComplete()
        {
            PlayOneShot(_completeSound);
        }

        public void StopLoop()
        {
            if (_loopSource == null || SoundManager.Instance == null)
            {
                _loopSource = null;
                return;
            }

            SoundManager.Instance.StopSFX(_loopSource);
            _loopSource = null;
        }

        private void StartLoop()
        {
            if (_loopSource != null || _loopSound == SFXKey.None || SoundManager.Instance == null)
            {
                return;
            }

            _loopSource = SoundManager.Instance.PlayLoop(_loopSound);
        }

        private void StopRunningFeedback()
        {
            _runningMotion?.StopMotion();
            StopLoop();
        }

        private static void PlayOneShot(SFXKey sound)
        {
            if (sound == SFXKey.None)
            {
                return;
            }

            SoundManager.Instance?.Play(sound, SoundType.Local);
        }
    }
}
