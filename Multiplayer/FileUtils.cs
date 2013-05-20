using System;

namespace Multiplayer
{
	public class FileUtils
	{
		public const string SAVE_PATH = "saves/";

		public FileUtils ()
		{ }

		public static string getCraftFile(string gameFolder, string craftFile, bool vab) {
			return SAVE_PATH + gameFolder + "/ships/" + ((vab) ? "VAB/" : "SPH/" ) + craftFile; 
		}
	}
}

