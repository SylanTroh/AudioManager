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

Each AudioZone script has at least one "ID" which is a string that names the zones. If a player enters a collider with an AudioZone script, they are tagged with its ID, and if they leave that tag is removed. A player is considered to be in an AudioZone as long as they are inside at least one Collider that has a matching ID. The AudioZoneManager will apply its audiosetting to players, making them unable to hear each other, if the two players do not share any IDs. 

Finally, if a zone has the id "", that is, the empty string, it will match players who are not in any zones in addition to zones with id "". This can be used to create "Transition Zones" or zones where players can hear players who are inside of a zone, while still hearing players who are outside of it.

# Known Issues
- Sitting in a station disables a player's capsule collider. If a player moves through AudioZones in this state their audio will break. This is most commonly caused by one player "carrying" another using a station on their avatar, but would also occur if a world has stations that can move players through AudioZones.
- VRChat doesn't do a perfect job of syncing remote player locations. As such, remote players may slightly clip through walls, triggering audiozones on the other side of a wall. This effect is especially pronounced with multiple floors, where remote players will clip quite far into the floor below. This needs to be worked around by leaving some space between an AudioZone and the walls of the room.
