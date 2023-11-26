using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GRF.ContainerFormat;
using GRF.IO;
using GRFEditor.WPF.PreviewTabs.GLGroup;

namespace GRFEditor.Core.MapRenderer {
	public class MapGnd {
		public short Version { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public float TileScale { get; set; }
		public int TextureCount { get; set; }
		public int MaxTexName { get; set; }
		public List<GLTexture> Textures;
		public List<MapLightmap> Lightmaps;
		public int LightmapWidth { get; set; }
		public int LightmapHeight { get; set; }
		public int GridSizeCell { get; set; }

		public MapGnd(ByteReader reader) {
			string magic = reader.StringANSI(4);

			if (magic != "GRGN")
				throw GrfExceptions.__FileFormatException.Create("GND");

			Version = reader.Int16();
			Width = reader.Int32();
			Height = reader.Int32();
			TileScale = reader.Float();
			TextureCount = reader.Int32();
			MaxTexName = reader.Int32();

			Textures = new List<GLTexture>(TextureCount);

			for (int i = 0; i < TextureCount; i++) {
				var path = reader.String(MaxTexName, '\0');

				GLTexture texture = new GLTexture();
				texture.GrfPath = @"data\texture\" + path;
			}

			int lightmapCount = reader.Int32();
			LightmapWidth = reader.Int32();
			LightmapHeight = reader.Int32();
			GridSizeCell = reader.Int32();
			
			if (LightmapWidth == 0 || LightmapHeight == 0 || GridSizeCell == 0) {
				LightmapWidth = 8;
				LightmapHeight = 8;
				GridSizeCell = 8;
			}

			Lightmaps = new List<MapLightmap>(lightmapCount);

			for (int i = 0; i < lightmapCount; i++) {
				MapLightmap lightmap = new MapLightmap();
				lightmap.Data = reader.Bytes(LightmapWidth * LightmapHeight * 4);
			}

			int tileCount = reader.Int32();

			for (int i = 0; i < tileCount; i++) {
				MapTile tile = new MapTile();
				tile.V1.X = reader.Float();
				tile.V2.X = reader.Float();
				tile.V3.X = reader.Float();
				tile.V4.X = reader.Float();
				tile.V1.Y = reader.Float();
				tile.V2.Y = reader.Float();
				tile.V3.Y = reader.Float();
				tile.V4.Y = reader.Float();
			}
		}
	}
}
