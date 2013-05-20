using System.IO;
using System.Net;
using System.Dynamic;
using Newtonsoft.Json;

namespace Multiplayer
{
	public class UpdateTaskJSON
	{
		public Type type;
		public Player player;
		public PeerInfo peer;
		public Animation animation;


		public enum Type {
			PEER_INFO, SAVE_FILE, VESSEL, FLIGHTSTAATE
		}

		public class PeerInfo {
			public string ip;
			public int port;

			public PeerInfo(string addr, int prt)
			{
				ip = addr;
				port = prt;
			}
		}

		public class Player
		{
			public string playerName;
			public string playerColor;

			public Player(string pname, string pcolor)
			{
				playerName = pname;
				playerColor = pcolor;
			}
		}

		public class Animation
		{
			public string part;
			public string objectId;
			public string animationEvent;
			public bool runTillNextFrame;
			public string duration;
		}
	}
}
