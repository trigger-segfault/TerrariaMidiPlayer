# Terraria Midi Player ![AppIcon](http://i.imgur.com/a6EWzOg.png)

[![Latest Release](https://img.shields.io/github/release/trigger-death/TerrariaMidiPlayer.svg?style=flat&label=version)](https://github.com/trigger-death/TerrariaMidiPlayer/releases/latest)
[![Latest Release Date](https://img.shields.io/github/release-date-pre/trigger-death/TerrariaMidiPlayer.svg?style=flat&label=released)](https://github.com/trigger-death/TerrariaMidiPlayer/releases/latest)
[![Total Downloads](https://img.shields.io/github/downloads/trigger-death/TerrariaMidiPlayer/total.svg?style=flat)](https://github.com/trigger-death/TerrariaMidiPlayer/releases)
[![Creation Date](https://img.shields.io/badge/created-august%202017-A642FF.svg?style=flat)](https://github.com/trigger-death/TerrariaMidiPlayer/commit/2a6570de78f8c2fd8816b8ba9380614e1badec0f)
[![Terraria Forums](https://img.shields.io/badge/terraria-forums-28A828.svg?style=flat)](https://forums.terraria.org/index.php?threads/61257/)
[![Discord](https://img.shields.io/discord/436949335947870238.svg?style=flat&logo=discord&label=chat&colorB=7389DC&link=https://discord.gg/vB7jUbY)](https://discord.gg/vB7jUbY)

A midi player for Terrarian instruments such as the Harp and Bell. Terraria Midi Player works by taking control of the mouse to click on the screen at the right coordinates to produce the correct notes as the midi plays. The program comes with a set of global hotkeys that can be pressed while focused on Terraria to force-stop the song or close Terraria Midi Player. The program will also force focus on Terraria when you start the song to avoid causing problems by clicking in unknown places.

![Window Preview](https://i.imgur.com/Sjs0sYB.png)

### [Wiki](https://github.com/trigger-death/TerrariaMidiPlayer/wiki) | [Credits](https://github.com/trigger-death/TerrariaMidiPlayer/wiki/Credits) | [Image Album](http://imgur.com/a/LtTvj)

### [![Get Terraria Midi Player](http://i.imgur.com/klNsxtL.png)](https://github.com/trigger-death/TerrariaMidiPlayer/releases/latest)

## About

* **Created By:** Robert Jordan
* **Language:** C#, WPF

## Requirements for Running
* .NET Framework 4.5.2 | [Offline Installer](https://www.microsoft.com/en-us/download/details.aspx?id=42642) | [Web Installer](https://www.microsoft.com/en-us/download/details.aspx?id=42643)
* Windows 7 or later
* Terraria for PC <sup>(will not play midis when it's not running)</sup>
* Terraria zoom setting must be at 100%

![100% Zoom Required](http://i.imgur.com/hZ9tm0U.png)

## Features
* Load Midis or ABC Notation files.
* Automatically attempts to fit notes within Terraria's two octaves.
* Midi customization:
  * Enable and disable tracks
  * Change a track's octave offset
  * Change note offset
  * Change the speed
* Ability to control where Magical Harp projectiles are aimed.
* Adjusts for mount height offsets.
* Use-time setting allowing you to play notes faster. (Assuming you have a way to modify it in-game as well)
* All settings are saved upon closing the window.
* Connect with others hosting from their Terraria Midi Player to sync songs for a performance *(Experimental)*
* Automatically stays focused on Terraria while playing songs in order to avoid accidental clicks. (Can be disabled)
* Play midis within the program to hear what they would sound like in Terraria.
* View a graph of all tracks within a midi to see where restrictions are causing problems.

## Tips
* Click on the projectile angle and drag for easy aiming.
* Use the mouse wheel while dragging to change the range.
* [MidiEditor](http://midieditor.sourceforge.net/) is a free tool for editing midis. You're going to need it in order to make most midis playable.

## Default Keybinds
* Force Close: `Numpad +` <sup>(<code>Page Up</code>, when no numpad is present)</sup>
* Play Midi: `Numpad 0` <sup>(<code>Delete</code>, when no numpad is present)</sup>
* Pause Midi: `Numpad 1` <sup>(<code>End</code>, when no numpad is present)</sup>
* Stop Midi: `Numpad 2` <sup>(<code>Page Down</code>, when no numpad is present)</sup>
* Toggle Mount Offset: `R` <sup>(Only when focused on Terraria)</sup>

## Youtube Previews
Below are some of the videos that have been recorded during the developement process of Terraria Midi Player.

[![Shake It!](http://i.imgur.com/GCdPcJm.png)](https://www.youtube.com/watch?v=NsOI2k8nKbQ) [![Through the Fire and Flames](http://i.imgur.com/sHypeWL.png)](https://www.youtube.com/watch?v=BAXK9uwE_BI) [![Tal Tal Heights](http://i.imgur.com/NNsoJCG.png)](https://www.youtube.com/watch?v=rP4O6BsBEh0)
