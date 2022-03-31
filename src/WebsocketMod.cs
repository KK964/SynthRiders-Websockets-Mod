﻿using System;
using System.IO;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

using Newtonsoft.Json;

using synth;
using Synth.Utils;

using UnityEngine;
using UnityEngine.Events;

using MelonLoader;

using SynthRidersWebsockets.Harmony;
using SynthRidersWebsockets.Events;

/*
* Todo:
* - Emit event if song failed.
* - Emit event is song is quit.
* - Previous high score
* - Emit what hand note was for (left, right, special one-hand, special two-hand)
* - Score on specific hits
*/
namespace SynthRidersWebsockets
{
    public class WebsocketMod : MelonMod
    {
        public static WebsocketMod Instance;
        private static GameControlManager gameControlManager;

        public static MelonPreferences_Category connectionCategory;

        /**
         * Keep track of last time play time was emitted so we only emit once per second.
         */
        private float lastPlayTimeEventMS = 0;
        private float currentPlayTimeMS = 0.0f;

        public override void OnApplicationStart() {
            Instance = this;
            connectionCategory = MelonPreferences.CreateCategory("Connection");
            string host = connectionCategory.CreateEntry<string>("Host", "localhost").Value;
            int port = connectionCategory.CreateEntry<int>("Port", 9000).Value;
            RuntimePatch.PatchAll();
            Websocket.Start($"{host}:{port}");
            LoggerInstance.Msg("[Websocket] Started Weboscket mod on " + host + ":" + port.ToString());
        }
        
        public override void OnApplicationQuit()
        {
            Websocket.Stop();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);

            EventDataSceneChange sceneChangeEvent = new EventDataSceneChange(sceneName);
            Send(new SynthRidersEvent<EventDataSceneChange>("SceneChange", sceneChangeEvent));
        }

        public override void OnUpdate()
        {
            // If song is playing, check the last play time.  If it's advanced at least one second, emit an update.
            if (gameControlManager != null && gameControlManager == GameControlManager.s_instance)
            {
                if (gameControlManager.SongIsPlaying)
                {
                    this.currentPlayTimeMS = gameControlManager.PlayTimeMS;
                    if (currentPlayTimeMS - lastPlayTimeEventMS > 999)
                    {
                        EventDataPlayTime playTimeEvent = new EventDataPlayTime(currentPlayTimeMS);
                        Send(new SynthRidersEvent<EventDataPlayTime>("PlayTime", playTimeEvent));
                        this.lastPlayTimeEventMS = currentPlayTimeMS;
                    }
                }
                else
                {
                    this.lastPlayTimeEventMS = 0.0f;
                }
            }
        }

        public void EmitReturnToMenuEvent()
        {
            Send(new SynthRidersEvent<object>("ReturnToMenu", new object()));
        }

        public void GameManagerInit()
        {
            if (gameControlManager == GameControlManager.s_instance) return;
            gameControlManager = GameControlManager.s_instance;
            try
            {
                LoggerInstance.Msg("Adding stage events!");
                StageEvents stageEvents = new StageEvents();
                stageEvents.OnSongStart = new UnityEvent();
                stageEvents.OnSongStart.AddListener(OnSongStart);
                stageEvents.OnSongEnd = new UnityEvent();
                stageEvents.OnSongEnd.AddListener(OnSongEnd);
                stageEvents.OnNoteHit = new UnityEvent();
                stageEvents.OnNoteHit.AddListener(OnNoteHit);
                stageEvents.OnNoteFail = new UnityEvent();
                stageEvents.OnNoteFail.AddListener(OnNoteFail);
                stageEvents.OnEnterSpecial = new UnityEvent();
                stageEvents.OnEnterSpecial.AddListener(OnEnterSpecial);
                stageEvents.OnCompleteSpecial = new UnityEvent();
                stageEvents.OnCompleteSpecial.AddListener(OnCompleteSpecial);
                stageEvents.OnFailSpecial = new UnityEvent();
                stageEvents.OnFailSpecial.AddListener(OnFailSpecial);
                GameControlManager.UpdateStageEventList(stageEvents);
            }
            catch (Exception e)
            {
                LoggerInstance.Msg(e.Message);
            }
        }
        
