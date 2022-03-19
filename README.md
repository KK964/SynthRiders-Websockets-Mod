# SynthRiders Websocket Integration

## Introduction

This is a mod for [SynthRiders](https://synthridersvr.com/). It allows you to connect to the websocket server and receive updates about the game.

## Setup

To install you need to either download a release, or build the mod yourself.

### Pre-requisites

1. [SynthRiders](https://synthridersvr.com/)
2. [MelonLoader](https://melonwiki.xyz/#/)
3. [SynthRiders Websocket Integration](https://github.com/KK964/SynthRiders-Websockets-Mod/releases)

### Download

1. Download the latest release from [GitHub](https://github.com/KK964/SynthRiders-Websockets-Mod/releases)
2. Extract the zip file
3. Copy the dlls to your SynthRiders MelonLoader Mods folder

### Build

1. Clone the repository
2. Inside visual studio, open `/src` as the project
3. Build the project
4. The resulting dll will be in `/src/bin/Debug/` as `SynthRidersWebsockets.dll`
5. Copy the dll to your SynthRiders MelonLoader Mods folder
6. Copy `websocket-sharp.dll` to the Mods folder

### MelonPreferences.cfg

- Host: The hostname of the websocket server
- Port: The port of the websocket server

---

## Usage

Connect to the websocket server; the server will send updates about the game.

## Events

- SongStart:
  - `SongStart {"song":"Song Name", "difficulty": "Difficulty", "author": "Author", "length": Length}`
- SongEnd:
  - `SongEnd {"song":"Song Name", "perfect": Perfect, "normal": Normal, "bad": Bad, "fail": Fail, "highestCombo": HighestCombo}`
- NoteHit:
  - `NoteHit {"combo": Combo, "completed": Completed}`
- NoteMiss:
  - `NoteMiss {}`
- EnterSpecial:
  - `EnterSpecial {}`
- CompleteSpecial:
  - `CompleteSpecial {}`
- FailSpecial:
  - `FailSpecial {}`
