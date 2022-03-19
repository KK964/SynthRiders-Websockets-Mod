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

namespace SynthRidersWebsockets
{
    public class WebsocketMod : MelonMod
    {
        public static WebsocketMod Instance;
        private static GameControlManager gameControlManager;

        public static MelonPreferences_Category connectionCategory;
        public override void OnApplicationStart() {
            Instance = this;
            connectionCategory = MelonPreferences.CreateCategory("Connection");
            string host = connectionCategory.CreateEntry<string>("Host", "localhost").Value;
            int port = connectionCategory.CreateEntry<int>("Port", 9000).Value;
            RuntimePatch.PatchAll();
            Websocket.Start($"{host}:{port}");
            LoggerInstance.Msg("[Websocket] Started Mod");
        }
        
        public override void OnApplicationQuit()
        {
            Websocket.Stop();
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
            public float length;
            public StageSongStartEvent(string song, string difficulty, string author, float length)
            {
                this.song = song;
                this.difficulty = difficulty;
                this.author = author;
                this.length = length;
            }
        }
        private void OnSongStart()
        {
            StageSongStartEvent stageSongStartEvent = new StageSongStartEvent(
                GameControlManager.s_instance.InfoProvider.TrackName,
                GameControlManager.s_instance.InfoProvider.CurrentDifficulty.ToString(),
                GameControlManager.s_instance.InfoProvider.Author,
                GameControlManager.CurrentTrackStatic.Song.clip.length);
            string songStart = JsonConvert.SerializeObject(stageSongStartEvent);
            Send("SongStart", songStart);
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
            string songEnd = JsonConvert.SerializeObject(stageSongEndEvent);
            Send("SongEnd", songEnd);
        }

        class StageNoteHitEvent
        {
            public int combo { get; set; }
            public float completed { get; set; }
            public StageNoteHitEvent(int combo, float completed)
            {
                this.combo = combo;
                this.completed = completed;
            }
        }
        private void OnNoteHit()
        {
            Game_ScoreManager score = (Game_ScoreManager) ReflectionUtils.GetValue(GameControlManager.s_instance, "m_scoreManager");
            StageNoteHitEvent stageNoteHitEvent = new StageNoteHitEvent(
                score.CurrentCombo,
                score.NotesCompleted);
            Send("NoteHit", JsonConvert.SerializeObject(stageNoteHitEvent));
        }

        private void OnNoteFail()
        {
            Send("NoteMiss", "{}");
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

        public void Send(string eventName, string data)
        {
            if (Websocket.server == null) return;
            Websocket.Send(eventName + " " + data);
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
