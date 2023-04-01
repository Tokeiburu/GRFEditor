using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Utilities.Hash {
	public class Md5Hash : IHash {
		public Md5Hash() {
			Error = new byte[] {
				255, 255, 255, 255, 255, 255, 255, 255,
				255, 255, 255, 255, 255, 255, 255, 255
			};
		}

		#region IHash Members

		public string ComputeHash(byte[] data) {
			using (MD5 md5 = new MD5CryptoServiceProvider()) {
				byte[] ba = md5.ComputeHash(data);
				StringBuilder sb = new StringBuilder(ba.Length * 2);

				foreach (byte b in ba) {
					sb.AppendFormat("{0:x2}", b);
				}

				return sb.ToString();
			}
		}

		public byte[] ComputeByteHash(byte[] data) {
			using (MD5 md5 = new MD5CryptoServiceProvider()) {
				return md5.ComputeHash(data);
			}
		}

		public byte[] Error { get; private set; }

		public int HashLength { get { return 16; } }

		#endregion

		public override string ToString() {
			return "Md5";
		}

		public bool CompareData(byte[] data1, byte[] data2) {
			byte[] hash1 = ComputeByteHash(data1);
			byte[] hash2 = ComputeByteHash(data2);

			return Methods.ByteArrayCompare(hash1, hash2);
		}

		public static bool Compare(byte[] data1, byte[] data2) {
			Md5Hash hash = new Md5Hash();

			byte[] hash1 = hash.ComputeByteHash(data1);
			byte[] hash2 = hash.ComputeByteHash(data2);

			return Methods.ByteArrayCompare(hash1, hash2);
		}

		public static bool Compare(string file1, string file2) {
			if (!File.Exists(file1) || !File.Exists(file2))
				return false;

			var file1Info = new FileInfo(file1);
			var file2Info = new FileInfo(file2);

			if (file1Info.Length != file2Info.Length)
				return false;

			return Compare(File.ReadAllBytes(file1), File.ReadAllBytes(file2));
		}
	}
}
