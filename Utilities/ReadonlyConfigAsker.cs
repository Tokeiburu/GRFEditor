using System.IO;

namespace Utilities {
	/// <summary>
	/// ROUtilityTool library class
	/// Used to write simple and basic configuration files
	/// They are very tolerant to errors and easy to use.
	/// </summary>
	public sealed class ReadonlyConfigAsker : ConfigAsker {
		private readonly byte[] _data;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigAsker" /> class.
		/// </summary>
		/// <param name="data">The data of the file.</param>
		public ReadonlyConfigAsker(byte[] data) : base(false) {
			_data = data;
			_load();
		}

		protected override void _save() {
		}

		protected override void _load() {
			using (StreamReader configStream = new StreamReader(new MemoryStream(_data))) {
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
