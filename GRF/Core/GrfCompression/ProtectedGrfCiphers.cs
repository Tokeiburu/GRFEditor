using System;
using System.Security.Cryptography;

namespace GRF.Core.GrfCompression {
	// CHAM uses 80 rounds and big-endian words. ThreeWay uses 11 rounds and
	// follows Crypto++'s public-domain implementation. IDEA and RC6 only encrypt
	// blocks here: the GRF format uses their output as XOR masks and CFB streams.
	internal static class ProtectedGrfCiphers {
		internal static void AesCbcDecrypt(byte[] data, int offset, int length, byte[] key, byte[] iv) {
			if (length % 16 != 0) throw ProtectedGrfDecoder.Invalid("Incomplete protected GRF AES block.");
			if (length == 0) return;
			using (var aes = Aes.Create()) {
				aes.Mode = CipherMode.CBC;
				aes.Padding = PaddingMode.None;
				using (var decryptor = aes.CreateDecryptor(key, iv)) {
					byte[] decoded = decryptor.TransformFinalBlock(data, offset, length);
					Buffer.BlockCopy(decoded, 0, data, offset, length);
				}
			}
		}

		internal static void ChamCbcDecrypt(byte[] data, int offset, int length, byte[] key, byte[] iv) {
			if (length % 16 != 0) throw ProtectedGrfDecoder.Invalid("Incomplete protected GRF CHAM block.");
			unchecked {
				uint[] keys = new uint[8];
				for (int i = 0; i < 4; i++) {
					uint value = BigWord(key, i * 4);
					keys[i] = value ^ RotateLeft(value, 1) ^ RotateLeft(value, 8);
					keys[(i + 4) ^ 1] = value ^ RotateLeft(value, 1) ^ RotateLeft(value, 11);
				}
				byte[] previous = (byte[])iv.Clone(), encrypted = new byte[16];
				uint[] words = new uint[4];
				for (int position = offset; position < offset + length; position += 16) {
					Buffer.BlockCopy(data, position, encrypted, 0, 16);
					for (int i = 0; i < 4; i++) words[i] = BigWord(encrypted, i * 4);
					for (int round = 79; round >= 0; round--) {
						int i = round % 4;
						words[i] = (RotateRight(words[i], (round & 1) != 0 ? 1 : 8) -
							(RotateLeft(words[(i + 1) % 4], (round & 1) != 0 ? 8 : 1) ^ keys[round % 8])) ^ (uint)round;
					}
					for (int i = 0; i < 4; i++) WriteBigWord(data, position + i * 4, words[i]);
					for (int i = 0; i < 16; i++) data[position + i] ^= previous[i];
					Buffer.BlockCopy(encrypted, 0, previous, 0, 16);
				}
			}
		}

		private static void ThreeWayTheta(ref uint a, ref uint b, ref uint c) {
			uint mix = RotateLeft(a ^ b ^ c, 16) ^ RotateLeft(a ^ b ^ c, 8);
			uint u = (a << 24) ^ (c >> 8) ^ (b << 8) ^ (a >> 24);
			uint v = (b << 24) ^ (a >> 8) ^ (c << 8) ^ (b >> 24);
			a ^= mix ^ u;
			b ^= mix ^ v;
			c ^= mix ^ (u >> 16) ^ (v << 16);
		}

		private static byte[] ThreeWayEncrypt(byte[] key, byte[] block) {
			uint k0 = BigWord(key, 0), k1 = BigWord(key, 4), k2 = BigWord(key, 8);
			uint a = BigWord(block, 0), b = BigWord(block, 4), c = BigWord(block, 8), rc = 0x0b0b;
			for (int round = 0; round < 11; round++) {
				a ^= k0 ^ (rc << 16); b ^= k1; c ^= k2 ^ rc;
				ThreeWayTheta(ref a, ref b, ref c);
				uint u = RotateLeft(a, 22), v = RotateLeft(c, 1);
				a = RotateLeft(u ^ (b | ~v), 1);
				c = RotateLeft(v ^ (u | ~b), 22);
				b ^= v | ~u;
				rc <<= 1;
				if ((rc & 0x10000) != 0) rc ^= 0x11011;
			}
			a ^= k0 ^ (rc << 16); b ^= k1; c ^= k2 ^ rc;
			ThreeWayTheta(ref a, ref b, ref c);
			byte[] result = new byte[12];
			WriteBigWord(result, 0, a); WriteBigWord(result, 4, b); WriteBigWord(result, 8, c);
			return result;
		}

