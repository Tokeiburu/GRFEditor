using System;
using Utilities.Tools;

namespace Utilities.Hash {
	public class Crc32Hash : IHash {
		public Crc32Hash() {
			Error = Methods.StringToByteArray(String.Format("{0:x8}", 0xFFFFFFFF));
		}

		#region IHash Members

		public string ComputeHash(byte[] data) {
			return String.Format("{0:x8}", Crc32.Compute(data));
		}

		public byte[] ComputeByteHash(byte[] data) {
			return BitConverter.GetBytes(Crc32.Compute(data));
		}

		public IHash Copy() {
			return new Crc32Hash();
		}

		public int HashLength { get { return 4; } }

		public byte[] Error { get; private set; }

		#endregion

		public override string ToString() {
			return "Crc32";
		}
	}
}
