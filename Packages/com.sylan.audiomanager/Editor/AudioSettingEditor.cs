using System;
using System.Collections.Generic;
using System.Linq;
using Sylan.AudioManager.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace Sylan.AudioManager
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AudioSettingCollider))]
    public class AudioSettingEditor : ZoneEditor
    {
        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.Pickable)]
        private static void DrawGizmos(AudioSettingCollider audioZone, GizmoType gizmoType)
        {
            DrawColliderGizmos(audioZone, Color.yellow);
        }

        protected override Migrator GetMigrator() => AudioSettingColliderMigrator.SingletonInstance;
    }

    public static class AudioSettingInitialize
    {
        public static int zoneIdCount;

        public static bool RunOnBuild()
        {
            AudioSettingCollider[] settingZones = SerializedPropertyUtils.FindAllObjects<AudioSettingCollider>();
            if (settingZones.Length == 0) return true;
            if (!SerializedPropertyUtils.TryFindSerializedObject(out AudioZoneManager manager, out SerializedObject managerSo)) return false;

            if (AudioZoneLayerInit.TryGetAudioZoneLayer(out int audioZoneLayer, manager))
            {
                SerializedPropertyUtils.SetLayerAndApply(settingZones.Select(z => z.gameObject).ToArray(), audioZoneLayer);
            }

            var settingIdDict = new Dictionary<AudioSettingData, int>();
            var allAudioSettings = new List<AudioSettingData>();

            foreach (var settingZone in settingZones)
            {
                int settingIndex = GetOrAdd(settingIdDict, allAudioSettings, new AudioSettingData(settingZone));
                SerializedObject so = new(settingZone);
                so.FindProperty(nameof(AudioSettingCollider.settingIndex)).intValue = settingIndex;
                so.ApplyModifiedProperties();
            }

            AudioZoneInitialize.MakeAllAttachedPrimitiveCollidersTriggers(settingZones);

            zoneIdCount = allAudioSettings.Count;

            if (managerSo != null)
            {
                void SetArray(string propertyName, Action<SerializedProperty, AudioSettingData> setValue)
                {
                    SerializedPropertyUtils.SetArrayProperty(managerSo.FindProperty(propertyName), allAudioSettings, setValue);
                }
                SetArray(nameof(AudioZoneManager.allAudioSettingsPriority), (p, v) => p.intValue = v.priority);
                SetArray(nameof(AudioZoneManager.allAudioSettingsVoiceGain), (p, v) => p.floatValue = v.voiceGain);
                SetArray(nameof(AudioZoneManager.allAudioSettingsVoiceNear), (p, v) => p.floatValue = v.voiceNear);
                SetArray(nameof(AudioZoneManager.allAudioSettingsVoiceFar), (p, v) => p.floatValue = v.voiceFar);
                SetArray(nameof(AudioZoneManager.allAudioSettingsVolumetricRadius), (p, v) => p.floatValue = v.volumetricRadius);
                SetArray(nameof(AudioZoneManager.allAudioSettingsLowpassFilter), (p, v) => p.boolValue = v.lowpassFilter);
                SetArray(nameof(AudioZoneManager.allAudioSettingsEnableFade), (p, v) => p.boolValue = v.enableFade);
                SetArray(nameof(AudioZoneManager.allAudioSettingsFadeDuration), (p, v) => p.floatValue = v.fadeDuration);
                managerSo.ApplyModifiedProperties();
            }

            return true;
        }

        private static int GetOrAdd(
            Dictionary<AudioSettingData, int> settingIdDict,
            List<AudioSettingData> allAudioSettings,
            AudioSettingData settingData)
        {
            if (settingIdDict.TryGetValue(settingData, out var value)) return value;

            value = settingIdDict.Count;
            settingIdDict.Add(settingData, value);
            allAudioSettings.Add(settingData);
            return value;
        }

        private readonly struct AudioSettingData : IEquatable<AudioSettingData>
        {
            public readonly int priority;

            public readonly float voiceGain;
            public readonly float voiceNear;
            public readonly float voiceFar;
            public readonly float volumetricRadius;
            public readonly bool lowpassFilter;

            public readonly bool enableFade;
            public readonly float fadeDuration;

            public AudioSettingData(AudioSettingCollider settingZone)
            {
                priority = settingZone.priority;
                voiceGain = settingZone.voiceGain;
                voiceNear = settingZone.voiceNear;
                voiceFar = settingZone.voiceFar;
                volumetricRadius = settingZone.volumetricRadius;
                lowpassFilter = settingZone.lowpassFilter;
                enableFade = settingZone.enableFade;
                fadeDuration = settingZone.fadeDuration;
            }

            public override readonly bool Equals(object obj)
            {
                return obj is AudioSettingData data && Equals(data);
            }

            public readonly bool Equals(AudioSettingData other)
            {
                return priority == other.priority
                    && voiceGain == other.voiceGain
                    && voiceNear == other.voiceNear
                    && voiceFar == other.voiceFar
                    && volumetricRadius == other.volumetricRadius
                    && lowpassFilter == other.lowpassFilter
                    && enableFade == other.enableFade
                    && fadeDuration == other.fadeDuration;
            }

            public override readonly int GetHashCode()
            {
                var result = new HashCode();
                result.Add(priority);
                result.Add(voiceGain);
                result.Add(voiceNear);
                result.Add(voiceFar);
                result.Add(volumetricRadius);
                result.Add(lowpassFilter);
                result.Add(enableFade);
                result.Add(fadeDuration);
                return result.ToHashCode();
            }
        }
    }
}