		internal static void ThreeWayCfbDecrypt(byte[] data, int offset, int length, byte[] key, byte[] iv) {
			if (length % 12 != 0) throw ProtectedGrfDecoder.Invalid("Incomplete protected GRF ThreeWay block.");
			byte[] previous = (byte[])iv.Clone(), encrypted = new byte[12];
			for (int position = offset; position < offset + length; position += 12) {
				Buffer.BlockCopy(data, position, encrypted, 0, 12);
				byte[] stream = ThreeWayEncrypt(key, previous);
				for (int i = 0; i < 12; i++) data[position + i] ^= stream[i];
				Buffer.BlockCopy(encrypted, 0, previous, 0, 12);
			}
		}

		private static ushort IdeaMultiply(ushort a, ushort b) {
			ulong left = a == 0 ? 65536UL : a, right = b == 0 ? 65536UL : b;
			return unchecked((ushort)(left * right % 65537));
		}

		private static ushort[] IdeaKeys(byte[] key) {
			byte[] value = (byte[])key.Clone();
			ushort[] keys = new ushort[52];
			for (int i = 0; i < keys.Length; i++) {
				if (i > 0 && i % 8 == 0) {
					// Rotate the 128-bit big-endian key left by 25 without requiring UInt128.
					byte[] rotated = new byte[16];
					for (int j = 0; j < 16; j++) rotated[j] = unchecked((byte)((value[(j + 3) % 16] << 1) | (value[(j + 4) % 16] >> 7)));
					value = rotated;
				}
				int offset = i % 8 * 2;
				keys[i] = (ushort)((value[offset] << 8) | value[offset + 1]);
			}
			return keys;
		}

		private static byte[] IdeaEncrypt(byte[] block, ushort[] keys) {
			unchecked {
				ushort a = (ushort)((block[0] << 8) | block[1]), b = (ushort)((block[2] << 8) | block[3]);
				ushort c = (ushort)((block[4] << 8) | block[5]), d = (ushort)((block[6] << 8) | block[7]);
				for (int i = 0; i < 48; i += 6) {
					a = IdeaMultiply(a, keys[i]); b += keys[i + 1]; c += keys[i + 2]; d = IdeaMultiply(d, keys[i + 3]);
					ushort e = IdeaMultiply((ushort)(a ^ c), keys[i + 4]);
					ushort f = IdeaMultiply((ushort)((b ^ d) + e), keys[i + 5]), g = (ushort)(e + f);
					ushort oldB = b;
					a ^= f; b = (ushort)(c ^ f); c = (ushort)(oldB ^ g); d ^= g;
				}
				ushort[] words = { IdeaMultiply(a, keys[48]), (ushort)(c + keys[49]), (ushort)(b + keys[50]), IdeaMultiply(d, keys[51]) };
				byte[] result = new byte[8];
				for (int i = 0; i < 4; i++) { result[i * 2] = (byte)(words[i] >> 8); result[i * 2 + 1] = (byte)words[i]; }
				return result;
			}
		}

