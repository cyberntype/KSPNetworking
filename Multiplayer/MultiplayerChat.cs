using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;
using UnityEngine;
using KSP.IO;

namespace Multiplayer {

	public class MultiplayerChat {
		public struct MultiplayerChatLine
		{
			public String message;
			public Color color;

			public MultiplayerChatLine(String message)
			{
				this.message = message;
				color = Color.white;
			}
		}
		private const int MAX_LINES = 256;
		private const int MAX_LINE_LENGTH = 512;
		private String chatMessage = "";
		private Vector2 scrollPos;
		private Queue<MultiplayerChatLine> chatLines;
		private String playerName = "";
		private NetworkView networkView;
		public MultiplayerChat(String playername, NetworkView view)
		{
			networkView = view;
			playerName = playername;
			chatLines = new Queue<MultiplayerChatLine> ();
		}

		public void addToChat(String line)
		{
			if (line.Length > MAX_LINE_LENGTH) {
				line = line.Substring(0, MAX_LINE_LENGTH);
			}
			MultiplayerChatLine chatLine = new MultiplayerChatLine (line);
			chatLines.Enqueue (chatLine);
			if (chatLines.Count > MAX_LINES) {
				chatLines.Dequeue();
			}
			scrollPos.y += 100;
		}

		public void chatWindow()
		{
			GUIStyle chatLineStyle = new GUIStyle(GUI.skin.button);

			chatLineStyle = new GUIStyle(GUI.skin.label);
			chatLineStyle.normal.textColor = Color.cyan;
			chatLineStyle.margin = new RectOffset(0, 0, 0, 0);
			chatLineStyle.padding = new RectOffset(0, 0, 0, 0);
			chatLineStyle.alignment = TextAnchor.LowerLeft;
			chatLineStyle.wordWrap = true;
			chatLineStyle.stretchWidth = true;
			chatLineStyle.fontStyle = FontStyle.Normal;
			if(Network.peerType == NetworkPeerType.Server || Network.peerType == NetworkPeerType.Client ){
				if(GUILayout.Button("Disconnect"))
				{
				}
			}
			scrollPos = GUILayout.BeginScrollView (scrollPos);
			GUILayout.BeginVertical();
			foreach (MultiplayerChatLine line in chatLines) {
				if(chatLineStyle.normal.textColor != line.color) {
					chatLineStyle.normal.textColor = line.color;
				}

				GUILayout.Label(line.message, chatLineStyle);
			}
			GUILayout.EndVertical ();
			GUILayout.EndScrollView ();
			GUILayout.BeginHorizontal ();

			chatMessage = GUILayout.TextField (chatMessage, MAX_LINE_LENGTH, chatLineStyle);
			if (chatMessage.Contains ('\n') || GUILayout.Button ("Send", GUILayout.Width(60))) {
				if(chatMessage.Contains('\n'))
				{
					chatMessage = chatMessage.Substring(0, chatMessage.Length - 1);
				}
				if (chatMessage.Length > MAX_LINE_LENGTH) {
					chatMessage = chatMessage.Substring(0, MAX_LINE_LENGTH);
				}
				chatMessage = playerName + ": " + chatMessage;
				networkView.RPC("addToChatWindow", RPCMode.All, chatMessage);
				chatMessage = "";
			}


			GUILayout.EndHorizontal ();

			GUI.DragWindow ();
		}

	}
}