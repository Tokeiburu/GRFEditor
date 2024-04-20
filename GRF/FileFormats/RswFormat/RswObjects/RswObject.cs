using System.IO;
using GRF.Graphics;

namespace GRF.FileFormats.RswFormat.RswObjects {
	public abstract class RswObject : IWriteableObject {
		protected internal RswObject() {
		}

		protected RswObject(RswObjectType type) {
			Type = type;
		}

		public RswObjectType Type { get; protected set; }
		public TkVector3 Position { get; set; }

		#region IWriteableObject Members

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public virtual void Write(BinaryWriter writer) {
			writer.Write((int) Type);
		}

		#endregion
	}
}