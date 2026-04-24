-Implement other types of AbstractAudioZonePlayerObjectSync
-Editor script to select correct implementation based on amound of AudioZoneColliders
-Use AudioSettingColliders in AbstractAudioZonePlayerObjectSync implementations to update AudioSettings
---Editor script to map Setting from AudioSettingColliders to Index.
---Deduplication if all settings (including priority) match
---extra class which has mapping from index to Settings (voice, priroity, ...), to then apply them to AudioSettingsManager on change
-Performance checking... somehow???

-refactoring, naming, small shit