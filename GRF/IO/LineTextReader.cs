using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GRF.IO {
	public class LineTextReader {
		private readonly List<string> _lines = new List<string>();

		public LineTextReader(string path, Encoding encoding) {
			_lines = File.ReadAllLines(path, encoding).ToList();
		}

		public LineTextReader(Stream stream, Encoding encoding) {
			using (StreamReader reader = new StreamReader(stream, encoding)) {
				while (!reader.EndOfStream) {
					var line = reader.ReadLine();

					if (line != null) {
						_lines.Add(line);
					}
				}
			}
		}

		public IEnumerable<string> Lines {
			get {
				foreach (string line in _lines) {
					if (line == null)
						continue;
					if (line.Length >= 2 && line[0] == '/' && line[1] == '/')
						continue;
					if (line.Length == 0)
						continue;

					yield return line;
				}
			}
		}

		public static  IEnumerable<string> ReadAllLines(string path, Encoding encoding) {
			return new LineTextReader(path, encoding).Lines;
		}

		public static IEnumerable<string> ReadAllLines(Stream stream, Encoding encoding) {
			return new LineTextReader(stream, encoding).Lines;
		}
	}
}