using UdonSharp;
using UnityEngine;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("Scrips/Audio Zone Collider")]
    public class AudioZoneCollider : UdonSharpBehaviour
    {
        /// <summary>
        /// <para>Used by editor scripting for migration purposes.</para>
        /// <para>Components from before this field existed will have an initial value of <c>0u</c>.</para>
        /// </summary>
        [HideInInspector] public uint scriptVersion;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public const uint CurrentScriptVersion = 1u;
        private void Reset() => scriptVersion = CurrentScriptVersion; // Runs when the component gets created or reset.
#endif

        [Header("Primary AudioZone ID")]
        /// <summary>
        /// Dont use this zoneId on runTime. It will be cleared.
        /// Use <c>zoneIdIndex</c> instead.
        /// Iu need the name, use <c>_AudioZoneManager.ZoneIdMapping[zoneIdIndex]</c>.
        /// </summary>
        public string zoneID = string.Empty;
        [HideInInspector] public int zoneIdIndex;

        [Header("Additional AudioZones. Useful for transitions.", order = 0)]
        [Space(-10, order = 1)]
        [Header("To match players who are not in a zone, set an empty string.", order = 2)]
        /// <summary>
        /// Do not use these zone IDs at runtime. They will be cleared.
        /// Use <c>transitionZoneIdIndexes</c> instead.
        /// If you need the name, use <c>_AudioZoneManager.ZoneIdMapping[index]</c>.
        /// </summary>
        public string[] transitionZoneIDs;
        [HideInInspector] public int[] transitionZoneIdIndexes;

        public bool isNegativeZone = false;

        [HideInInspector] public ulong combinedZoneIdsField1;
        [HideInInspector] public ulong combinedZoneIdsField2;
        [HideInInspector] public ulong combinedZoneIdsField3;
    }
}
