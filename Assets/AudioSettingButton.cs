
using Sylan.AudioManager;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AudioSettingButton : UdonSharpBehaviour
{
    [Header("AudioSetting ID. Used for debugging.")]
    public string settingID = string.Empty;

    [Header("Lower number means higher priority", order = 0)]
    [Space(-10, order = 1)]
    [Header("Audiozones have priority 1000", order = 2)]
    public int priority = AudioSettingManager.DEFAULT_PRIORITY;

    public float voiceGain = AudioSettingManager.DEFAULT_VOICE_GAIN;
    public float voiceNear = AudioSettingManager.DEFAULT_VOICE_RANGE_NEAR;
    public float voiceFar = AudioSettingManager.DEFAULT_VOICE_RANGE_FAR;
    public float volumetricRadius = AudioSettingManager.DEFAULT_VOICE_VOLUMETRIC_RADIUS;
    public bool lowpassFilter = AudioSettingManager.DEFAULT_VOICE_LOWPASS;

    [SerializeField] private AudioSettingManager _AudioSettingManager;

    DataList audioSetting;

    private void Start()
    {
        DataToken[] tokens = { voiceGain, voiceNear, voiceFar, volumetricRadius, lowpassFilter };
        audioSetting = new DataList(tokens);
    }
    public void ApplyAudioSetting()
    {
        var triggeringPlayer = Networking.LocalPlayer;
        Debug.Log("[AudioManager] Apply Audio Setting " + settingID + " to " + triggeringPlayer.displayName + "-" + triggeringPlayer.playerId.ToString());

        triggeringPlayer.AddAudioSetting(_AudioSettingManager, settingID, priority, audioSetting);

        _AudioSettingManager.UpdateAudioSettings(triggeringPlayer);
    }
    public void RemoveAudioSetting()
    {
        var triggeringPlayer = Networking.LocalPlayer;
        Debug.Log("[AudioManager] Apply Audio Setting " + settingID + " to " + triggeringPlayer.displayName + "-" + triggeringPlayer.playerId.ToString());

        triggeringPlayer.RemoveAudioSetting(_AudioSettingManager, settingID);

        _AudioSettingManager.UpdateAudioSettings(triggeringPlayer);
    }
}
