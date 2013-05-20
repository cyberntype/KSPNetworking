using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using Lidgren.Network;
using DiffMatchPatch;
using System.Text;

namespace FileSynch
{
	public enum FileType { PERSISTENT=1, QUICKSAVE, VAB_CRAFT, SPH_CRAFT };

	public class TransferLogic
	{
		private const int BYTE_READ = 16;
		private const byte COUNT_DELIMITER = 58;
		private const byte DIFF_DELIMITER = 59;
		private const byte INFO_DELIMITER = 124;
		private const int FETCHSIZE = 15;
		public const int LOWESTBYTES = 4096;
		public const string PLUGINSAVE = "PluginData/KSPMultiplayer/";

		private string gameFileName;
		private string gameFolderName;
		private FileType fileType;
		private string fullPath;
		private string gamePath;
		private int length;

		public int Length { get; set; }

		public TransferLogic ( string filename, string gamefolder, FileType type, int len)
		{
			gameFileName = filename;
			gameFolderName = gamefolder;
			fileType = type;
			length = len;
			fullPath = PLUGINSAVE + gamefolder + "/" + filename;
			try{
				if(!System.IO.Directory.Exists( PLUGINSAVE + gamefolder )) {
					System.IO.Directory.CreateDirectory( PLUGINSAVE + gamefolder );
				}
				if(!System.IO.File.Exists( fullPath ) ) {
					System.IO.File.Create( fullPath );
				}
			}catch(Exception e) {
				Console.WriteLine(e.Message);
			}
		}

		private int compareFileDates(string a, string b) {
			int aStart = a.IndexOf ("_");
			int bStart = b.IndexOf ("_");
			string dateA = a.Substring (aStart + 1, a.Length - aStart - 1);
			string dateB = b.Substring (bStart + 1, b.Length - bStart - 1);
			dateA.Replace ("_", "");
			dateB.Replace ("_", "");
			DateTime thenA = DateTime.ParseExact (dateA, "yyyyMMddHHmm", CultureInfo.InvariantCulture);
			DateTime thenB = DateTime.ParseExact (dateB, "yyyyMMddHHmm", CultureInfo.InvariantCulture);

			return thenA.CompareTo (thenB);
		}

		private List<string> getSaves( string path ) {
			string[] files  = Directory.GetFiles(path);
			List<string> saves = new List<string>();
			foreach( string name in files ) {
				if(name.IndexOf("persistent") > -1 ) {
					saves.Add (name);
				}
			}
			return saves;
		}

		private List<string> getOrderedSaves( string path ) {
			List<string> saves = getSaves (path);
			saves.Sort ( compareFileDates );
			return saves;
		}

		public string[] getGameSaves(int fetch) {
			try{
				string savepath = "saves/" + gameFileName + "/";
				List<string> saves = getOrderedSaves(savepath);
				string[] ret = new string[fetch];
				int ctr = 0;
				while ( saves.Count > 0 && ctr < fetch) {
					ret[ctr] = saves[ saves.Count - ctr ];
					ctr++;
				}

				return ret;
			} catch(Exception e) {
				Console.WriteLine (e.Message);
				return null;
			}
		}
		
		public string getGameSave(int index) {
			string[] saves = getGameSaves( index + 1 );
			return saves [index];
		}

		public bool readSaveUpdate( NetIncomingMessage msg ) {
			try {
				int index = (int)msg.ReadByte();
				string save = getGameSave(index);
				int messageCount = msg.ReadInt32();

				if( msg.ReadByte() == COUNT_DELIMITER ) {
					int ctr = 0;
					List<Diff> updates = new List<Diff>(messageCount);

					while( ctr < messageCount ) {
						Operation operation = (Operation)msg.ReadByte();
						int byteLength = msg.ReadInt32();
						string content;
						if(operation == Operation.INSERT) {
							msg.ReadByte();
							byte[] data = msg.ReadBytes ( byteLength );
							content = Encoding.UTF8.GetString(data);
						} else {
							content = "";
						}
						msg.ReadByte();
						Diff update = new Diff((Operation) operation, content);
						update.length = byteLength;
						updates.Add ( update );
						ctr++;
					}

					NetworkedFileUpdate.writeFromUpdates( updates, save );
				}


			} catch(Exception e) {
				Console.WriteLine (e.Message);
				return false;
			}
			return true;
		}

