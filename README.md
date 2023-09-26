# AudioManager
An UDON system designed to set player voice volume. 

Features Include:
- Fake Audio Occlusion using Trigger Colliders to define 'AudioZones'
- Changing player voice settings directly using Trigger Colliders to define 'AudioSettingZones'
- A manager script that allows for multiple systems that change player voice settings to coexist in a single world by using a priority system.

# Installation
1. Go to https://sylantroh.github.io/SylanVCC/ and click "Add to VCC"
2. Click Manage Project in the creator companion and press the plus button next to AudioManager
3. Create an empty game object in your scene and add an 'AudioSettingManager' component.
4. If you would like to use fake audio occlusion, add a gameobject with an 'AudioZoneManager' component to your scene as well.
