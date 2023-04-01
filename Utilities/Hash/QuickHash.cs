using System;
using System.Text;

namespace Utilities.Hash {
	public class QuickHash : IHash {
		private readonly IHash _baseHash;

		public QuickHash(IHash baseHash) {
			_baseHash = baseHash;
			Error = baseHash.Error;
		}

		#region IHash Members

		public string ComputeHash(byte[] data) {
			byte[] ba = _baseHash.ComputeByteHash(data);
			StringBuilder sb = new StringBuilder(ba.Length * 2);

			foreach (byte b in ba) {
				sb.AppendFormat("{0:x2}", b);
			}

			return sb.ToString();
		}

		public byte[] ComputeByteHash(byte[] data) {
			byte[] tdata = new byte[4096];

			if (data.Length >= tdata.Length) {
				Buffer.BlockCopy(data, 0, tdata, 0, 2048);
				Buffer.BlockCopy(data, 2048, tdata, tdata.Length - 2048, 2048);
				data = tdata;
			}

			return _baseHash.ComputeByteHash(data);
		}

		public byte[] Error { get; private set; }

		public int HashLength { get { return _baseHash.HashLength; } }

		#endregion

		public override string ToString() {
			return "Md5 Quick";
		}
	}
}
