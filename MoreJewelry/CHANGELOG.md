## Please always delete/move your old probablykory.MoreJewelry.cfg file to apply the latest changes!

### 1.0.9
 * Fixed Bizarre bug interaction with Backpacks, causing bp_explorer backpack to accidentally grant Perception status effect.
 * Fix to ensure yaml-defined items configured with vanilla crafting tables work as expected.
 * Pulled in latest managers.
 * Minor project cleanups.

### 1.0.8
 * Fixed auto-pickup for a couple different prefab models.
 * Updated description of the default Silver band to indicate what the 'Aquatic' effect does.
 * Special thanks to Majestic for the bug reports. ❤️

### 1.0.7
 * Updated mod and manager dependencies for Valheim Ashlands 0.218.15
 * Added support for all the new SE_Stat properties for ring effects (Enable external Yaml and look at the everything ring for examples)
 * Fix for case-insensitive name matching of built-in JC effects
 * Fix crash when running on a full Valheim debug build

### 1.0.6
 * Minor update to ensure mocks work with Valheim 0.217.46
 * Small updates to default yaml config which includes a new ring, Humite Ring of Perception
 * Changed default silver ring effect to be Aquatic (gift damage buff while wet)

### 1.0.5
 * Bugfix: Ensure custom skill types work when used in adhoc status effects (ones defined with yaml).  Thanks William!

### 1.0.4
 * Set Config.SaveOnConfigSet to ensure config settings don't unintentionally revert.

### 1.0.3
 * Added soft dep for VNEI, disabling the template prefabs.
 * Bugfix for ConfigManager api, rebuilding settings works.

### 1.0.2
 * Bugfix for JC's original jewelry.  Lumberjacking, etc should all work again.  Thanks Majestic!

### 1.0.1
 * Fixes for the YamlConfigEditor.  Enabled proper color usage when using Azu's Unofficial Config Manager, and fallback when not.

### 1.0.0
 * Initial Version.
