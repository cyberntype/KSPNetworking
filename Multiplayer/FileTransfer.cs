using System;
using System.IO;

namespace Multiplayer
{
	public class FileTransfer
	{
		enum FileType { PERSISTENT, QUICKSAVE, VAB, SPH };

		private string fileName;
		private string gameFolder;
		private string vabPath;
		private string sphPath;
		private uint fileLength;
		private int progress;
		private FileStream writeStream;
		private FileStream readStream;

		private uint fileKBLength;
		private uint fileKBReceived;
		private byte[] fileContents;

		private int chunksTransferred = 0;

		public FileTransfer (string name, uint len, bool read)
		{
			fileName = name;
			fileLength = len;
			fileContents = new byte[ fileLength ];
		}

		public void writeBytes(byte[] contents) {

			if (writeStream == null) {
				try {
					writeStream = new FileStream(fileName, FileMode.Append, FileAccess.Write);
				} catch ( Exception e) { Console.WriteLine(e.Message); } ); 
			}

			try {
				writeStream.Write(contents, 0, contents.Length);
				writeStream.Flush();
				writeStream.Close();
			} catch(IOException e) {
				Console.Write (e.Message);
			}
		}

		public void copyFile(string destFile, bool overwrite) {
			System.IO.File.Copy (fileName, destFile, overwrite);
		}
		
		public void getFileContents() {
			if (readStream == null) {
				try {
					FileInfo info = new FileInfo(fileName);
					fileLength = (int)info.Length();
					readStream = new FileStream (fileName, FileMode.Open, FileAccess.Read);
				} catch (Exception e) {
					Console.WriteLine (e.Message);
				}
			}

			try{
				readStream.Read (fileContents, 0, fileLength);
				readStream.Close();
			} catch(IOException e) {
				Console.WriteLine (e.Message);
			}
		}
	

		private void createSaveStructure( ) {
			try{
				System.IO.Directory.CreateDirectory ("saves/" + gameFolder);
				System.IO.Directory.CreateDirectory ("saves/" + gameFolder + "/ships");
				vabPath = "saves/" + gameFolder + "/ships/VAB";
				sphPath = "saves/" + gameFolder + "/ships/SPH";
				System.IO.Directory.CreateDirectory (sphPath); 
				System.IO.Directory.CreateDirectory (vabPath);
			} catch (IOException e) {
				Console.WriteLine(e.Message);
			} catch (DirectoryNotFoundException f) {
				Console.WriteLine(f.Message);
			} catch (UnauthorizedAccessException g) {
				Console.WriteLine(g.Message);
			}
		}
	}
}

