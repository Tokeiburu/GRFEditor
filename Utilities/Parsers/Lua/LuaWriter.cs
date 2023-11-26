using System;
using System.IO;
using System.Text;
using Utilities.Parsers.Lua.Structure;

namespace Utilities.Parsers.Lua {
	public class LuaWriter : IDisposable {
		private int _indentLevel;
		private Stream _writer;
		private readonly StringBuilder _builder = new StringBuilder();

		public LuaWriter(string file) {
			_writer = File.Open(file, FileMode.Create);
		}

		public LuaWriter(Stream stream) {
			_writer = stream;// new StreamWriter(stream, Encoding.GetEncoding(1252));
		}

		public void Write(LuaList list) {
			_indentLevel = 0;

			foreach (ILuaVariable variable in list.Variables) {
				variable.Write(_builder, _indentLevel);
				_builder.AppendLine();
			}

			byte[] data = Encoding.GetEncoding(1252).GetBytes(_builder.ToString());
			_writer.Write(data, 0, data.Length);
		}

		internal static void GetIndent(StringBuilder builder, int indent) {
			for (int i = 0; i < indent; i++) {
				builder.Append('\t');
			}
		}

		#region IDisposable

		private bool _disposed;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~LuaWriter() {
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing) {
			if (_disposed) {
				return;
			}
			if (disposing) {
				if (_writer != null)
					_writer.Dispose();
			}
			_writer = null;
			_disposed = true;
		}

		#endregion
	}
}
