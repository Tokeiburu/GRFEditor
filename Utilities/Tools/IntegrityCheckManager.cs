using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Utilities.Tools {
	/// <summary>
	/// ROUtilityTool library class
	/// Compares all the files in two different folders
	/// </summary>
	public class IntegrityCheckManager {
		public List<HashInfo> HashDataPath1 = new List<HashInfo>();
		public List<HashInfo> HashDataPath2 = new List<HashInfo>();

		private IntegrityCheckManager() {
		}

		/// <summary>
		/// Compares two folders
		/// </summary>
		/// <param name="path1">The path1.</param>
		/// <param name="path2">The path2.</param>
		/// <param name="resultPath">The output path for the result file.</param>
		public static void Compare(string path1, string path2, string resultPath) {
			File.WriteAllBytes(resultPath, Encoding.Default.GetBytes(Compare(path1, path2)));
		}

		/// <summary>
		/// Compares two folders
		/// </summary>
		/// <param name="path1">The path1.</param>
		/// <param name="path2">The path2.</param>
		/// <returns>The result data</returns>
		public static string Compare(string path1, string path2) {
			IntegrityCheckManager icm = new IntegrityCheckManager();
			icm.HashDataPath1 = icm.GetHashInfo(path1);
			icm.HashDataPath2 = icm.GetHashInfo(path2);

			List<HashInfo> result = icm.Compare();

			string output = "Comparing (only show differences)\nPath1 = " + path1 + "\nPath2 = " + path2 + "\n\n";

			if (result.Count == 0) {
				output += "All files were identical";
			}
			else {
				output += icm.Format(result);
			}

			return output;
		}

		/// <summary>
		/// Compares the two list of HashInfo
		/// </summary>
		/// <returns>The resulting HashInfo list containing all the differences</returns>
		public List<HashInfo> Compare() {
			List<HashInfo> hi = new List<HashInfo>();

			for (int i = 0; i < HashDataPath2.Count; i++) {
				HashInfo hashInfo2 = HashDataPath2[i];

				HashInfo hashInfo1 = HashDataPath1.Find(p => p.FileName == hashInfo2.FileName);

				if (hashInfo1 == null) {
					hi.Add(new HashInfo(hashInfo2.FileName, null, IntegrityStatus.FileShouldNotBeHere));
					continue;
				}

				if (hashInfo1.Hash != hashInfo2.Hash) {
					hi.Add(new HashInfo(hashInfo2.FileName, "h1=" + hashInfo1.Hash + "_h2=" + hashInfo2.Hash, IntegrityStatus.HashBad));
					HashDataPath1.Remove(hashInfo1);
				}
				else {
					HashDataPath1.Remove(hashInfo1);
				}
			}

			foreach (HashInfo hashInfo in HashDataPath1) {
				hi.Add(new HashInfo(hashInfo.FileName, hashInfo.Hash, IntegrityStatus.FileMissing));
			}

			return hi;
		}

		/// <summary>
		/// Gets the hash info list from a folder
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns></returns>
		public List<HashInfo> GetHashInfo(string path) {
			List<HashInfo> hi = new List<HashInfo>();

			string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
			foreach (string file in files) {
				string fileName = file.Replace(path, "");

				try {
					using (var md5 = new MD5CryptoServiceProvider()) {
						using (var stream = File.OpenRead(file)) {
							string hash = ConvertToString(md5.ComputeHash(stream));
							hi.Add(new HashInfo(fileName, hash, IntegrityStatus.Unknown));
						}
					}
				}
				catch { }
			}

			return hi;
		}

		/// <summary>
		/// Converts the md5 hash to an hex format
		/// </summary>
		/// <param name="data">The data.</param>
		/// <returns></returns>
		public string ConvertToString(byte[] data) {
			StringBuilder sBuilder = new StringBuilder();

			for (int i = 0; i < data.Length; i++) {
				sBuilder.Append(data[i].ToString("x2"));
			}

			return sBuilder.ToString();
		}

		/// <summary>
		/// Formats the hash data list in a string format
		/// </summary>
		/// <param name="hashData">The hash data.</param>
		/// <returns></returns>
		public string Format(List<HashInfo> hashData) {
			StringBuilder builder = new StringBuilder();

			foreach (HashInfo hashInfo in hashData) {
				builder.AppendLine(hashInfo.ToString());
			}

			return builder.ToString();
		}
	}

	public class HashInfo {
		public string FileName;
		public string Hash;
		public IntegrityStatus Status;

		public HashInfo(string fileName, string hash, IntegrityStatus status) {
			FileName = fileName;
			Hash = hash;
			Status = status;
		}

		public HashInfo(string data) {
			string[] temp = data.Split('\t');

			if (temp.Length == 2) {
				if (temp[0] == "" || temp[1] == "") {
					throw new Exception("Bad hash info");
				}

				FileName = temp[0];
				Hash = temp[1];
				Status = IntegrityStatus.Unknown;

			}
			else if (temp.Length == 3) {
				if (temp[0] == "" || temp[1] == "" || temp[2] == "") {
					throw new Exception("Bad hash info");
				}

				FileName = temp[0];
				Hash = temp[1];
				Status = (IntegrityStatus)Enum.Parse(typeof(IntegrityStatus), temp[2]);
			}
			else {
				throw new Exception("Bad hash info");
			}
		}

		public override string ToString() {
			return String.Format("{0}\t{1}\t{2}", FileName, Hash, Status);
		}
	}

	public enum IntegrityStatus {
		HashOk,
		HashBad,
		FileMissing,
		FileShouldNotBeHere,
		Unknown
	}
}
