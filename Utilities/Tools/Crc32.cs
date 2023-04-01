using System;
using System.Security.Cryptography;

namespace Utilities.Tools {
	public class Crc32 : HashAlgorithm {
		public const UInt32 DefaultPolynomial = 0xedb88320;
		public const UInt32 DefaultSeed = 0xffffffff;
		private static UInt32[] _defaultTable;

		private readonly UInt32 _seed;
		private readonly UInt32[] _table;
		private UInt32 _hash;

		public Crc32() {
			_table = _initializeTable(DefaultPolynomial);
			_seed = DefaultSeed;
			Initialize();
		}

		public Crc32(UInt32 polynomial, UInt32 seed) {
			_table = _initializeTable(polynomial);
			_seed = seed;
			Initialize();
		}

		static Crc32() {
			UInt32[] createTable = new UInt32[256];
			for (int i = 0; i < 256; i++) {
				UInt32 entry = (UInt32)i;
				for (int j = 0; j < 8; j++)
					if ((entry & 1) == 1)
						entry = (entry >> 1) ^ DefaultPolynomial;
					else
						entry = entry >> 1;
				createTable[i] = entry;
			}

			_defaultTable = createTable;
		}

		public override int HashSize {
			get { return 32; }
		}

		public override sealed void Initialize() {
			_hash = _seed;
		}

		protected override void HashCore(byte[] buffer, int start, int length) {
			_hash = _calculateHash(_table, _hash, buffer, start, length);
		}

		protected override byte[] HashFinal() {
			byte[] hashBuffer = UInt32ToBigEndianBytes(~_hash);
			HashValue = hashBuffer;
			return hashBuffer;
		}

		public static UInt32 Compute(byte[] buffer) {
			return ~_calculateHash(_initializeTable(DefaultPolynomial), DefaultSeed, buffer, 0, buffer.Length);
		}

		public static UInt32 ComputeQuick(byte[] buffer) {
			if (buffer.Length >= 4096) {
				return ~_calculateHashQuick(_initializeTable(DefaultPolynomial), DefaultSeed, buffer, 0, buffer.Length);
			}
			else
				return ~_calculateHash(_initializeTable(DefaultPolynomial), DefaultSeed, buffer, 0, buffer.Length);
		}

		public static UInt32 Compute(UInt32 seed, byte[] buffer) {
			return ~_calculateHash(_initializeTable(DefaultPolynomial), seed, buffer, 0, buffer.Length);
		}

		public static UInt32 Compute(UInt32 polynomial, UInt32 seed, byte[] buffer) {
			return ~_calculateHash(_initializeTable(polynomial), seed, buffer, 0, buffer.Length);
		}

		private static UInt32[] _initializeTable(UInt32 polynomial) {
			if (polynomial == DefaultPolynomial && _defaultTable != null)
				return _defaultTable;

			UInt32[] createTable = new UInt32[256];
			for (int i = 0; i < 256; i++) {
				UInt32 entry = (UInt32)i;
				for (int j = 0; j < 8; j++)
					if ((entry & 1) == 1)
						entry = (entry >> 1) ^ polynomial;
					else
						entry = entry >> 1;
				createTable[i] = entry;
			}

			if (polynomial == DefaultPolynomial)
				_defaultTable = createTable;

			return createTable;
		}

		private static UInt32 _calculateHash(UInt32[] table, UInt32 seed, byte[] buffer, int start, int size) {
			UInt32 crc = seed;
			for (int i = start; i < size; i++)
				unchecked {
					crc = (crc >> 8) ^ table[buffer[i] ^ crc & 0xff];
				}
			return crc;
		}

		private static UInt32 _calculateHashQuick(UInt32[] table, UInt32 seed, byte[] buffer, int start, int size) {
			UInt32 crc = seed;
			for (int i = start; i < 2048; i++)
				unchecked {
					crc = (crc >> 8) ^ table[buffer[i] ^ crc & 0xff];
				}
			for (int i = buffer.Length - 2048; i < size; i++)
				unchecked {
					crc = (crc >> 8) ^ table[buffer[i] ^ crc & 0xff];
				}
			return crc;
		}

		private byte[] UInt32ToBigEndianBytes(UInt32 x) {
			return new byte[] {
				(byte) ((x >> 24) & 0xff),
				(byte) ((x >> 16) & 0xff),
				(byte) ((x >> 8) & 0xff),
				(byte) (x & 0xff)
			};
		}
	}
}
