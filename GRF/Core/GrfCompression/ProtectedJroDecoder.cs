using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;

namespace GRF.Core.GrfCompression {
	internal static class ProtectedJroDecoder {
		private const int HeaderSize = 104;
		private const string EnvelopeSha256 = "34c3ac066164d6f96bd88e9c3ce841e45791113b86896ffeb8b634ed9ab28e7b";
		private const string KeyBlockBase64 = "AwABAOTmn+ObjNB5quAufI/88vSozPqxqUvSyq5fdHyUX4+Aq+Hi+P60PiLf6JZYA4lQMtR2mjVRdBnt/wv6lwEAAAD7BDxjLGCpM+QADflN13L0xueJ4LMgd7ABXjgSPRnpY7HTBXh0mQ1ialhL4KmQy48cQ29Tw8HIj7kHO9ECAAAAT3rvtbuuYWdSlqpGbS9dfPtqec/uRgSBrrIwZ4dKlItvNrfMdiDiD8OWDv0piz7KuguP52Jt0rKT3O4X";

		public static byte[] DecodeHeader241(byte[] data) {
			if (data == null) throw new ArgumentNullException(nameof(data));
			if (data.Length < HeaderSize) throw new InvalidDataException("Truncated GPakEx header.");
			if (data[0] > 1) throw new NotSupportedException("Unsupported GPakEx header version " + data[0].ToString() + ".");
			if (data[1] != 241) throw new ArgumentException("Expected GPakEx header cipher 241.", nameof(data));
			
			byte[] cipherParameters = { 0x56, 0xFD, 0x7E, 0x71, 0x2F, 0x6D, 0x36, 0xF5, 0x5C, 0xA5, 0x99, 0xE6, 0x7B, 0x18, 0x10, 0x7A, 0x4C, 0xE8, 0xA1, 0xAE, 0x45, 0xB3, 0x21, 0x60 };
			byte[] header = Slice(data, 0, HeaderSize);
			byte[] key = Slice(cipherParameters, 0, 16);
			byte[] iv = Slice(cipherParameters, 16, 8);
			byte[] decryptedHeader = new byte[96];
			
			CbcBlockCipher cipher = new CbcBlockCipher(new IdeaEngine());
			cipher.Init(false, new ParametersWithIV(new KeyParameter(key), iv));
				
			for (int offset = 0; offset < decryptedHeader.Length; offset += 8)
				cipher.ProcessBlock(data, 8 + offset, decryptedHeader, offset);

			if (XxHash32.HashToUInt32(decryptedHeader) != ReadUInt32(header, 4))
				throw new InvalidDataException("GPakEx header XXH32 checksum mismatch.");
				
			Buffer.BlockCopy(decryptedHeader, 0, header, 8, decryptedHeader.Length);
			return header;
		}

		public static byte[] DecodeBody(byte[] payload, byte[] header, byte[] filename, ushort algorithm, IDictionary<uint, byte[]> keys) {
			if (payload == null) throw new ArgumentNullException(nameof(payload));
			if (header == null) throw new ArgumentNullException(nameof(header));
			if (filename == null) throw new ArgumentNullException(nameof(filename));
			if (header.Length < HeaderSize) throw new InvalidDataException("Truncated GPakEx header.");
			if (algorithm != 259 && algorithm != 260 && algorithm != 269) throw new NotSupportedException("Unsupported jRO GPakEx body cipher ID " + algorithm.ToString() + ".");
			if (payload.Length <= 128)
				return (byte[])payload.Clone();

			uint key = ReadUInt16(header, 40);
			byte[] material;
			
			if (key == 0) {
				material = Slice(header, 44, 60);
			}
			else {
				if (keys == null || !keys.TryGetValue(key, out material))
					throw new InvalidDataException("GPakEx body requires archive key ID " + key + ". Load its authenticated key block.");
				if (material == null || material.Length != 60)
					throw new InvalidDataException("Invalid GPakEx archive key material.");
				material = (byte[])material.Clone();
			}

			if (header[11] != 0) {
				for (int index = 0; index < material.Length; ++index)
					material[index] ^= header[24 + (index & 15)];
			}

			switch (algorithm) {
				case 259:
					return Decode259(payload, header, filename, material);
				case 260:
					return Decode260(payload, header, filename, material);
				default:
					return Decode269(payload, header, filename, material);
			}
		}

