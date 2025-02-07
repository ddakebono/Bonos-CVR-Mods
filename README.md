# Bono's CVR Mods
This repo acts as a hideaway for some smaller mods that have no need for a standalone repo.

Some mods in here require [BTKUILib](https://github.com/BTK-Development/BTKUILib) to function!

## Current Things
- Self Moderation Normalization Fix
    - Fixes a bug with SelfModerationManager that causes voice normalization to be disable in most cases
- Movie Night Redirect
    - This mod is primarily in use for my movie night, but it can be used in other places too, see readme for usage!

## Building/Contribution

### Dependencies
- Suitable modern C# development environment
- ChilloutVR
- MelonLoader >= 0.6.1
- BTKUILib >= 2.0.0

### Preparing Dev Enviroment
* Create the "libs" folder in your newly cloned project root
* Open Command Prompt inside the "libs" folder and create a symlinks to folders within your ChilloutVR install

```
mklink /j ml "C:\Program Files (x86)\Steam\steamapps\common\ChilloutVR\MelonLoader"
mklink /j Managed "C:\Program Files (x86)\Steam\steamapps\common\ChilloutVR\ChilloutVR_Data\Managed"
mklink /j Mods "C:\Program Files (x86)\Steam\steamapps\common\ChilloutVR\Mods"
```

### Building

You can build

## Install/Usage
Install [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader) version 0.6.1 or higher, this is required as the mod has been updated specifically for 0.6.1!

Download the latest releases for the mods you want from [Releases](https://github.com/ddakebono/Bonos-CVR-Mods) and place in your ChilloutVR/Mods folder!

## Disclamer
### No mods in this repository are made by or affiliated with Alpha Blend Interactive

## Credits
* [HerpDerpinstine/MelonLoader](https://github.com/HerpDerpinstine/MelonLoader)
* [ChilloutVR](https://store.steampowered.com/app/661130/ChilloutVR/)