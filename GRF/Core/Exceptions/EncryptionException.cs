using System;

namespace GRF.Core.Exceptions {
	public class EncryptionException : Exception {
		public readonly EncryptionExceptionReason Reason = EncryptionExceptionReason.Unknown;

		public EncryptionException(string message, EncryptionExceptionReason reason) : base(message) {
			Reason = reason;
		}
	}

	public enum EncryptionExceptionReason {
		Unknown,
		FailedToDecrypt,
		FailedToEncrypt,
		NoKeySet,
	}
}