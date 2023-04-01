using System.Collections.Generic;
using GRF.FileFormats.RsmFormat.MeshStructure;

namespace GRF.FileFormats.GndFormat {
	public class GndMesh {
		public int Width { get; set; }
		public int Height { get; set; }

		public uint[] Lightmap { get; set; }
		public int LightmapSize { get; set; }

		public byte[] TileColor { get; set; }
		public byte[] ShadowMap { get; set; }

		public List<float[]> Mesh { get; set; }
		public int MeshVertCount { get; set; }

		public List<float[]> WaterMesh { get; set; }
		public int WaterVertCount { get; set; }

		public Dictionary<string, MeshRawData> MeshRawData { get; set; }
		public Dictionary<string, MeshRawData> WaterRawData { get; set; }
	}
}