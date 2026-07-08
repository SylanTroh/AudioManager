# AudioManager
An UDON system designed to set player voice volume.

Features Include:
- Fake Audio Occlusion using Trigger Colliders to define 'AudioZones'
- Changing player voice settings directly using Trigger Colliders to define 'AudioSettingZones'
- A manager script that allows for multiple systems that change player voice settings to coexist in a single world by using a priority system.

# Installation
1. Go to https://sylantroh.github.io/SylanVCC/ and click "Add to VCC"
2. Click Manage Project in the creator companion and press the plus button next to AudioManager
3. Create an empty game object in your scene and add an 'AudioSettingManager' component. You can use this component to set the default audio settings for your world.
4. If you would like to use fake audio occlusion, add a gameobject with an 'AudioZoneManager' component to your scene as well. You can use component to set what audio settings will be in use when players don't share an AudioZone as well as change the default priority of AudioZones.

# Setting up Fake Audio Occlusion
The AudioZone system simulates audio occlusion by applying an audiosetting that makes players unable to hear each other if certain conditions are met.

Each AudioZoneCollider script has at least one "ID" which is a string that corresponds to an AudioZone. These IDs do not need to be unique, and a single AudioZoneCollider can have multiple IDs and therefore be a part of multiple AudioZones. If a player enters an AudioZoneCollider, they are tagged with all of its IDs. A player is considered to be inside an AudioZone as long as they are inside at least one AudioZoneCollider that has a matching ID. The AudioZoneManager will apply its audiosetting to players, making them unable to hear each other, if the two players do not share any AudioZones.

If a AudioZone has the id "", that is, the empty string, it will match players who are not in any AudioZones in addition to the AudioZone with id "". This can be used to create "Transition Zones" or zones where players can hear players who are inside of a zone, while still hearing players who are outside of it. Finally, if a player is in a AudioZoneCollider with the "Is Negative Zone" checkbox enabled, this will prevent them from matching players in AudioZones with the IDs specified on that AudioZoneCollider.

## Video Guide
**Important Note: The comments about players clipping through walls and floors no longer applies, zones can match the size of rooms exactly.** Player capsule colliders are no longer used since version 1.7.0, zones are instead checked for specifically at the head position. Local and remote players will always agree with each other since which zones players are in is manually synced by this system.
[![Setup](https://img.youtube.com/vi/9Saxs7rcltQ/hqdefault.jpg)](https://youtu.be/9Saxs7rcltQ)

# Known Issues
- Non uniform scaling of audio zones works, however the in scene view drawn gizmos and handles will be incorrect, especially once skews due to rotations within non uniformly scaled objects are introduced. Rely on gizmos drawn by the actual colliders themselves in this case, or avoid non uniform scaling for audio zones.

---

# Update Log

## Version 1.7.0
- Support players being in stations, be it in world or on avatars, the system continues to detect zone changes (resolves previously known issue)
- Support audio and setting zone game objects or their colliders getting toggled at runtime, the system will respect their active state
- Remove discrepancy between local and remote players
- Significantly reduce the need to account for players clipping into zones they should not be in (resolves previously known issue)
- Change detection of audio zones, no longer using player trigger events, instead using periodic physics overlap checks centered at the player's head
- Add per player object used for syncing which zones players are in, optimized for lowest network bandwidth usage, gets created automatically
- Add automatic migration ensuring existing scenes should continue working without any mandatory changes
- Add migration window with migration instructions, such as potentially un-shrinking zones and changing Head Check Radius
- Add option to configure the radius used to check for audio zones around the player's head for backwards compatibility
- Disable all collision for the `AudioZones` layer in project settings automatically upon creating the layer as well as once upon migration of a scene
- Allow using a layer different than the `AudioZones` layer
- Remove ability to change setting zone settings at runtime
- Ensure the AudioSettingManager API can be used as early as possible for every player, even those having just joined
- Fix voice of late joiners not getting updated until they or oneself changes zones
- Force audio zones and setting zones to be trigger colliders, except for mesh colliders
- Support all scripts being part of prefab instances
- Support multi selection and editing of zones
- Improve in editor support for zones with multiple colliders
- Remember zone inspector foldout and shrink size throughout Unity session
- Change default shrink/growth from 0.5 to 0.25 in zones inspector
- Add explicit grow zone button in the zones inspector
- Add scene view gizmos for capsule colliders used for zones
- Add preprocessor define `AUDIO_MANAGER_DISABLE_INFO_LOGGING` to disable log messages logged every time a player's voice settings get changed
- Add preprocessor define `AUDIO_MANAGER_DEBUG` toe enable debug logging
- Make editor asmdef truly only compile for editor
- Remove needless console messages in the unity editor when entering play mode or building the world
- Add temporary kill switch for first usage in the real world
- Add [UdonGitFilters](https://github.com/JanSharp/UdonGitFilters) to gitattributes, relevant/useful for contributors

## Version 1.6.0
- Added VoiceApplicator component to manage smooth fading between audio settings
- VoiceApplicator is automatically added to an AudioSettingManager on build/play
- Audiosettings now need two additional parameters (for a total of 7) to support fading. The old format will still work for now, but is depracated.
