using System.Collections.Generic;
using System.Windows.Media.Media3D;
using GRF.FileFormats.RsmFormat;
using GRF.Graphics;
using Point = System.Windows.Point;

namespace GRFEditor.WPF.PreviewTabs {
	public class MeshRawData2 {
		public string Texture;
		public byte Alpha;
		public List<int> TriangleIndices = new List<int>();
		public List<Point> TextureCoordinates = new List<Point>();
		public List<Point3D> Positions = new List<Point3D>();
		public List<Vector3D> Normals = new List<Vector3D>();
		public Vertex Position = new Vertex(0, 0, 0);
		public BoundingBox BoundingBox = new BoundingBox();
		public Material Material;
		public Mesh Mesh;
		public bool MaterialNoTile;
	}
}
