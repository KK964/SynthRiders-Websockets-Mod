# SynthRiders Websocket Integration

## Introduction

This is a mod for [SynthRiders](https://synthridersvr.com/). It allows you to connect to the websocket server and receive updates about the game.

## Setup

To install you need to either download a release, or build the mod yourself.

### Pre-requisites

1. [SynthRiders](https://synthridersvr.com/)
2. [MelonLoader](https://melonwiki.xyz/#/)

### Download

1. Download the latest release from [GitHub](https://github.com/KK964/SynthRiders-Websockets-Mod/releases)
2. Extract the zip file
3. Copy the dlls to your SynthRiders MelonLoader Mods folder (typically a folder called `Mods` in your SynthRiders installation.  If you see a text file named "PLACE MODS HERE.txt", you're in the right place.)

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

Connect to the websocket server; the server will send updates about the game.  By default, the websocket host will run at `ws://localhost:9000`.  Host and Port can be configured in the MelonPreferences.cfg file if an alternate config is required (e.g. for 2 PC stream setups)

## Events

All events are JSON and follow this general structure:

```json
{ "eventType": "EventType", "data": {}}
```

### SongStart 

Emitted when the map begins playing.

```json
{
    "eventType":"SongStart",
	"data":{
     	"song":"2 Phut Hon (Kaiz Remix)",
        "difficulty":"Master",
        "author":"Phao",
        "beatMapper":"ICHDerHorst",
        "length":191.857,
        "bpm":128.0,
        "albumArt": ""
	}
}
```

`albumArt` - If album art is available, this contains a data url containing a base64-encoded PNG image.  (You can use this URL as-is in a web browser or browser source to display the image).  Otherwise it's an empty string.

### SongEnd

Emitted as the last note of the map is completed.

```json
{
    "eventType":"SongEnd",
    "data":{
        "song":"2 Phut Hon (Kaiz Remix)",
        "perfect":350,
        "normal":126,
        "bad":281,
        "fail":2,
        "highestCombo":482
    }
}
```

### PlayTime

Emitted once per second when the song is playing.

```json
{"eventType":"PlayTime","data":{"playTimeMS":19662.48}}
```

- `playTimeMS` - Current play time position, in milliseconds.

### NoteHit

Emitted on every note hit successfully

```json
{
    "eventType":"NoteHit",
    "data":{
        "score":938,
        "combo":1,
        "multiplier":1,
        "completed":1.0,
        "lifeBarPercent":1.0
    }
}
```

  - `score` - Total score after the note is hit
  - `combo` - Number of consecutive hits made so far.  This resets after a note miss.
  - `multiplier` - Current score multiplier.  Runs from 1 to 6.
  - `completed` - Indicates the percentage of notes completed in the map.? (CHECK THIS)
  - `lifeBarPercent` - A number between 0 and 1 indicating life bar percentage.

### NoteMiss

```json
{
    "eventType":"NoteMiss",
    "data":{
        "multiplier":2,
        "lifeBarPercent":0.8333333
    }
}
```

### EnterSpecial

```json
{"eventType":"EnterSpecial","data":{}}
```

### CompleteSpecial

```json
{"eventType":"CompleteSpecial","data":{}}
```

### FailSpecial

```json
{"eventType":"FailSpecial","data":{}}
```

### SceneChange

Emitted when changing 'scenes'.  For example, changing to game play, summary, main menu, etc.  

Some scene names of interest:

* `3.GameEnd` - The summary scene after successfully completing a map where it shows your score, accuracy, etc.  

```json
{
    "eventType":"SceneChange",
    "data":{
        "sceneName":"3.GameEnd"
    }
}
```

### ReturnToMenu

Emitted when the user selects the "Return to Menu" button on the game pause screen.

```
{"eventType":"ReturnToMenu","data":{}}
```

