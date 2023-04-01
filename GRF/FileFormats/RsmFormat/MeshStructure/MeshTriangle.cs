using GRF.Graphics;

namespace GRF.FileFormats.RsmFormat.MeshStructure {
	public class MeshTriangle {
		public Vertex[] Normals = new Vertex[3];
		public Vertex[] Positions = new Vertex[3];
		public Point[] TextureCoords = new Point[3];
	}
}