		private static byte[] IdeaMask(byte[] key, byte[] input, byte[] iv) {
			ushort[] keys = IdeaKeys(key);
			byte[] block = new byte[8];
			for (int i = 0; i < 8; i++) block[i] = (byte)(input[i] ^ iv[i]);
			byte[] first = IdeaEncrypt(block, keys);
			for (int i = 0; i < 8; i++) block[i] = (byte)(input[8 + i] ^ first[i]);
			byte[] second = IdeaEncrypt(block, keys), mask = new byte[16];
			Buffer.BlockCopy(first, 0, mask, 0, 8); Buffer.BlockCopy(second, 0, mask, 8, 8);
			return mask;
		}

		private static uint[] Rc6Keys(byte[] key) {
			unchecked {
				uint[] words = new uint[4], keys = new uint[44];
				for (int n = 0; n < 4; n++) words[n] = ProtectedGrfDecoder.Word(key, n * 4);
				for (int n = 0; n < 44; n++) keys[n] = 0xb7e15163U + (uint)n * 0x9e3779b9U;
				uint a = 0, b = 0;
				int i = 0, j = 0;
				for (int n = 0; n < 132; n++) {
					a = keys[i] = RotateLeft(keys[i] + a + b, 3);
					b = words[j] = RotateLeft(words[j] + a + b, (int)(a + b));
					i = (i + 1) % 44; j = (j + 1) % 4;
				}
				return keys;
			}
		}

		private static byte[] Rc6Encrypt(byte[] block, uint[] keys) {
			unchecked {
				uint a = ProtectedGrfDecoder.Word(block, 0), b = ProtectedGrfDecoder.Word(block, 4) + keys[0];
				uint c = ProtectedGrfDecoder.Word(block, 8), d = ProtectedGrfDecoder.Word(block, 12) + keys[1];
				for (int i = 1; i <= 20; i++) {
					uint t = RotateLeft(b * (b * 2 + 1), 5), u = RotateLeft(d * (d * 2 + 1), 5);
					a = RotateLeft(a ^ t, (int)u) + keys[2 * i]; c = RotateLeft(c ^ u, (int)t) + keys[2 * i + 1];
					uint oldA = a; a = b; b = c; c = d; d = oldA;
				}
				byte[] result = new byte[16];
				WriteLittleWord(result, 0, a + keys[42]); WriteLittleWord(result, 4, b);
				WriteLittleWord(result, 8, c + keys[43]); WriteLittleWord(result, 12, d);
				return result;
			}
		}

		internal static void DecryptBodyTail(int cipher, byte[] parameters, byte[] filenameHash, byte[] outputHash, byte[] data, int offset) {
			byte[] mask = cipher == 128 ? IdeaMask(outputHash, filenameHash, ProtectedGrfDecoder.Slice(parameters, 49, 8)) :
				IdeaMask(filenameHash, outputHash, ProtectedGrfDecoder.Slice(parameters, 49, 8));
			int length = data.Length - offset;
			if (cipher == 128) {
				int start = length < 3072 ? 0 : 2048, size = length < 3072 ? length / 16 * 16 : 1024;
				AesCbcDecrypt(data, offset + start, size, ProtectedGrfDecoder.Slice(parameters, 44, 16), ProtectedGrfDecoder.Slice(parameters, 28, 16));
			}
			else {
				uint[] keys = Rc6Keys(ProtectedGrfDecoder.Slice(parameters, 24, 16));
				byte[] previous = ProtectedGrfDecoder.Slice(parameters, 40, 16), encrypted = new byte[16];
				int size = Math.Min(length / 16 * 16, 1024);
				for (int position = offset; position < offset + size; position += 16) {
					Buffer.BlockCopy(data, position, encrypted, 0, 16);
					byte[] stream = Rc6Encrypt(previous, keys);
					for (int i = 0; i < 16; i++) data[position + i] ^= stream[i];
					Buffer.BlockCopy(encrypted, 0, previous, 0, 16);
				}
			}
			for (int i = 0; i < length; i++) data[offset + i] ^= mask[i % 16];
		}