		internal static Dictionary<uint, byte[]> TryLoadArchiveKeys(byte[] envelope) {
			if (!JroArchiveProfiles.Contains(envelope))
				return null;

			byte[] decodedKeyBlock = Convert.FromBase64String(KeyBlockBase64);
			byte[] keyRecords;

			switch (ReadUInt32(envelope, 12)) {
				case 64:
					keyRecords = Slice(decodedKeyBlock, 64, 64);
					break;
				case 128:
					keyRecords = new byte[128];
					Buffer.BlockCopy(decodedKeyBlock, 128, keyRecords, 0, 64);
					Buffer.BlockCopy(decodedKeyBlock, 64, keyRecords, 64, 64);
					break;
				case 192:
					keyRecords = (byte[])decodedKeyBlock.Clone();
					break;
				default:
					throw new InvalidDataException("Unsupported JRO key-record size.");
			}

			if (XxHash32.HashToUInt32(keyRecords) != ReadUInt32(envelope, 16))
				throw new InvalidDataException("JRO archive key checksum mismatch.");

			Dictionary<uint, byte[]> keys = new Dictionary<uint, byte[]>();

			for (int offset = 0; offset < keyRecords.Length; offset += 64) {
				uint key = ReadUInt16(keyRecords, offset);
				
				if (key == 0 || keys.ContainsKey(key))
					throw new InvalidDataException("Invalid JRO key ID.");

				keys.Add(key, Slice(keyRecords, offset + 4, 60));
			}

			return keys;
		}

		private static byte[] Decode259(byte[] payload, byte[] header, byte[] filename, byte[] material) {
			byte[] output = (byte[])payload.Clone();
			int encryptedLength = Math.Min(1024, payload.Length - 128 & -8);
			TransformCfb(payload, output, 128, encryptedLength, material, 0, 32, true);
			byte[] mask = DeriveMask(material, header, filename, 50, true);
			XorMask(output, 128, output.Length - 128, mask);
			return output;
		}

		private static byte[] Decode260(byte[] payload, byte[] header, byte[] filename, byte[] material) {
			byte[] output = (byte[])payload.Clone();
			int bodyLength = payload.Length - 128;
			bool hasExtendedZone = bodyLength >= 3072;
			int cipherOffset = hasExtendedZone ? 2176 : 128;
			int encryptedLength = hasExtendedZone ? 1024 : bodyLength & ~7;
			TransformCfb(payload, output, cipherOffset, encryptedLength, material, 32, 0, false);
			byte[] mask = DeriveMask(material, header, filename, 48, false);
			XorMask(output, 128, output.Length - 128, mask);
			return output;
		}

		private static byte[] Decode269(byte[] payload, byte[] header, byte[] filename, byte[] material) {
			uint decodedLengthValue = ReadUInt32(header, 16);
			if (decodedLengthValue == uint.MaxValue || decodedLengthValue > int.MaxValue) throw new InvalidDataException("Unsupported GPakEx body 269 decoded length.");
			int decodedLength = (int)decodedLengthValue;
			byte[] mask = DeriveMask(material, header, filename, 50, false);

			if (decodedLength <= 2176) {
				if (decodedLength != payload.Length)
					throw new InvalidDataException("GPakEx body 269 payload length mismatch.");

				byte[] output = (byte[])payload.Clone();
				XorMask(output, 128, output.Length - 128, mask);
				return output;
			}

			int encryptedLength = Math.Min(1024, decodedLength - 2176);
			int paddingLength = 8 - (encryptedLength & 7);
			int paddedEncryptedLength = encryptedLength + paddingLength;

			if (payload.Length != decodedLength + paddingLength || payload.Length < 2176 + paddedEncryptedLength)
				throw new InvalidDataException("GPakEx body 269 padded payload length mismatch.");

			byte[] decryptedWindow = TransformRc5Ofb(payload, 2176, paddedEncryptedLength, material);

			for (int index = encryptedLength; index < decryptedWindow.Length; ++index) {
				if (decryptedWindow[index] != paddingLength)
					throw new InvalidDataException("GPakEx body 269 PKCS7 padding mismatch.");
			}

			byte[] outputBuffer = new byte[decodedLength];
			Buffer.BlockCopy(payload, 0, outputBuffer, 0, 2176);
			XorMask(outputBuffer, 128, 2048, mask);
			Buffer.BlockCopy(decryptedWindow, 0, outputBuffer, 2176, encryptedLength);
			int trailingOffset = 2176 + encryptedLength;
			int trailingLength = decodedLength - trailingOffset;
			Buffer.BlockCopy(payload, 2176 + paddedEncryptedLength, outputBuffer, trailingOffset, trailingLength);
			XorMask(outputBuffer, trailingOffset, trailingLength, mask);
			return outputBuffer;
		}

		private static byte[] DeriveMask(byte[] material, byte[] header, byte[] filename, int ivOffset, bool checksumAsKey) {
			byte[] filenameHash = NativeHash128(filename);
			byte[] headerHash = Slice(header, 24, 16);
			byte[] iv = Slice(material, ivOffset, 8);
			byte[] mask = new byte[16];

			CbcBlockCipher cipher = new CbcBlockCipher(new IdeaEngine());
			cipher.Init(true, new ParametersWithIV(new KeyParameter(checksumAsKey ? headerHash : filenameHash), iv));
			byte[] input = checksumAsKey ? filenameHash : headerHash;
			cipher.ProcessBlock(input, 0, mask, 0);
			cipher.ProcessBlock(input, 8, mask, 8);
			return mask;
		}

