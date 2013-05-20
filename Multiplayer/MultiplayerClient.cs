using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Dynamic;
using UnityEngine;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Lidgren.Network;
using System.Diagnostics;
using KerbalData.Models;
using System.Text.RegularExpressions;
using FileSynch;

namespace Multiplayer {

	public enum MessageType { File=1, FileUpdate, State };

	public class MultiplayerTask 
	{
		public Peer peer;
		public String generalMessage;

		public class Peer 
		{
			public string playerName;
			public string ip;
			public int port;
			public bool connect;
		}
	}
	
	public class MultiplayerPeer 
	{		
		private const int MAX_LOG_LENGTH = 256;
		private const int MAX_LOG_LOSE = 32;
		public volatile bool isConnected;
		public Queue<MultiplayerTask> sendTasks;
		public string[] streamLog;
		public int logSize;
		public int networkRate;
		public volatile bool isActive;
		public readonly object _logLock = new object();
		public readonly object _taskLock = new object();
		private Stopwatch watch;
		private NetPeerConfiguration config;
		private NetPeer peer;
		private String playerName;
		private Multiplayer user;
		private string taskSplit;

		private enum MessageChannel{ FILE=1, UPDATE, CHAT, VESSEL };
		
		public MultiplayerPeer(int netrate, String pname, Multiplayer player) {
			playerName = pname;
			sendTasks = new Queue<MultiplayerTask>();
			streamLog = new string[MAX_LOG_LENGTH];
			networkRate = netrate;
			
			config = new NetPeerConfiguration("multiplayer");
			config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
			config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
			config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
			config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
			config.EnableMessageType(NetIncomingMessageType.StatusChanged);
			config.LocalAddress = NetUtility.Resolve("localhost");
			
			peer = new NetPeer(config);
			isActive = true;
			user = player;
			taskSplit = "(?<=[\":'[0-9]]) ?};;{ ?(?=[\"'])";
		}
	

		private void addToLog(String message) {
			lock(user._chatLock) {
				while(user.chatMessages.Count >= Multiplayer.MAX_CHAT_SIZE) {
					user.chatMessages.Dequeue();
				}
				user.chatMessages.Enqueue(message);
			}
		}
		
		private NetOutgoingMessage createPeerInfo(IPEndPoint end, String pname) 
		{
			MultiplayerTask task = new MultiplayerTask();
			task.peer = new MultiplayerTask.Peer();
			task.peer.ip = System.Text.Encoding.Unicode.GetString(end.Address.GetAddressBytes());
			task.peer.port = end.Port;
			task.peer.playerName = pname;
			task.peer.connect = true;
			String serializedTask = JsonConvert.SerializeObject(task);
			NetOutgoingMessage msg = peer.CreateMessage();
			msg.Write(serializedTask);
			return msg;
		}

		private void sendSaveFile() {

		}
		
		public void sendDiscoveryRequest(IPEndPoint end)
		{
			peer.DiscoverKnownPeer(end);			
		}
		
		private MultiplayerTask createMultiplayerTask(String task)
		{
			MultiplayerTask clientTask = (MultiplayerTask)JsonConvert.DeserializeObject (task);
			return clientTask;
		}
		
		private bool isConnectionRequest(MultiplayerTask task)
		{
			if(task.peer != null && task.peer.connect) {
				return true;
			}
			return false;
		}
		
		private byte[] getIPAddress(String addr) {
			return System.Text.Encoding.Unicode.GetBytes(addr);
		}

		public void messageStream()
		{
			while( isActive ) 
			{
				if(watch == null) {
					watch = Stopwatch.StartNew();
				}else {
					watch.Reset();
				}
				
				NetIncomingMessage msg;
				while((msg = peer.ReadMessage()) != null) {
					switch(msg.MessageType) {
					case NetIncomingMessageType.DiscoveryRequest:
						addToLog("Discovery Request from ip: " + msg.SenderEndPoint.Address.ToString());
						NetOutgoingMessage resp = peer.CreateMessage();
						resp.Write(playerName);
						peer.SendDiscoveryResponse(resp, msg.SenderEndPoint);
							break;
					case NetIncomingMessageType.DiscoveryResponse:
						addToLog("Discovery Response from player: " + msg.ReadString());
						peer.Connect(msg.SenderEndPoint);
						isConnected = true;
						break;
					case NetIncomingMessageType.ConnectionApproval:
						msg.SenderConnection.Approve();
						NetOutgoingMessage send = this.createPeerInfo(msg.SenderEndPoint, "");
						break;
					case NetIncomingMessageType.Data:
						try{
							int messageType = (int)msg.ReadByte();
							if (((MessageType)messageType) == MessageType.File) {
								TransferLogic logic = TransferLogic.instantiateTransfer( TransferLogic.getFileInfo( msg ) );
								if( logic != null ) {
									byte[] contents = msg.ReadBytes(logic.Length);
									logic.Wr
								}
							}

						}catch(Exception e) {Console.WriteLine(e.Message); }

						break;
					case NetIncomingMessageType.UnconnectedData:
						break;
					case NetIncomingMessageType.StatusChanged:
						break;
					default:
						break;
					}
				}
				
				lock(_taskLock) {
					if(sendTasks.Count > 0) {
						MultiplayerTask send = sendTasks.Dequeue();
						send.peer = new MultiplayerTask.Peer();
						send.peer.playerName = this.playerName;
						send.peer.connect = false;
						NetOutgoingMessage snd = peer.CreateMessage();
						snd.Write(JsonConvert.SerializeObject(send));
					}
				}
				
				watch.Stop();
				long elapsed = watch.ElapsedMilliseconds;
				Thread.Sleep(1000/networkRate - (int)elapsed);
			}
		}
		
	}
}