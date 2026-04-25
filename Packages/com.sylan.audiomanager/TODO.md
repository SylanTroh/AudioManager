
- [x] Implement other types of AudioZoneSyncCore
- [x] Editor script to select correct implementation based on amount of AudioZoneColliders
- [x] Use AudioSettingColliders in AudioZoneSyncCore implementations to update AudioSettings
  - [x] Editor script to map Setting from AudioSettingColliders to Index.
  - [x] Deduplication if all settings (including priority) match
  - [x] ~~extra class~~ audio zone manager which has mapping from index to Settings (voice, priority, ...)
    - [x] to then apply them to AudioSettingsManager on change
- [ ] Performance checking... somehow???
- [x] Change the AudioZones layer in project settings to not collide with anything anymore
- [ ] Handle network congestion, using `Networking.Suffering`

- [ ] refactoring, naming, small shit
