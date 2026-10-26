using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Security.Cryptography;
using System.Text;
using ComponentAce.Compression.Libs.zlib;
using K4os.Compression.LZ4;

namespace GRF.Core.GrfCompression {
	// Protected GRF records authenticate the original filename bytes, not a decoded path.
	internal static class ProtectedGrfDecoder {
		private const int HeaderSize = 104;
		private const int MaxOutputSize = 256 * 1024 * 1024;
		private const int MaxPackedSize = 258 * 1024 * 1024;

		internal static Dictionary<uint, byte[]> DecodeArchiveKeys(byte[] raw) {
			if (raw == null) throw new ArgumentNullException(nameof(raw));
			var keys = new Dictionary<uint, byte[]>();
			if (raw.Length == 0) return keys;
			if (raw.Length < 20)
				throw Invalid("Unsupported or truncated GRF archive key block.");
			uint size = Word(raw, 12);
			if (size == 0 || size % 64 != 0 || size / 64 > 65535 || size != raw.Length - 20)
				throw Invalid("GRF archive key-block size mismatch.");
			byte[] digest;
			using (var sha = SHA256.Create()) digest = sha.ComputeHash(Encoding.ASCII.GetBytes("uf8E8qAwHthrLxiS"));
			byte[] decoded = Slice(raw, 20, (int)size);
			
			if (raw[0] != 1 || raw[1] != 1 || raw[2] != 0 || raw[3] != 0) {
				// Try using kRO default keys as a backup
				int keyCount = Math.Min(GrfHeader.KRODefaultProtectedKeys.Count, decoded.Length / 64);
				for (uint i = 0; i < keyCount; i++) {
					Buffer.BlockCopy(BitConverter.GetBytes(i + 1), 0, decoded, (int)(64 * i), 4);
					Buffer.BlockCopy(GrfHeader.KRODefaultProtectedKeys[i + 1], 0, decoded, (int)(64 * i + 4), 60);
				}

				if (XxHash32.HashToUInt32(decoded) != Word(raw, 16))
					throw Invalid("Unsupported GRF archive key block.");

				return GrfHeader.KRODefaultProtectedKeys;
			}

			ProtectedGrfCiphers.ChamCbcDecrypt(decoded, 0, decoded.Length, Slice(digest, 0, 16), Slice(digest, 16, 16));
			if (XxHash32.HashToUInt32(decoded) != Word(raw, 16))
				throw Invalid("GRF archive key-block checksum mismatch.");
			for (int offset = 0; offset < decoded.Length; offset += 64) {
				uint id = Word(decoded, offset);
				if (id == 0 || id > 65535 || keys.ContainsKey(id))
					throw Invalid("Invalid or duplicate GRF archive key-block record.");
				keys.Add(id, Slice(decoded, offset + 4, 60));
			}
			return keys;
		}

		internal static byte[] Decode(byte[] payload, byte[] rawName, int expectedSize, IDictionary<uint, byte[]> archiveKeys) {
			if (payload == null) throw new ArgumentNullException(nameof(payload));
			if (rawName == null) throw new ArgumentNullException(nameof(rawName));
			if (expectedSize < 0 || expectedSize > MaxOutputSize)
				throw Invalid("Protected GRF output size exceeds limit or differs from file table.");
			byte[] nameHash = NativeHash(rawName);
			byte[] header = DecodeHeader(payload, nameHash);
			uint paddedSize = Word(header, 12), compressedSize = Word(header, 16), outputSize = Word(header, 20);
			if (outputSize != expectedSize || outputSize > MaxOutputSize)
				throw Invalid("Protected GRF output size exceeds limit or differs from file table.");
			if (paddedSize > MaxPackedSize || paddedSize > payload.Length - HeaderSize || compressedSize > paddedSize)
				throw Invalid("Protected GRF body size lies outside payload or exceeds limit.");
			int cipher = header[8] | (header[9] << 8);
			if (cipher != 1 && cipher != 5 && cipher != 7 && cipher != 128 && cipher != 133)
				throw Invalid("Unsupported protected GRF body cipher " + cipher + ".");
			int compression = header[10];
			if (compression > 2) throw Invalid("Unsupported protected GRF compression " + compression + ".");
			if ((header[11] & ~1) != 0) throw Invalid("Unsupported protected GRF parameter flags " + header[11] + ".");
			uint keyId = Word(header, 40);
			byte[] material;
			if (keyId == 0) material = Slice(header, 44, 60);
			else if (keyId > 65535 || archiveKeys == null || !archiveKeys.TryGetValue(keyId, out material) || material == null || material.Length != 60)
				throw Invalid("Missing or invalid GRF archive key " + keyId + ".");
			byte[] outputHash = Slice(header, 24, 16);
			byte[] parameters = new byte[64];
			for (int i = 0; i < 60; i++) parameters[i] = (byte)(material[i] ^ ((header[11] & 1) != 0 ? outputHash[i % 16] : 0));
			byte[] body = Slice(payload, HeaderSize, (int)paddedSize);
			if (compressedSize > 128) {
				if (cipher == 1)
					ProtectedGrfCiphers.DecryptChamTail(parameters, NativeHash(rawName, expectedSize), outputHash, body, 128);
				else if (cipher == 5 || cipher == 7)
					ProtectedGrfCiphers.DecryptLegacyTail(cipher, parameters, nameHash, outputHash, body, 128, (int)compressedSize - 128);
				else
					ProtectedGrfCiphers.DecryptBodyTail(cipher, parameters, nameHash, outputHash, body, 128);
			}
			byte[] output;
			switch (compression) {
				case 0:
					if (compressedSize != expectedSize) throw Invalid("Protected GRF stored output size mismatch.");
					output = body.Length == expectedSize ? body : Slice(body, 0, expectedSize);
					break;
				case 1:
					output = DecodeZlib(body, (int)compressedSize, expectedSize);
					break;
				default:
					output = new byte[expectedSize];
					// K4os returns -1 for a block producing zero bytes; the empty LZ4 block is one zero token.
					if (expectedSize == 0 ? compressedSize != 1 || body[0] != 0 :
						LZ4Codec.Decode(body, 0, (int)compressedSize, output, 0, output.Length) != expectedSize)
						throw Invalid("Invalid protected GRF LZ4 stream or output size mismatch.");
					break;
			}
			if (!Equal(NativeHash(output), outputHash)) throw Invalid("Protected GRF output hash mismatch.");
			return output;
		}

