using System;
using Utilities.Tools;

namespace Utilities.Hash {
	public class Crc32EHash : IHash {
		public Crc32EHash() {
			Error = Methods.StringToByteArray(String.Format("{0:x8}", 0xFFFFFFFF));
		}

		#region IHash Members

		public string ComputeHash(byte[] data) {
			return String.Format("{0:x8}", Crc32.ComputeQuick(data));
		}

		public byte[] ComputeByteHash(byte[] data) {
			return BitConverter.GetBytes(Crc32.ComputeQuick(data));// Methods.StringToByteArray(ComputeHash(data));
		}

		public int HashLength { get { return 4; } }

		public byte[] Error { get; private set; }

		#endregion

		public override string ToString() {
			return "Crc32 Extended";
		}
	}
}
