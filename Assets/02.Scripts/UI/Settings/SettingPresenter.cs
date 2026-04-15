using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingPresenter
{
    private readonly SettingView _view;
    private readonly SoundManager _soundManager;

    private readonly Button _openButton;

    public SettingPresenter(SettingView view, SoundManager soundManager, Button openButton)
    {
        _view = view;
        _soundManager = soundManager;
        _openButton = openButton;

        _view.Initialize(this);

        _openButton.onClick.AddListener(HandleOpenButtonClicked);

        RefreshView();
    }

    public void Dispose()
    {
        _openButton.onClick.RemoveListener(HandleOpenButtonClicked);
    }

    public void HandleCloseRequested()
    {
    }

    private void HandleOpenButtonClicked()
    {
        _view.Show();
    }


    public void RefreshView()
    {
        if (_soundManager == null)
        {
            Debug.LogWarning("[SettingPresenter] SoundManager instance was not found.");
            return;
        }

        _view.SetBgmVolume(_soundManager.BGMVolume);
        _view.SetSfxVolume(_soundManager.SFXVolume);
    }

    public void HandleBgmVolumeChanged(float value)
    {
        if (_soundManager == null)
        {
            return;
        }

        _soundManager.SetBGMVolume(value);
    }

    public void HandleSfxVolumeChanged(float value)
    {
        if (_soundManager == null)
        {
            return;
        }

        _soundManager.SetSFXVolume(value);
    }

    public void HandleQuitButtonClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
