using System;

namespace Utilities.Hash {
	public class FileSizeHash : IHash {
		public FileSizeHash() {
			Error = Methods.StringToByteArray(String.Format("{0:x8}", 0xFFFFFFFF));
		}

		#region IHash Members

		public string ComputeHash(byte[] data) {
			return String.Format("{0:x8}", data.Length);
		}

		public byte[] ComputeByteHash(byte[] data) {
			return BitConverter.GetBytes(data.Length);// Methods.StringToByteArray(ComputeHash(data));
		}

		public int HashLength { get { return 4; } }

		public byte[] Error { get; private set; }

		#endregion

		public override string ToString() {
			return "File size";
		}
	}
}
