using System.IO;

namespace GRF.FileFormats.RswFormat {
	public class QuadTreeNode : IWriteableObject {
		public const int StructSize = 48;

		public float[] Center = new float[3];
		public int[] Child = new int[4];
		public float[] Halfsize = new float[3];
		public float[] Max = new float[3];
		public float[] Min = new float[3];

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Write(BinaryWriter writer) {
			writer.Write(Max[0]);
			writer.Write(Max[1]);
			writer.Write(Max[2]);
			writer.Write(Min[0]);
			writer.Write(Min[1]);
			writer.Write(Min[2]);
			writer.Write(Halfsize[0]);
			writer.Write(Halfsize[1]);
			writer.Write(Halfsize[2]);
			writer.Write(Center[0]);
			writer.Write(Center[1]);
			writer.Write(Center[2]);
		}
	}
}