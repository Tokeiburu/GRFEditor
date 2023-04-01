using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GRF.ContainerFormat;
using GRF.IO;
using Utilities.Services;

namespace GRF.FileFormats.ImfFormat {
	/// <summary>
	/// Haven't got a clue what the IMF files are used for. Modifying them has no impact ingame.
	/// </summary>
	public class Imf : IPrintable, IWriteableFile {
		private readonly List<ImfMainNode> _mainNodes = new List<ImfMainNode>();

		/// <summary>
		/// Initializes a new instance of the <see cref="Imf" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		public Imf(MultiType data) : this(data.GetBinaryReader()) {
			LoadedPath = data.Path;
		}

		private Imf(IBinaryReader reader) {
			string magic = reader.StringANSI(4);

			if (magic != EncodingService.Ansi.GetString(new byte[] { 0xae, 0x47, 0x81, 0x3f }))
				throw GrfExceptions.__FileFormatException.Create("IMF");

			CheckSum = reader.UInt32();
			var count = reader.Int32() + 1;

			for (int i = 0; i < count; i++) {
				MainNodes.Add(new ImfMainNode(reader));
			}
		}

		/// <summary>
		/// Gets the checksum.
		/// </summary>
		public uint CheckSum { get; private set; }

		/// <summary>
		/// Gets the number of layers.
		/// </summary>
		public int NumberOfLayers {
			get { return _mainNodes.Count; }
		}

		/// <summary>
		/// Gets or sets the loaded path (used to save the file).
		/// </summary>
		public string LoadedPath { get; set; }

		/// <summary>
		/// Gets the main nodes.
		/// </summary>
		public List<ImfMainNode> MainNodes {
			get { return _mainNodes; }
		}

		#region IPrintable Members

		/// <summary>
		/// Prints an object to a text format.
		/// </summary>
		/// <returns>
		/// A string containing the object parsed to a text format.
		/// </returns>
		public string GetInformation() {
			StringBuilder sb = new StringBuilder();

			for (int index = 0; index < MainNodes.Count; index++) {
				ImfMainNode mainNode = MainNodes[index];
				sb.Append("Node ");
				sb.AppendLine(index.ToString(CultureInfo.InvariantCulture));
				sb.AppendLine("\tActions... : " + mainNode.NumberOfAction);

				for (int i = 0; i < mainNode.NumberOfAction; i++) {
					sb.AppendLine("\t\tFrames... : " + mainNode.Nodes[i].NumberOfFrames);

					for (int j = 0; j < mainNode.Nodes[i].NumberOfFrames; j++) {
						var node = mainNode.Nodes[i].Nodes[j];
						sb.Append("\t\t\t(");
						sb.Append(node.X);
						sb.Append(", ");
						sb.Append(node.Y);
						sb.Append(")");
						sb.Append(" Priority " + node.Priority);
						sb.AppendLine();
					}
				}

				sb.AppendLine();
			}

			return sb.ToString();
		}

		#endregion

		#region IWriteableFile Members

		/// <summary>
		/// Saves this object from the LoadedPath.
		/// </summary>
		public void Save() {
			GrfExceptions.IfNullThrow(LoadedPath, "LoadedPath");
			Save(LoadedPath);
		}

		/// <summary>
		/// Saves the specified filename.
		/// </summary>
		/// <param name="filename">The filename.</param>
		public void Save(string filename) {
			using (var stream = new FileStream(filename, FileMode.Create)) {
				Save(stream);
			}
		}

		/// <summary>
		/// Saves this object to the specified stream.
		/// </summary>
		/// <param name="stream">The stream.</param>
		public void Save(Stream stream) {
			_save(new BinaryWriter(stream));
		}

		#endregion

		private void _save(BinaryWriter writer) {
			writer.Write(new byte[] { 0xae, 0x47, 0x81, 0x3f });
			writer.Write(0);
			writer.Write(1);

			foreach (ImfMainNode node in MainNodes) {
				node.Write(writer);
			}
		}
	}
}