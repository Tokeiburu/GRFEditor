using System;
using System.IO;
using GRF.Graphics;
using GRF.IO;
using GRF.Image;

namespace GRF.FileFormats.GndFormat {
	/// <summary>
	/// A texture tile for the ground map.
	/// </summary>
	public class Tile : IWriteableObject {
		public GrfColor TileColor = new GrfColor();
		public TkVector2[] TexCoords = new TkVector2[4];

		/// <summary>
		/// Initializes a new instance of the <see cref="Tile" /> class.
		/// </summary>
		/// <param name="textureIndex">Index of the texture.</param>
		public Tile(Int16 textureIndex = 0) {
			TextureIndex = textureIndex;
			TileColor = GrfColor.FromArgb(255, 255, 255, 255);
			TexCoords[0] = new TkVector2(1, 0);
			TexCoords[1] = new TkVector2(0, 0);
			TexCoords[2] = new TkVector2(1, 1);
			TexCoords[3] = new TkVector2(0, 1);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Tile" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		public Tile(IBinaryReader data) {
			for (int j = 0; j < 2; j++)
				for (int i = 0; i < 4; i++)
					TexCoords[i][j] = data.Float();

			TextureIndex = data.Int16();
			LightmapIndex = data.UInt16();
			TileColor = data.GrfColor();
		}

		public Tile(Tile tile) {
			for (int i = 0; i < 4; i++)
				TexCoords[i] = tile.TexCoords[i];

			TextureIndex = tile.TextureIndex;
			LightmapIndex = tile.LightmapIndex;
			TileColor = tile.TileColor;
		}

		/// <summary>
		/// Gets or sets the index of the texture.
		/// </summary>
		public Int16 TextureIndex { get; set; }

		/// <summary>
		/// Gets or sets the index of the lightmap.
		/// </summary>
		public UInt16 LightmapIndex { get; set; }

		#region IWriteableObject Members

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Write(BinaryWriter writer) {
			for (int j = 0; j < 2; j++)
				for (int i = 0; i < 4; i++)
					writer.Write(TexCoords[i][j]);

			writer.Write(TextureIndex);
			writer.Write(LightmapIndex);
			writer.Write(new byte[] { TileColor.A, TileColor.R, TileColor.G, TileColor.B });
		}

		#endregion

		/// <summary>
		/// Copies this instance.
		/// </summary>
		/// <returns>A copy of this object.</returns>
		public Tile Copy() {
			Tile tile = new Tile();

			for (int i = 0; i < 4; i++)
				tile.TexCoords[i] = TexCoords[i];

			tile.TileColor = new GrfColor(TileColor);
			tile.LightmapIndex = LightmapIndex;
			tile.TextureIndex = TextureIndex;

			return tile;
		}

		/// <summary>
		/// Resets the texture uv.
		/// </summary>
		public void ResetTextureUv() {
			TexCoords[0][0] = 1.0f;
			TexCoords[0][1] = 0.0f;
			TexCoords[1][0] = 1.0f;
			TexCoords[1][1] = 0.0f;
			TexCoords[2][0] = 0.0f;
			TexCoords[2][1] = 0.0f;
			TexCoords[3][0] = 1.0f;
			TexCoords[3][1] = 1.0f;
		}
	}
}