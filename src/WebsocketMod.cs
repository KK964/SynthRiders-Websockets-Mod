using System;
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
/*
 * Todo:
 * - Emit event if song failed.
 * - Emit event is song is quit.
 * - Previous high score
 * - Emit what hand note was for (left, right, special one-hand, special two-hand
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
        private float lastPlayTimeMS = 0;

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

        class PlayTimeEvent
        {
            public float playTimeMS;

            public PlayTimeEvent(float playTimeMS)
            {
                this.playTimeMS = playTimeMS;
            }
        }
        public override void OnUpdate()
        {
            // If song is playing, check the last play time.  If it's advanced at least one second, emit an update.
            if (gameControlManager != null && gameControlManager == GameControlManager.s_instance)
            {
                if (gameControlManager.SongIsPlaying && (gameControlManager.PlayTimeMS - this.lastPlayTimeMS > 999))
                {
                    PlayTimeEvent playTimeEvent = new PlayTimeEvent(gameControlManager.PlayTimeMS);
                    Send("PlayTime", playTimeEvent);
                    this.lastPlayTimeMS = gameControlManager.PlayTimeMS;
                } else if (!gameControlManager.SongIsPlaying && this.lastPlayTimeMS > 0)
                {
                    this.lastPlayTimeMS = 0;
                }
            }
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

        class StageSongStartEvent
        {
            public string song;
            public string difficulty;
            public string author;
            public string beatMapper;
            public float length;
            public float bpm;

            public StageSongStartEvent(string song, string difficulty, string author, string beatMapper, float length, float bpm)
            {
                this.song = song;
                this.difficulty = difficulty;
                this.author = author;
                this.beatMapper = beatMapper;
                this.length = length;
                this.bpm = bpm;
            }
        }
        private void OnSongStart()
        {
            StageSongStartEvent stageSongStartEvent = new StageSongStartEvent(
                GameControlManager.s_instance.InfoProvider.TrackName,
                GameControlManager.s_instance.InfoProvider.CurrentDifficulty.ToString(),
                GameControlManager.s_instance.InfoProvider.Author,
                GameControlManager.s_instance.InfoProvider.Beatmapper,
                GameControlManager.CurrentTrackStatic.Song.clip.length,
                GameControlManager.CurrentTrackStatic.TrackBPM
            );

            Send("SongStart", stageSongStartEvent);
        }

        class StageSongEndEvent
        {
            public string song;
            public int perfect;
            public int normal;
            public int bad;
            public int fail;
            public int highestCombo;
            public StageSongEndEvent(string song, int perfect, int normal, int bad, int fail, int highestCombo)
            {
                this.song = song;
                this.perfect = perfect;
                this.normal = normal;
                this.bad = bad;
                this.fail = fail;
                this.highestCombo = highestCombo;
            }
        }
        private void OnSongEnd()
        {
            Game_ScoreManager score = (Game_ScoreManager)ReflectionUtils.GetValue(GameControlManager.s_instance, "m_scoreManager");
            StageSongEndEvent stageSongEndEvent = new StageSongEndEvent(
                GameControlManager.s_instance.InfoProvider.TrackName,
                GameControlManager.s_instance.InfoProvider.TotalPerfectNotes,
                GameControlManager.s_instance.InfoProvider.TotalNormalNotes,
                GameControlManager.s_instance.InfoProvider.TotalBadNotes,
                GameControlManager.s_instance.InfoProvider.TotalFailNotes,
                score.MaxCombo);
            Send("SongEnd", stageSongEndEvent);
        }

        class StageNoteHitEvent
        {
            public int score { get; set; }
            public int combo { get; set; }
            public int multiplier { get; set; }
            public float completed { get; set; }
            public float lifeBarPercent { get; set; }
            
            public StageNoteHitEvent(int score, int combo, float completed, int multiplier, float lifeBarPercent)
            {
                this.score = score;
                this.combo = combo;
                this.completed = completed;
                this.multiplier = multiplier;
                this.lifeBarPercent = lifeBarPercent;
            }
        }
        private void OnNoteHit()
        {
            Game_ScoreManager score = (Game_ScoreManager) ReflectionUtils.GetValue(GameControlManager.s_instance, "m_scoreManager");
            
            StageNoteHitEvent stageNoteHitEvent = new StageNoteHitEvent(
                score.Score,
                score.CurrentCombo,
                score.NotesCompleted,
                score.TotalMultiplier,
                LifeBarHelper.GetScalePercent()
            );
            
            Send("NoteHit", stageNoteHitEvent);
        }
        class StageNoteMissEvent
        {
            public float playTimeMS;

            public int multiplier;

            public float lifeBarPercent;

            public StageNoteMissEvent(float playTimeMS, int multiplier, float lifeBarPercent)
            {
                this.playTimeMS = playTimeMS;
                this.multiplier = multiplier;
                this.lifeBarPercent = lifeBarPercent;
            }
        }
        private void OnNoteFail()
        {
            StageNoteMissEvent missEvent = new StageNoteMissEvent(
                GameControlManager.s_instance.PlayTimeMS, 
                GameControlManager.s_instance.ScoreManager.TotalMultiplier,
                LifeBarHelper.GetScalePercent()
            );

            Send("NoteMiss", missEvent);
        }

        private void OnEnterSpecial()
        {
            Send("EnterSpecial", "{}");
        }

        private void OnCompleteSpecial()
        {
            Send("CompleteSpecial", "{}");
        }

        private void OnFailSpecial()
        {
            Send("FailSpecial", "{}");
        }

        class OutputEvent
        {
            public string eventType;
            public object data;

            public OutputEvent(string eventType, object data)
            {
                this.eventType = eventType;
                this.data = data;
            }
        }
        public void Send(string eventName, object data)
        {
            if (Websocket.server == null) return;
            OutputEvent outputEvent = new OutputEvent(eventName, data);

            Websocket.Send(JsonConvert.SerializeObject(outputEvent));

            MelonLogger.Msg(JsonConvert.SerializeObject(outputEvent));
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

                protected override void OnError(ErrorEventArgs e)
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
