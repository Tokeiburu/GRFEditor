using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Security.Cryptography;

namespace GRF.Core.GrfCompression {
	internal static class ProtectedCnDecoder {
		public const int HeaderSize = 168;
		private const string EnvelopeSha256 = "a09a4c27abdfbb473392e1e246b8def486ff820bdd51de7ad7206c279cc3b529";
		private const string EnvelopeSha256_2 = "2ef7f6c8fb08fa9d90b163de26c04a7323ec9231f59efcb7c783f1dee040053a";
		private const string ClearBlockBase64 = "AQAAALxm2JCPpxl7D5Ai7UJt3x/fz+/KRNDWyd7AZg+mAQ4Fcipwjcce+Gf7WWRdR3zEdIrzoF7Pv0JkawTpnOOdIacCAAAAvSoF4FuHJVpOnT/eOGmitkWP90kSIJz5YiM+YERVT2w0HK80l5wppmTULl7ro1Z0puMZPl7Omn3hGz1Z6NfoeA==";

		private static readonly byte[] PublicKey = { 0x0F, 0xF9, 0x96, 0x1A, 0x1B, 0x2C, 0xD5, 0x2E, 0x73, 0x5B, 0x33, 0xD7, 0x72, 0xC7, 0xE1, 0x41, 0x67, 0xFD, 0x4A, 0x71, 0x48, 0x5E, 0x9D, 0xBE, 0xEC, 0x8D, 0xC4, 0xB7, 0x01, 0xB8, 0x62, 0x30 };

		internal static void VerifyHeader(byte[] data, byte[] header) {
			ValidateV2(data);

			if (data.Length < HeaderSize) throw new InvalidDataException("Truncated GPakEx v2 signature.");
			if (header == null) throw new ArgumentNullException(nameof(header));

			Ed25519Signer signer = new Ed25519Signer();
			signer.Init(false, new Ed25519PublicKeyParameters(PublicKey, 0));
			signer.BlockUpdate(header, 0, header.Length);

			byte[] signature = Slice(data, ProtectedGrfDecoder.HeaderSize, 64);

			if (!signer.VerifySignature(signature))
				throw new InvalidDataException("GPakEx v2 header Ed25519 signature mismatch.");
		}

		public static byte[] DecodeBody9WithMaterial(byte[] payload, byte[] header, byte[] filename, byte[] material) {
			if (payload == null) throw new ArgumentNullException(nameof(payload));
			if (header == null || header.Length < ProtectedGrfDecoder.HeaderSize) throw new InvalidDataException("Truncated GPakEx resource header.");
			if (filename == null) throw new ArgumentNullException(nameof(filename));
			if (material == null || material.Length < 60) throw new InvalidDataException("Invalid GPakEx body 9 key material.");

			byte[] output = (byte[])payload.Clone();
			
			if (payload.Length <= 128)
				return output;

			int bodyLength = payload.Length - 128;
			bool hasExtendedZone = bodyLength >= 2048;
			int cipherOffset = hasExtendedZone ? 1152 : 128;
			int cipherLength = hasExtendedZone ? 1024 : bodyLength & -16;

			byte[] key = Slice(material, 16, 16);
			byte[] feedback = Slice(material, 44, 16);
			byte[] encryptedFeedback = new byte[16];

			RC6Engine cipher = new RC6Engine();
			cipher.Init(true, new KeyParameter(key));

			for (int srcOffset = cipherOffset; srcOffset < cipherOffset + cipherLength; srcOffset += 16) {
				cipher.ProcessBlock(feedback, 0, encryptedFeedback, 0);

				for (int index = 0; index < 16; ++index)
					output[srcOffset + index] = (byte)(payload[srcOffset + index] ^ (uint)encryptedFeedback[index]);

				Buffer.BlockCopy(payload, srcOffset, feedback, 0, 16);
			}

			byte[] mask = DeriveBody9Mask(material, header, filename);

			for (int index = 128; index < output.Length; ++index)
				output[index] ^= mask[index - 128 & 15];

			return output;
		}

		internal static Dictionary<uint, byte[]> TryLoadArchiveKeys(byte[] envelope) {
			if (envelope == null || envelope.Length != 156)
				return null;

			byte[] hash;
			using (SHA256 shA256 = SHA256.Create())
				hash = shA256.ComputeHash(envelope);

			string hashHex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
			
			if (hashHex != EnvelopeSha256 && 
				hashHex != EnvelopeSha256_2)
				return null;

			if (ReadUInt32(envelope, 0) != 258 || ReadUInt32(envelope, 12) != 136)
				throw new InvalidDataException("Invalid CN archive envelope.");

			byte[] keyData = Convert.FromBase64String(ClearBlockBase64);

			if (Crc32(keyData) != ReadUInt32(envelope, 16))
				throw new InvalidDataException("CN archive key checksum mismatch.");

			Dictionary<uint, byte[]> keys = new Dictionary<uint, byte[]>();

			for (int offset = 0; offset < keyData.Length; offset += 68) {
				uint key = ReadUInt32(keyData, offset);

				if (key == 0U || key > ushort.MaxValue || keys.ContainsKey(key))
					throw new InvalidDataException("Invalid CN key ID.");

				keys.Add(key, Slice(keyData, offset + 4, 64));
			}

			return keys;
		}

		private static void ValidateV2(byte[] data) {
			if (data == null) throw new ArgumentNullException(nameof(data));
			if (data.Length < ProtectedGrfDecoder.HeaderSize) throw new InvalidDataException("Truncated GPakEx version 2 header.");
			if (data[0] != 2) throw new ArgumentException("Expected GPakEx header version 2.", nameof(data));
			if (data[1] != 1 && data[1] != 2) throw new NotSupportedException("Unsupported GPakEx version 2 header cipher " + data[1] + ".");
		}

		private static byte[] DeriveBody9Mask(byte[] material, byte[] header, byte[] filename) {
			byte[] filenameHash = XxHash128.Hash(filename);
			Array.Reverse(filenameHash);
			byte[] iv = Slice(material, 50, 8);
			byte[] headerHash = Slice(header, 24, 16);
			byte[] mask = new byte[16];

			CbcBlockCipher cbcBlockCipher = new CbcBlockCipher(new IdeaEngine());
			cbcBlockCipher.Init(true, new ParametersWithIV(new KeyParameter(filenameHash), iv));
			cbcBlockCipher.ProcessBlock(headerHash, 0, mask, 0);
			cbcBlockCipher.ProcessBlock(headerHash, 8, mask, 8);
			return mask;
		}

		internal static byte[] Slice(byte[] data, int offset, int length) {
			byte[] result = new byte[length];
			Buffer.BlockCopy(data, offset, result, 0, length);
			return result;
		}

		private static uint ReadUInt32(byte[] input, int offset) => (uint)(input[offset] | input[offset + 1] << 8 | input[offset + 2] << 16 | input[offset + 3] << 24);

		private static uint Crc32(byte[] input) {
			uint crc = uint.MaxValue;
			
			foreach (byte num2 in input) {
				crc ^= num2;
				for (int index = 0; index < 8; ++index)
					crc = crc >> 1 ^ (((int)crc & 1) != 0 ? 3988292384U : 0U);
			}

			return crc ^ uint.MaxValue;
		}
	}
}
