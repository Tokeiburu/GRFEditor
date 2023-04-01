using System;

namespace Utilities.Hash {
	public interface IHash {
		int HashLength { get; }
		byte[] Error { get; }
		string ComputeHash(Byte[] data);
		byte[] ComputeByteHash(Byte[] data);
		//IHash Copy();
	}
}
