using System.Collections.Generic;
using System.IO;
using GRF.IO;

namespace GRF.FileFormats.ImfFormat {
	/// <summary>
	/// IMF main node
	/// </summary>
	public class ImfMainNode : IWriteableObject {
		private readonly List<ImfActionNode> _nodes = new List<ImfActionNode>();

		/// <summary>
		/// Initializes a new instance of the <see cref="ImfMainNode" /> class.
		/// </summary>
		/// <param name="reader">The reader.</param>
		public ImfMainNode(IBinaryReader reader) {
			int count = reader.Int32();

			for (int i = 0; i < count; i++) {
				Nodes.Add(new ImfActionNode(reader));
			}
		}

		/// <summary>
		/// Gets the nodes.
		/// </summary>
		public List<ImfActionNode> Nodes {
			get { return _nodes; }
		}

		/// <summary>
		/// Gets the number of actions.
		/// </summary>
		public int NumberOfAction {
			get { return _nodes.Count; }
		}

		#region IWriteableObject Members

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Write(BinaryWriter writer) {
			writer.Write(Nodes.Count);
			foreach (ImfActionNode node in Nodes) {
				node.Write(writer);
			}
		}

		#endregion
	}
}