		private static void TransformCfb(byte[] input, byte[] output, int offset, int count, byte[] material, int keyAt, int ivOffset, bool xtea) {
			uint[] key = new uint[4];
			byte[] feedback = Slice(material, ivOffset, 8);
			byte[] encryptedFeedback = new byte[8];

			for (int index = 0; index < key.Length; ++index)
				key[index] = ReadBigEndian(material, keyAt + index * 4);

			for (int srcOffset = offset; srcOffset < offset + count; srcOffset += 8) {
				EncryptTeaBlock(key, feedback, encryptedFeedback, xtea);

				for (int index = 0; index < 8; ++index)
					output[srcOffset + index] = (byte)(input[srcOffset + index] ^ (uint)encryptedFeedback[index]);

				Buffer.BlockCopy(input, srcOffset, feedback, 0, 8);
			}
		}

		private static void EncryptTeaBlock(uint[] key, byte[] input, byte[] output, bool xtea) {
			uint left = ReadBigEndian(input, 0);
			uint right = ReadBigEndian(input, 4);
			uint sum = 0;

			for (int index = 0; index < 16; ++index) {
				if (xtea) {
					left += (uint)(((int)right << 4 ^ (int)(right >> 5)) + (int)right ^ (int)sum + (int)key[(int)sum & 3]);
					sum += 2654435769U;
					right += (uint)(((int)left << 4 ^ (int)(left >> 5)) + (int)left ^ (int)sum + (int)key[(int)(sum >> 11) & 3]);
				}
				else {
					sum += 2654435769U;
					left += (uint)(((int)right << 4) + (int)key[0] ^ (int)right + (int)sum ^ (int)(right >> 5) + (int)key[1]);
					right += (uint)(((int)left << 4) + (int)key[2] ^ (int)left + (int)sum ^ (int)(left >> 5) + (int)key[3]);
				}
			}

			WriteBigEndian(output, 0, left);
			WriteBigEndian(output, 4, right);
		}

		private static byte[] TransformRc5Ofb(byte[] input, int offset, int length, byte[] material) {
			uint[] roundKeys = new uint[42];
			uint[] keyWords = new uint[4];
			byte[] feedback = Slice(material, 0, 8);
			byte[] output = new byte[length];

			for (int index = 0; index < 4; ++index)
				keyWords[index] = ReadUInt32(material, 16 + index * 4);

			roundKeys[0] = 3084996963U;

			for (int index = 1; index < roundKeys.Length; ++index)
				roundKeys[index] = roundKeys[index - 1] + 2654435769U;

			uint left = 0;
			uint right = 0;

			for (int index1 = 0; index1 < 126; ++index1) {
				int index2 = index1 % 42;
				int index3 = index1 % 4;
				left = roundKeys[index2] = RotateLeft(roundKeys[index2] + left + right, 3);
				right = keyWords[index3] = RotateLeft(keyWords[index3] + left + right, left + right);
			}

			for (int index4 = 0; index4 < length; index4 += 8) {
				uint count1 = ReadUInt32(feedback, 0) + roundKeys[0];
				uint count2 = ReadUInt32(feedback, 4) + roundKeys[1];

				for (int index5 = 1; index5 <= 20; ++index5) {
					count1 = RotateLeft(count1 ^ count2, count2) + roundKeys[index5 * 2];
					count2 = RotateLeft(count2 ^ count1, count1) + roundKeys[index5 * 2 + 1];
				}

				WriteUInt32(feedback, 0, count1);
				WriteUInt32(feedback, 4, count2);

				for (int index6 = 0; index6 < 8; ++index6)
					output[index4 + index6] = (byte)(input[offset + index4 + index6] ^ (uint)feedback[index6]);
			}

			return output;
		}

		private static uint RotateLeft(uint value, uint count) {
			int shift = (int)count & 31;
			return value << shift | value >> 32 - shift;
		}

		private static void XorMask(byte[] data, int offset, int count, byte[] mask) {
			for (int i = 0; i < count; i++)
				data[offset + i] ^= mask[i & 15];
		}

		private static byte[] NativeHash128(byte[] data) {
			byte[] hash = XxHash128.Hash(data);
			Array.Reverse(hash);
			return hash;
		}

		internal static byte[] Slice(byte[] data, int offset, int length) {
			byte[] result = new byte[length];
			Buffer.BlockCopy(data, offset, result, 0, length);
			return result;
		}

		private static ushort ReadUInt16(byte[] data, int offset) => (ushort)(data[offset] | (uint)data[offset + 1] << 8);

		private static uint ReadUInt32(byte[] data, int offset) => (uint)(data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24);

		private static uint ReadBigEndian(byte[] data, int offset) => (uint)(data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8) | data[offset + 3];

		private static void WriteUInt32(byte[] data, int offset, uint value) {
			data[offset] = (byte)value;
			data[offset + 1] = (byte)(value >> 8);
			data[offset + 2] = (byte)(value >> 16);
			data[offset + 3] = (byte)(value >> 24);
		}

		private static void WriteBigEndian(byte[] data, int offset, uint value) {
			data[offset] = (byte)(value >> 24);
			data[offset + 1] = (byte)(value >> 16);
			data[offset + 2] = (byte)(value >> 8);
			data[offset + 3] = (byte)value;
		}
	}
}
