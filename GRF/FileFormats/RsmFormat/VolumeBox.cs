using System.IO;
using GRF.Graphics;
using GRF.IO;

namespace GRF.FileFormats.RsmFormat {
	public class VolumeBox {
		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeBox" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		/// <param name="noFlag">if set to <c>true</c> [no flag].</param>
		public VolumeBox(IBinaryReader data, bool noFlag = false) {
			Size = new TkVector3(data);
			Position = new TkVector3(data);
			Rotation = new TkVector3(data);
			Flag = noFlag ? 0 : data.Int32();
		}

		public TkVector3 Size { get; private set; }
		public TkVector3 Position { get; private set; }
		public TkVector3 Rotation { get; private set; }
		public int Flag { get; private set; }

		public void Write(BinaryWriter writer, bool noFlag = false) {
			Size.Write(writer);
			Position.Write(writer);
			Rotation.Write(writer);

			if (!noFlag)
				writer.Write(Flag);
		}
	}
}