		public bool startSaveUpdate( string[] clientSaves, NetOutgoingMessage msg) {
			try {
				string savepath = "saves/" + gameFileName + "/";
				List<string> saves = getSaves(savepath);
				int find = -1;
				int ctr = 0;
				foreach( string save in clientSaves ) {
					find = saves.BinarySearch(save);
					if( find >= 0 ) {
						NetworkedFileUpdate updateLogic = new NetworkedFileUpdate(save, saves[saves.Count - 1]);
						List<Diff> diffs = updateLogic.updateFile();
						msg.Write( (byte)ctr );
						msg.Write( diffs.Count );
						msg.Write( COUNT_DELIMITER );

						foreach( Diff diff in diffs ) {
							msg.Write( (byte)diff.operation );
							if ( diff.operation == Operation.INSERT ) {
								msg.Write( diff.contents.Length );
								msg.Write( DIFF_DELIMITER );
								msg.Write( diff.contents, 0, diff.contents.Length );
							}else {
								msg.Write( diff.length );
							}
							msg.Write( DIFF_DELIMITER );
						}

						break;
					}
					ctr++;
				}

				//we found an update
				if( find > -1 ) {
					return true;
				}

				FileStream stream = new FileStream( saves[ saves.Count - 1], FileMode.Open, FileAccess.Read);
				byte[] contents = new byte[ stream.Length ];
				stream.Read(contents, 0, contents.Length);
				msg.Write(contents, 0, contents.Length);
				stream.Close();
				return true;
			}catch(Exception e) {
				Console.WriteLine (e);
			}
			return false;
		}

		public void writeFile(byte[] data) {
			try {
				FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Write);
				stream.Write(data, 0, data.Length);
				stream.Flush();
				stream.Close();
			} catch( IOException e) {
				Console.WriteLine (e.Message);
			}
		}

		public void copyFromGameFile() {
			try {
				string savepath = "saves/" + gameFolderName + "/";
				if(fileType == FileType.PERSISTENT || fileType == FileType.QUICKSAVE) {
					savepath += gameFileName;
				} else if( fileType == FileType.SPH_CRAFT ) {
					savepath += "SPH/" + gameFileName;
				} else if( fileType == FileType.VAB_CRAFT ) {
					savepath += "VAB/" + gameFileName;
				}
				System.IO.File.Copy(fullPath, savepath);
			} catch(Exception e) {
				Console.Write (e.Message);
			}
		}
		
		public void copyToGameFile() {
			try {
				string savepath = "saves/" + gameFolderName + "/";
				if(fileType == FileType.PERSISTENT || fileType == FileType.QUICKSAVE) {
					savepath += gameFileName;
				} else if( fileType == FileType.SPH_CRAFT ) {
					savepath += "SPH/" + gameFileName;
				} else if( fileType == FileType.VAB_CRAFT ) {
					savepath += "VAB/" + gameFileName;
				}
				System.IO.File.Copy(fullPath, savepath);
			} catch(Exception e) {
				Console.Write (e.Message);
			}
		}

		public static void copyFromGameFile( string gameFolder ) {
			try {
				string path = "saves/" + gameFolder +"/persistent.sfs";
				string newPath = path + DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm");
				string pluginPath = PLUGINSAVE + gameFolder +"/";
				if(!Directory.Exists(pluginPath)) {
					Directory.CreateDirectory(pluginPath);
				}
				File.Copy(path, pluginPath + newPath);
			} catch(Exception e) {
				Console.WriteLine (e);
			}
		}