		internal static void DecryptLegacyTail(int cipher, byte[] parameters, byte[] filenameHash, byte[] outputHash, byte[] data, int offset, int compressedLength) {
			int length = Math.Min(compressedLength, 1024), encryptedLength = (length / 16 + 1) * 16, tailLength = data.Length - offset;
			if (tailLength < encryptedLength || tailLength - encryptedLength != compressedLength - length)
				throw ProtectedGrfDecoder.Invalid("Protected GRF AES body padding size mismatch.");
			byte[] key, iv, mask;
			if (cipher == 5) {
				key = ProtectedGrfDecoder.Slice(parameters, 0, 16); iv = ProtectedGrfDecoder.Slice(parameters, 16, 16);
				mask = IdeaMask(filenameHash, outputHash, ProtectedGrfDecoder.Slice(parameters, 48, 8));
			}
			else {
				key = ProtectedGrfDecoder.Slice(parameters, 28, 16); iv = ProtectedGrfDecoder.Slice(parameters, 44, 16);
				mask = IdeaMask(outputHash, filenameHash, ProtectedGrfDecoder.Slice(parameters, 52, 8));
			}
			AesCbcDecrypt(data, offset, encryptedLength, key, iv);
			int padding = data[offset + encryptedLength - 1];
			if (padding < 1 || padding > 16 || encryptedLength - padding != length)
				throw ProtectedGrfDecoder.Invalid("Invalid protected GRF AES body PKCS#7 padding.");
			for (int i = length; i < encryptedLength; i++)
				if (data[offset + i] != padding) throw ProtectedGrfDecoder.Invalid("Invalid protected GRF AES body PKCS#7 padding.");
			Buffer.BlockCopy(data, offset + encryptedLength, data, offset + length, tailLength - encryptedLength);
			for (int i = 0; i < compressedLength - length; i++) data[offset + length + i] ^= mask[i % 16];
		}

		internal static void DecryptChamTail(byte[] parameters, byte[] seededNameHash, byte[] outputHash, byte[] data, int offset) {
			ulong seed = ProtectedGrfDecoder.Word(seededNameHash, 8) | (ulong)ProtectedGrfDecoder.Word(seededNameHash, 12) << 32;
			byte[] key = ProtectedGrfDecoder.NativeHash(ProtectedGrfDecoder.Slice(parameters, 0, 60), unchecked((long)seed));
			int length = data.Length - offset, fullLength = length / 16 * 16;
			ChamCbcDecrypt(data, offset, Math.Min(fullLength, 1024), key, outputHash);
			byte[] previous = (byte[])outputHash.Clone(), original = new byte[16];
			for (int position = offset; position < offset + fullLength; position += 16) {
				Buffer.BlockCopy(data, position, original, 0, 16);
				for (int i = 0; i < 16; i++) {
					int rotated = (i + 12) % 16;
					data[position + i] = (byte)(original[rotated] ^ seededNameHash[rotated] ^ previous[i]);
				}
				Buffer.BlockCopy(original, 0, previous, 0, 16);
			}
			for (int i = 0; i < length - fullLength; i++) data[offset + fullLength + i] ^= (byte)(seededNameHash[i] ^ previous[i]);
		}

		private static uint RotateLeft(uint value, int count) { return (value << (count & 31)) | (value >> ((32 - count) & 31)); }
		private static uint RotateRight(uint value, int count) { return RotateLeft(value, 32 - count); }
		private static uint BigWord(byte[] data, int offset) { return (uint)(data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3]); }
		private static void WriteBigWord(byte[] data, int offset, uint value) {
			unchecked { data[offset] = (byte)(value >> 24); data[offset + 1] = (byte)(value >> 16); data[offset + 2] = (byte)(value >> 8); data[offset + 3] = (byte)value; }
		}
		private static void WriteLittleWord(byte[] data, int offset, uint value) {
			unchecked { data[offset] = (byte)value; data[offset + 1] = (byte)(value >> 8); data[offset + 2] = (byte)(value >> 16); data[offset + 3] = (byte)(value >> 24); }
		}
	}
}
