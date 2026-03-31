using Photon.Voice.PUN;
using Photon.Voice.Unity;
using UnityEngine;

public class PlayerVoiceAbility : PlayerAbility
{
    private PhotonVoiceView _photonVoiceView;
    private Recorder _recorder;

    private void Start()
    {
        if (_photonVoiceView == null)
        {
            _photonVoiceView = GetComponent<PhotonVoiceView>();
        }

        if (_recorder == null && PhotonVoiceManager.Instance != null)
        {
            _recorder = PhotonVoiceManager.Instance.Recorder;
        }

        if (_recorder == null)
        {
            Debug.LogWarning("[PlayerVoiceAbility] 레코더가 없음");
            return;
        }

        _recorder.VoiceDetection = true;
        _recorder.VoiceDetectionThreshold = 0.005f;
        _recorder.VoiceDetectionDelayMs = 300;
    }

    private void Update()
    {
        if (_owner?.PhotonView == null)
        {
            return;
        }

        if (_owner.PhotonView.IsMine)
        {
            if (_recorder == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                _recorder.TransmitEnabled = !_recorder.TransmitEnabled;
            }

            return;
        }
    }
}