        private void OnSongStart()
        {
            // It'd be better to get this directly from within the game, but it seems
            // artwork isn't populated in the info provider.  This seems to work well enough
            // but do feel free to implement a better option if available.
            string albumArtPath = Directory.GetCurrentDirectory() + "\\SongStatusImage.png";
            string albumArtEncoded = null;

            if (File.Exists(albumArtPath))
            {
                albumArtEncoded = "data:image/png;base64," + System.Convert.ToBase64String(File.ReadAllBytes(albumArtPath));
            }

            EventDataSongStart songStartEvent = new EventDataSongStart(
                GameControlManager.s_instance.InfoProvider.TrackName,
                GameControlManager.s_instance.InfoProvider.CurrentDifficulty.ToString(),
                GameControlManager.s_instance.InfoProvider.Author,
                GameControlManager.s_instance.InfoProvider.Beatmapper,
                GameControlManager.CurrentTrackStatic.Song.clip.length,
                GameControlManager.CurrentTrackStatic.TrackBPM,
                albumArtEncoded
            );

            Send(new SynthRidersEvent<object>("SongStart", songStartEvent));
        }

        private void OnSongEnd()
        {
            Game_ScoreManager score = (Game_ScoreManager)ReflectionUtils.GetValue(GameControlManager.s_instance, "m_scoreManager");
            EventDataSongEnd songEndEvent = new EventDataSongEnd(
                GameControlManager.s_instance.InfoProvider.TrackName,
                GameControlManager.s_instance.InfoProvider.TotalPerfectNotes,
                GameControlManager.s_instance.InfoProvider.TotalNormalNotes,
                GameControlManager.s_instance.InfoProvider.TotalBadNotes,
                GameControlManager.s_instance.InfoProvider.TotalFailNotes,
                score.MaxCombo);

            Send(new SynthRidersEvent<EventDataSongEnd>("SongEnd", songEndEvent));
        }
        
        private void OnNoteHit()
        {
            Game_ScoreManager score = (Game_ScoreManager) ReflectionUtils.GetValue(GameControlManager.s_instance, "m_scoreManager");
            
            EventDataNoteHit noteHitEvent = new EventDataNoteHit(
                score.Score,
                score.CurrentCombo,
                score.NotesCompleted,
                score.TotalMultiplier,
                LifeBarHelper.GetScalePercent(),
                currentPlayTimeMS
            );

            Send(new SynthRidersEvent<EventDataNoteHit>("NoteHit", noteHitEvent));
        }
        
        private void OnNoteFail()
        {
            EventDataNoteMiss noteMissEvent = new EventDataNoteMiss(
                GameControlManager.s_instance.ScoreManager.TotalMultiplier,
                LifeBarHelper.GetScalePercent(),
                currentPlayTimeMS
            );

            Send(new SynthRidersEvent<EventDataNoteMiss>("NoteMiss", noteMissEvent));
        }

        private void OnEnterSpecial()
        {
            Send(new SynthRidersEvent<object>("EnterSpecial", new object()));
        }

        private void OnCompleteSpecial()
        {
            Send(new SynthRidersEvent<object>("CompleteSpecial", new object()));
        }

        private void OnFailSpecial()
        {
            Send(new SynthRidersEvent<object>("FailSpecial", new object()));
        }

        public void Send<T>(SynthRidersEvent<T> outputEvent)
        {
            if (Websocket.server == null)
            {
                return;
            }

            Websocket.Send(JsonConvert.SerializeObject(outputEvent));
        }

        public class Websocket
        {
            public static WebSocketServer server;
            public static EventSocket eventSocket;

            public static void Start(string host)
            {
                Instance.LoggerInstance.Msg($"[Websocket] Starting socket server on: ws://{host}/");
                server = new WebSocketServer("ws://" + host);
                server.AddWebSocketService<EventSocket>("/");
                server.Start();
            }

            public static void Stop()
            {
                if (server != null) server.Stop();
            }

            public static void Send(string message)
            {
                if (server == null || eventSocket == null) return;
                eventSocket.SendBroadcast(message);
            }

            public class EventSocket : WebSocketBehavior
            {
                public void SendBroadcast(string msg)
                {
                    Sessions.Broadcast(msg);
                }

                protected override void OnOpen()
                {
                    if (eventSocket == null) eventSocket = this;
                }

                protected override void OnError(WebSocketSharp.ErrorEventArgs e)
                {
                    if (eventSocket == null) eventSocket = this;
                }

                protected override void OnClose(CloseEventArgs e)
                {
                    if (eventSocket == null) eventSocket = this;
                }

                protected override void OnMessage(MessageEventArgs e)
                {
                }
            }
        }
    }
}