		private static byte[] DecodeHeader(byte[] payload, byte[] nameHash) {
			if (payload.Length < HeaderSize || payload[0] != 1)
				throw Invalid("Unsupported or truncated protected GRF header.");
			byte[] header = Slice(payload, 0, HeaderSize);
			switch (header[1]) {
				case 1:
				case 2:
				case 15:
					byte[] key = { 0x56, 0xfd, 0x7e, 0x71, 0x2f, 0x6d, 0x36, 0xf5, 0x5c, 0xa5, 0x99, 0xe6, 0x7b, 0x18, 0x10, 0x7a };
					byte[] iv = header[1] == 1 ? new byte[] { 0x4c, 0xe8, 0xa1, 0xae, 0x45, 0xb3, 0x21, 0x60, 0x7e, 0x4f, 0x26, 0xa4, 0xbb, 0x14, 0x0c, 0x96 } :
						header[1] == 2 ? nameHash : NativeHash(Slice(header, 4, 4));
					ProtectedGrfCiphers.AesCbcDecrypt(header, 8, 96, key, iv);
					break;
				case 113:
					ProtectedGrfCiphers.ThreeWayCfbDecrypt(header, 8, 96,
						new byte[] { 0xad, 0x61, 0xae, 0xda, 0xbf, 0x20, 0x3e, 0x1d, 0xfe, 0x1d, 0xb7, 0x2b },
						new byte[] { 0xa0, 0x95, 0xdf, 0xcf, 0xd0, 0x4b, 0x74, 0xdb, 0x02, 0x7e, 0xde, 0x09 });
					break;
				default:
					throw Invalid("Unsupported protected GRF header type " + header[1] + ".");
			}
			if (XxHash32.HashToUInt32(Slice(header, 8, 96)) != Word(header, 4))
				throw Invalid("Protected GRF header checksum mismatch.");
			return header;
		}

		private static byte[] DecodeZlib(byte[] body, int length, int expected) {
			// The existing managed zlib exposes stream completion, consumed input and Adler-32 validation.
			// Its bounded output has one sentinel byte to detect expansion beyond the declared size.
			byte[] output = new byte[expected + 1];
			var stream = new ZStream();
			if (stream.inflateInit() != zlibConst.Z_OK) throw Invalid("Could not initialize protected GRF zlib stream.");
			try {
				stream.next_in = body;
				stream.avail_in = length;
				stream.next_out = output;
				stream.avail_out = output.Length;
				int status = stream.inflate(zlibConst.Z_FINISH);
				if (status != zlibConst.Z_STREAM_END || stream.total_in != length || stream.total_out != expected)
					throw Invalid("Invalid protected GRF zlib stream: truncated, checksum failed or size differs.");
			}
			finally { stream.inflateEnd(); }
			Array.Resize(ref output, expected);
			return output;
		}

		internal static byte[] NativeHash(byte[] data, long seed = 0) {
			byte[] hash = XxHash128.Hash(data, seed);
			// .NET writes canonical big-endian high64/low64; GRF stores low64/high64 little-endian.
			Array.Reverse(hash);
			return hash;
		}

		internal static uint Word(byte[] data, int offset) {
			return (uint)(data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24);
		}

		internal static byte[] Slice(byte[] data, int offset, int length) {
			byte[] result = new byte[length];
			Buffer.BlockCopy(data, offset, result, 0, length);
			return result;
		}

		private static bool Equal(byte[] left, byte[] right) {
			if (left.Length != right.Length) return false;
			for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
			return true;
		}

		internal static InvalidDataException Invalid(string message) { return new InvalidDataException(message); }
	}
}
