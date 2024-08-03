namespace GRF.FileFormats.BsonFormat {
	internal class JsonTextReader {
		public int Index { get; set; }
		public string Text { get; set; }

		public bool CanRead {
			get {
				return Index < Text.Length;
			}
		}

		public JsonTextReader(string text) {
			Index = 0;
			Text = text;
		}

		public char ReadChar() {
			return Text[Index++];
		}

		public char PeekChar() {
			return Text[Index];
		}

		public bool ReadUntil(char c) {
			char d = c == '\0' ? '1' : '\0';

			while (CanRead && (d = ReadChar()) != c) {
				// skip
			}

			return d == c;
		}

		public void SkipSpace() {
			bool keepReading = true;

			while (keepReading && CanRead) {
				switch (PeekChar()) {
					case '\r':
					case '\n':
					case '\t':
					case ' ':
						Index++;
						break;
					default:
						keepReading = false;
						break;
				}
			}
		}
	}
}