		private static string getNextPart( NetIncomingMessage msg) {
			string part = String.Empty;
			string convert = String.Empty;
			int bytesRead = 0;
			int byteChunk = 0;
			bool eof = false;
			char delim = (char) INFO_DELIMITER;
			byte[] contents = new byte[BYTE_READ];
			while (!eof) {
				if( bytesRead + BYTE_READ >= msg.LengthBytes ) {
					byteChunk = 0;
					if( msg.LengthBytes - bytesRead > 0 ) {
						byteChunk = msg.LengthBytes - bytesRead;
						contents = msg.PeekBytes (msg.LengthBytes - bytesRead);
					}
					eof = true;
				} else {
					byteChunk = BYTE_READ;
					contents = msg.PeekBytes (BYTE_READ);
				}
				if(contents != null) {
					string temp = Encoding.UTF8.GetString(contents);
					int ii = temp.IndexOf(delim);
					if( ii > -1) {
						msg.ReadBytes( ii + 1);
						convert += temp.Substring(0, ii);
						return convert;
					}
					convert += temp;
				}

				if(byteChunk > 0 ) { //advance cursor
					msg.ReadBytes( byteChunk );
				}
				bytesRead += byteChunk;
				contents = null;
			}
			return part;
		}

		// [0] = fileName
		// [1] = folderName
		// [2] = byteLength or diffCount
		public static string[] getFileInfo( NetIncomingMessage msg ) {
			string[] parts = new string[3];

			parts[0] = getNextPart (msg);
			if (!parts[0].Equals (string.Empty)) {
				parts[1] = getNextPart (msg);
				if(!parts[1].Equals(string.Empty)) {
					parts[2] = getNextPart (msg);
					if(!parts[2].Equals(string.Empty)) {
						return parts;
					}
				}
			}

			return null;
		}
		
		public static void createGameStructure( string gameFolder ) {
			try {
				string gameBase = "saves/" + gameFolder;
				string ships = gameBase + "/Ships";
				if( !Directory.Exists ( gameBase ) ) {
					Directory.CreateDirectory(gameBase);
					Directory.CreateDirectory(ships);
					Directory.CreateDirectory(ships + "/VAB");
					Directory.CreateDirectory(ships + "/SPH");
				} else if( !Directory.Exists ( ships ) ) {
					Directory.CreateDirectory(ships);
					Directory.CreateDirectory(ships + "/VAB");
					Directory.CreateDirectory(ships + "/SPH");
				} else {
					if( !Directory.Exists ( ships +"/VAB" ) ) {
						Directory.CreateDirectory(ships + "/VAB");
					}
					if( !Directory.Exists ( ships +"/SPH" ) ) {
						Directory.CreateDirectory(ships + "/SPH");
					}
				}
			} catch(Exception e) {
				Console.WriteLine (e.Message);
			}
		}

		public static TransferLogic instantiateTransfer( string[] parts ) {
			if (parts [0] == String.Empty || parts [1] == String.Empty) {
				return null;
			}
			string fileName = parts [0];
			string gameFolder = parts [1];
			int.TryParse (parts [2], length);

			FileType type;
			if (fileName.IndexOf ("SPH") > -1) {
				type = FileType.SPH_CRAFT;
				fileName = fileName.Substring(fileName.IndexOf("/") + 1 );
			} else if (fileName.IndexOf ("persistent") > -1) {
				type = FileType.PERSISTENT;
			} else if (fileName.IndexOf ("quicksave") > -1) {
				type = FileType.QUICKSAVE;
			} else {
				type = FileType.VAB_CRAFT;
				fileName = fileName.Substring(fileName.IndexOf("/") + 1 );
			}

			TransferLogic logic = new TransferLogic (fileName, gameFolder, length, type);
			TransferLogic.createGameStructure (gameFolder);
			return logic;
		}
	}
}

