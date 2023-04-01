using System.Collections.Generic;
using System.IO;
using System.Text;
using GRF.ContainerFormat;
using GRF.IO;
using Utilities.Extension;

namespace GRF.FileFormats {
	public class Fna : IPrintable, IWriteableFile {
		private readonly List<string> _files = new List<string>();

		/// <summary>
		/// Initializes a new instance of the <see cref="Fna" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		public Fna(MultiType data) : this(data.GetBinaryReader()) {
			LoadedPath = data.Path;
		}

		private Fna(IBinaryReader reader) {
			while (reader.CanRead) {
				_files.Add(reader.String(reader.Int32(), '\0'));
			}
		}

		/// <summary>
		/// Gets or sets the loaded file path of this object.
		/// </summary>
		public string LoadedPath { get; set; }

		public List<string> Files {
			get { return _files; }
		}

		#region IPrintable Members

		public string GetInformation() {
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Files : " + _files.Count);

			foreach (string file in _files) {
				sb.AppendLine("    " + file);
			}

			return sb.ToString();
		}

		#endregion

		#region IWriteableFile Members

		public void Save() {
			GrfExceptions.IfNullThrow(LoadedPath, "LoadedPath");
			Save(LoadedPath);
		}

		public void Save(string filename) {
			using (var stream = new FileStream(filename, FileMode.Create)) {
				Save(stream);
			}
		}

		public void Save(Stream stream) {
			_save(new BinaryWriter(stream));
		}

		#endregion

		private void _save(BinaryWriter writer) {
			foreach (var file in _files) {
				writer.Write(file.Length + 1);
				writer.WriteANSI(file, file.Length + 1);
			}
		}
	}
}