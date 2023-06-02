![Splash](https://raw.githubusercontent.com/probablykory/valheim-mods/main/MoreCrossbows/splash.jpg)  
A Valheim mod which adds several crossbows and bolts.

## Features

![Showcase](https://raw.githubusercontent.com/probablykory/valheim-mods/main/MoreCrossbows/showcase.jpg)

Progression-appropriate crossbow options for every 'age'.  
New bolts to provide direct elemental damage.  
New recipes for crafting the existing bolts from Workbench or Forge.  
Brand new Area Effect bolts, based on the ooze & bile bombs, but includes fire and ice as well.  

![Showcase AoE](https://raw.githubusercontent.com/probablykory/valheim-mods/main/MoreCrossbows/showcase-aoe.jpg)

### Detailed list of new items
Wooden crossbow  
Bronze crossbow  
Iron crossbow  
Silver crossbow  
Blackmetal crossbow  
Wood bolt  
Fire bolt  
Ooze bolt (change config to enable)  
Surtling bolt (change config to enable)  
Poison bolt  
Silver bolt  
Frost bolt  
Ice bolt (change config to enable)  
Lightning bolt (change config to enable)  
Lightning arrow (change config to enable)  
Bile bolt (change config to enable)  
Flametal bolt (change config to enable)  

## Rationale 

During my first playthrough of Mistlands, the Arbalest fundamentally changed my experience of the game.  I *love* that thing.  On subsequent playthroughs I found myself wanting progression-appropriate crossbows to compliment the bows.  So I made some 🙂  

The reasoning behind the Lighting Bolts and Arrows is similar.  I wanted a way to have some fun with Eitr that didn't demand I change armor and weapons.  

Lastly, I added the Area Effect bolts to spice things up a bit, give the mod something fun outside the vanilla experience.

## Installation

> **_NOTE:_**  Users that are upgrading should delete their config file and run the game to let the plugin generate a new one.  Settings can and do change between versions, and old configs may be incompatible.

### Manual

  * Un-zip `MoreCrossbows.dll` to your `/Valheim/BepInEx/plugins/` folder.

### Thunderstore (manual install)

  * Go to Settings > Import local mod > Select `MoreCrossbows.zip`.
  * Click "OK/Import local mod" on the pop-up for information.

## Translations

This mod has full support for translations, but at the moment only has values for English & Russian.  If you wish to add a translation, please do the following:  
 1.  Copy the json file located at `$your_valheim_folder/BepInEx/plugins/MoreCrossbows/Translations/English/english.json` into a new directory within `Translations`
 2.  Name that directory the same as the language you wish to translate (refer to this [list](https://valheim-modding.github.io/Jotunn/data/localization/language-list.html) of languages; the name must be exact).
 3.  Change the values for all the json keys to what they should be.
 
 Now when you run Valheim, it should see the translations automatically.  Lastly, consider sending me the json file (via an issue on [github](https://github.com/probablykory/valheim-mods) or on discord).  I'd like to add as many translations as I can. 🙂

## Support me

I spend countless hours every day working on, updating, and fixing mods for everyone to enjoy.  While I will never ask for anyone to pay me to make a mod or add a feature, any [support](https://www.paypal.com/paypalme/probablyk) is greatly appreciated!

## Changelog

### v1.2.3
 * Added customized gem effects for crossbows, loaded if and only if Jewelcrafting is installed.
   (This change is in tandem with a PR to Jewelcrafting itself, enabling gem effects for the arbalest.)
 * Slight adjustment to translation naming.
 * Russian translation added, thanks to Migilian!

### v1.2.2
 * Minor fixes to config, enabled reloading when file changes or when config manager is used.

### v1.2.1
 * Minor bugfix for Surtling bolt.

### v1.2.0
 * Added new AOE bolts
 * General configuration overhaul.
 * Damage is now configurable.  Defaults are balanced well for vanilla, but those who play on higher-difficulty servers or in radically different environments can now tweak the damage to suit their needs.
 * Localization updates.  Plugin now writes default translations to disk, allowing users to modify/add languages.

### v1.1.2
 * Fixed minimum table level requirement defaults for the later crossbows.
 * Texture fixes for all crossbows, should now have a more pixelated look, just like all vanilla valheim weapons.

### v1.1.1
 * Repo move to accommodate other mod plugin projects.
 * Minor balance adjustments to crossbow damage.
 * Increased default create and update costs for Iron, Silver and Blackmetal xbows.
 * Updated build targets for BepInEx and Jotunn.

### v1.1.0
 * Item and Recipe requirements are now configurable.
 * Adjusted default upgrade costs of several xbows.
 * Wooden Crossbow size reduced by 5%.
 * Several bugfixes.

### 1.0.0
 * Initial Version

## Known issues
If you find a problem, please visit my [github](https://github.com/probablykory/valheim-mods)
