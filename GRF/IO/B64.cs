using System;
using System.IO;
using System.Text;
using Utilities.Extension;
using Utilities.Services;

namespace GRF.IO {
	public static class B64 {
		private static string _b64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
		private static byte[] _b642index;

		static B64() {
			_b642index = new byte[256];

			for (int i = 0; i < _b64.Length; i++) {
				_b642index[(byte)_b64[i]] = (byte)i;
			}
		}

		public static string Encode(string text) {
			text = text.ReplaceAll("\r\n", "\n");
			var lines = text.Split('\n');

			StringBuilder b = new StringBuilder();

			for (int i = 0; i < lines.Length; i++) {
				var splitData = lines[i].Split('\t');

				for (int j = 0; j < splitData.Length; j++) {
					var data = splitData[j];
					var byteData = EncodingService.Utf8.GetBytes(data);
					int bitPosition = 0;
					StringBuilder bt = new StringBuilder();
					byte c = 0;

					for (int k = 0; k < byteData.Length; k++) {
						if (bitPosition == 4) {
							bt.Append(_b64[(byte)(byteData[k] >> 6) | (c << 2)]);
							bitPosition = 6;
							c = (byte)(byteData[k] & 63);
						}
						else if (bitPosition == 2) {
							bt.Append(_b64[(byte)(byteData[k] >> 4) | (c << 4)]);
							bitPosition = 4;
							c = (byte)(byteData[k] & 15);
						}
						else {
							bt.Append(_b64[(byte)(byteData[k] >> 2)]);
							bitPosition = 2;
							c = (byte)(byteData[k] & 3);
						}

						if (bitPosition >= 6) {
							bt.Append(_b64[c]);
							bitPosition = 0;
						}
					}

					if (bitPosition > 0) {
						bt.Append(_b64[c << (bitPosition == 2 ? 4 : 2)]);
					}

					var res = bt.ToString();
					var padding = 4 - (res.Length % 4);

					b.Append(res);

					if (padding < 4) {
						for (int k = 0; k < padding; k++) {
							b.Append('=');
						}
					}

					if (j != splitData.Length - 1)
						b.Append(',');
				}

				if (i < lines.Length - 1)
					b.AppendLine();
			}

			return b.ToString();
		}

		public class BitWriter : IDisposable {
			private MemoryStream _stream = new MemoryStream();
			private int _bitsLength = 0;
			private bool _disposed;
			private ushort _c = 0xffff;

			~BitWriter() {
				Dispose(true);
			}

			protected virtual void Dispose(bool disposing) {
				if (_disposed) {
					return;
				}
				if (disposing) {
					if (_stream != null)
						_stream.Dispose();
				}
				_stream = null;
				_disposed = true;
			}

			public void Dispose() {
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			public void Write(byte data, byte offset, int count) {
				if (count >= 8) {
					throw new InvalidOperationException("Count cannot be great than 8 bits.");
				}

				data = (byte)((byte)(data << offset) >> offset);

				_c = (ushort)(_c << count);
				_c = (ushort)(_c | data);

				_bitsLength += count;

				if (_bitsLength >= 8) {
					_stream.Write(new byte[] { (byte)(_c >> (_bitsLength - 8)) }, 0, 1);
					_bitsLength -= 8;
				}
			}

			public void Reset() {
				_stream.Position = 0;
				_bitsLength = 0;
			}

			public string GetString() {
				var length = _stream.Position;
				byte[] data = new byte[length];
				_stream.Position = 0;
				_stream.Read(data, 0, (int)length);

				return EncodingService.Utf8.GetString(data);
			}
		}

		public static string Decode(string text) {
			text = text.ReplaceAll("\r\n", "\n");
			var lines = text.Split('\n');

			StringBuilder b = new StringBuilder();

			for (int i = 0; i < lines.Length; i++) {
				BitWriter writer = new BitWriter();
				var line = lines[i];

				for (int k = 0; k < line.Length; k++) {
					switch (line[k]) {
						case ',':
							b.Append(writer.GetString());
							b.Append("\t");
							writer.Reset();
							break;
						case '=':
							break;
						default:
							writer.Write(_b642index[line[k]], 2, 6);
							break;
					}
				}

				b.Append(writer.GetString());

				if (i < lines.Length - 1)
					b.AppendLine();

				writer.Reset();
			}

			return b.ToString();
		}
	}
}
