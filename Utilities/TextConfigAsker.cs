using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Utilities {
	/// <summary>
	/// ROUtilityTool library class
	/// Used to write simple and basic configuration files
	/// They are very tolerant to errors and easy to use.
	/// </summary>
	public sealed class TextConfigAsker : ConfigAsker {
		private string _data;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigAsker" /> class.
		/// </summary>
		/// <param name="data">The data of the file.</param>
		public TextConfigAsker(byte[] data) : base(false) {
			_data = Encoding.Default.GetString(data);
			_load();
		}

		public TextConfigAsker(MemoryStream stream) : base(false) {
			byte[] data = new byte[stream.Length];
			stream.Read(data, 0, data.Length);

			_data = Encoding.Default.GetString(data);
			_load();
		}

		protected override void _save() {
			StringBuilder builder = new StringBuilder();

			foreach (KeyValuePair<string, string> property in _properties.OrderBy(p => p.Key)) {
				builder.AppendLine(property.Key + "=" + property.Value);
			}

			_data = builder.ToString();
			_properties.Clear();
			_load();
		}

		public string GetData() {
			_save();
			return _data;
		}

		public byte[] GetByteData() {
			_save();
			return Encoding.Default.GetBytes(_data);
		}

		protected override void _load() {
			using (StreamReader configStream = new StreamReader(new MemoryStream(Encoding.Default.GetBytes(_data)))) {
				while (!configStream.EndOfStream) {
					string buffer = configStream.ReadLine();

					if (buffer != null) {
						string[] values = buffer.Split(new char[] { '=' }, 2);

						try {
							_properties[values[0]] = values[1];
						}
						catch { }
					}
				}
			}
		}
	}
}
