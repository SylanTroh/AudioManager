
- [x] Implement other types of AudioZoneSyncCore
- [x] Editor script to select correct implementation based on amount of AudioZoneColliders
- [ ] Use AudioSettingColliders in AudioZoneSyncCore implementations to update AudioSettings
  - [ ] Editor script to map Setting from AudioSettingColliders to Index.
  - [ ] Deduplication if all settings (including priority) match
  - [ ] extra class which has mapping from index to Settings (voice, priority, ...), to then apply them to AudioSettingsManager on change
- [ ] Performance checking... somehow???

- [ ] refactoring, naming, small shit
