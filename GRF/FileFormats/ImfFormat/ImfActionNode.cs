using System.Collections.Generic;
using System.IO;
using GRF.IO;

namespace GRF.FileFormats.ImfFormat {
	public class ImfActionNode : IWriteableObject {
		private readonly List<ImfFrameNode> _nodes = new List<ImfFrameNode>();

		/// <summary>
		/// Initializes a new instance of the <see cref="ImfActionNode" /> class.
		/// </summary>
		/// <param name="reader">The reader.</param>
		public ImfActionNode(IBinaryReader reader) {
			int count = reader.Int32();

			for (int i = 0; i < count; i++) {
				Nodes.Add(new ImfFrameNode(reader));
			}
		}

		/// <summary>
		/// Gets the nodes.
		/// </summary>
		public List<ImfFrameNode> Nodes {
			get { return _nodes; }
		}

		/// <summary>
		/// Gets the number of frames.
		/// </summary>
		public int NumberOfFrames {
			get { return _nodes.Count; }
		}

		#region IWriteableObject Members

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Write(BinaryWriter writer) {
			writer.Write(Nodes.Count);
			foreach (ImfFrameNode node in Nodes) {
				node.Write(writer);
			}
		}

		#endregion
	}
}