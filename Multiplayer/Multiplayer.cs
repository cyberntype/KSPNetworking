using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;
using UnityEngine;
using Lidgren.Network;
using System.Net;
using System.Threading;
using KerbalData;
using KerbalData.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Multiplayer
{
	public class MultiplayerEntry : KSP.Testing.UnitTest
	{
		public MultiplayerEntry()
		{
			var gameobject = new GameObject ("Multiplayer", typeof(Multiplayer));
			UnityEngine.Object.DontDestroyOnLoad (gameobject);
		}
	}

	public class Multiplayer : MonoBehaviour
	{
		enum MultiplayerGUIs
		{
			INITIAL_CONNECT,
			CHAT_WINDOW
		}
		public const int MAX_CHAT_SIZE = 512;

		private bool hostSave = false;
		private String playerName = "Player Name";
		private String ip = "ip address";
		private String port = "port";
		private String pass = "pass";
		private Rect windowRect;
		private GUILayoutOption[] guiOpts;
		private String windowName;
		private MultiplayerPeer client;
		private Thread clientWorker;
		private Vector2 scrollpos = new Vector2(0,0);
		private StorableObjects<SaveFile> saves;
		private bool[] comboBox;
		private string hostedSaveKey;
		private int logStart = 0;
		private string chatMessage;
		private KerbalData.KerbalData userData;
		public Queue<string> chatMessages;
		public Queue<MultiplayerTask> engineTasks;
		public readonly object _chatLock = new object();
		public readonly object _dataLock = new object();

		public void Start()
		{
			chatMessages = new Queue<string> (MAX_CHAT_SIZE);
			userData = new KerbalData.KerbalData(@"B:\ksp\KSP_WIN", null);
			saves = userData.Saves;
			comboBox = new bool[saves.Count];
			for( int ii=0; ii<comboBox.Length; ii++) {
				comboBox[ii] = false;
			}
			guiOpts = new GUILayoutOption[2];
			windowName = "Server connect";
			windowRect = new Rect (((Screen.width/2)+100) * 1.2f, (Screen.height/2)+100, 350, 200);
		}
		
		public void OnGUI()
		{
			GameScenes scene = HighLogic.LoadedScene;
			
			if (scene == GameScenes.LOADING || scene == GameScenes.SETTINGS 
			    || scene == GameScenes.SPLASHSCREEN || scene == GameScenes.CREDITS) {
				return;
			}
			GUI.skin = HighLogic.Skin;
			windowRect = GUILayout.Window (1, windowRect, drawGUI, windowName, GUILayout.MinWidth (250));
		}

		private void readEngineQueue() {
			lock (_dataLock) {
				MultiplayerTask task = engineTasks.Dequeue();

			}
		}

		private void addToLog(MultiplayerTask message) {
			lock (_chatLock) {
				while(chatMessages.Count >= MAX_CHAT_SIZE) {
					chatMessages.Dequeue();
				}
				//chatMessages.Enqueue(message);
			}
		}

		private void FixedUpdate() {
			readEngineQueue ();
		}

		private void Update() {
			readEngineQueue ();
		}

		private void drawGUI(int windowID)
		{
			GUIStyle mySty = new GUIStyle(GUI.skin.button);
			mySty.normal.textColor = mySty.focused.textColor = Color.white;
			mySty.hover.textColor = mySty.active.textColor = Color.cyan;
			mySty.onNormal.textColor = mySty.onFocused.textColor = mySty.onHover.textColor = mySty.onActive.textColor = Color.green;
			mySty.padding = new RectOffset(8, 8, 8, 8);
			GUILayout.BeginHorizontal();
			playerName = GUILayout.TextField (playerName, 30);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			ip = GUILayout.TextField(ip, GUILayout.Width (100));
			port = GUILayout.TextField(port, GUILayout.Width (30));
			pass = GUILayout.PasswordField (pass, '*', GUILayout.MinWidth(20));
			GUILayout.EndHorizontal();
			GUILayout.BeginVertical();
			hostSave = GUILayout.Toggle(hostSave, "Host Game");

			if (GUILayout.Button ("Connect!", GUILayout.Height (30))) {
				int intport;
				if(int.TryParse( port, out intport)) 
				{
					client = new MultiplayerPeer(15, playerName, this);
					IPAddress addr = new IPAddress(System.Text.Encoding.Unicode.GetBytes(ip));
					IPEndPoint end = new IPEndPoint(addr, intport);
					client.sendDiscoveryRequest(end);
					clientWorker = new Thread(client.messageStream);
					clientWorker.Start();
				}
			}

			if (hostedSaveKey == null && hostSave && client != null && client.isConnected) {
				scrollpos = GUILayout.BeginScrollView (scrollpos);
				GUILayout.BeginVertical ();
				IEnumerable<string> keys = saves.Names;
				int ii = 0;
				foreach (string key in keys) {
					comboBox [ii] = GUILayout.Toggle (comboBox [ii], saves [key].Game.Title);
					if (comboBox [ii] && hostSave) {
						MultiplayerTask task = new MultiplayerTask ();

						hostedSaveKey = key;
						break;
					}
					ii++;
				}
				GUILayout.EndVertical ();
				GUILayout.EndScrollView ();
			} else {
				scrollpos = GUILayout.BeginScrollView (scrollpos);
				GUILayout.BeginVertical ();
				lock(client._logLock) {
					int len = client.logSize;
					for(int ii = logStart; ii < len; ii++) {
						GUILayout.Label(client.streamLog[ii]);
					}
				}
				foreach( string message in chatMessages) {
					GUILayout.Label(message);
				}
				GUILayout.EndVertical ();
				GUILayout.EndScrollView ();

				GUILayout.BeginHorizontal();
				chatMessage = GUILayout.TextField(chatMessage, GUILayout.Width(180));
				GUILayout.FlexibleSpace();
				if(GUILayout.Button("Send", GUILayout.Width(30))) {

					chatMessage = playerName + ": " + chatMessage; 
					lock(client._taskLock) {
						MultiplayerTask task = new MultiplayerTask();
						task.generalMessage = chatMessage;
					}
				}
				GUILayout.EndHorizontal();
			}
			GUI.DragWindow (new Rect (0, 0, 10000, 40));
		}

	}
}
