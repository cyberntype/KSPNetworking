using System;
using System.IO;
using System.Collections.Generic;
using DiffMatchPatch;

namespace Multiplayer
{
	public class NetworkedFileUpdate
	{		
		private const string REWRITE = "rewrite";
		public string oldFile;
		private string newFile;
		public NetworkedFileUpdate (string oldf, string newf)
		{
			oldFile = oldf;
			newFile = newf;
		}

		public static bool writeFromUpdates(List<Diff> updates, string source) {
			try{
				File.Move(source, source+REWRITE);
				FileStream read = new FileStream(source+REWRITE, FileMode.Open, FileAccess.Read);
				FileStream stream = new FileStream(source, FileMode.Open, FileAccess.Write);
				int diffreadstart = 0;
				int diffstart = 0;
				byte[] transfer;
				foreach (Diff diff in updates) {
					if(diff.operation == Operation.DELETE) {
						diffreadstart += diff.length;
						continue;
					}

					stream.Seek( (long)(diffstart), SeekOrigin.Begin ); 

					if( diff.operation == Operation.INSERT ) {
						stream.Write(diff.contents, 0, diff.contents.Length);
						diffstart += diff.contents.Length;
					} else if (diff.operation == Operation.EQUAL ) {
						read.Seek(diffreadstart, SeekOrigin.Begin);
						transfer = new byte[ diff.contents.Length ];
						read.Read(transfer, 0, transfer.Length);
						stream.Write( transfer, 0, transfer.Length);
						diffreadstart += diff.length;
						diffstart += diff.length;
					}
				}
				stream.Flush();
				stream.Close();
				read.Close();

				File.Delete( source + REWRITE);
				transfer = null;
			} catch( Exception e) { Console.WriteLine(e.Message); return false;}
			return true;
		}

		/* Find differences in two files
		 * Pass length only if equal/delete operation
		 * Include text for insert operations
		 */
		public List<Diff> updateFile() {
			diff_match_patch dmp = new diff_match_patch ();
			List<Diff> updates = dmp.diff_main (oldFile, newFile, false);

			string merge = String.Empty;
			foreach( Diff diff in updates) {
				if(diff.operation != Operation.INSERT) {
					diff.length = System.Text.Encoding.UTF8.GetByteCount(diff.text);
					diff.text = string.Empty;
				} else {
					diff.contents = System.Text.Encoding.UTF8.GetBytes( diff.text );
					diff.length = 0; 
					diff.text = string.Empty;
				}
			}
			return updates;
		}
	}
}

