using System;
using System.IO;
using GRF.IO;
using GRF.Image;

namespace GRF.FileFormats.GndFormat {
	/// <summary>
	/// A texture tile for the ground map.
	/// </summary>
	public class Tile : IWriteableObject {
		public GrfColor TileColor = new GrfColor();
		private float _u1 = 1.0f;
		private float _u2;
		private float _u3 = 1.0f;
		private float _u4;
		private float _v1;
		private float _v2;
		private float _v3 = 1.0f;
		private float _v4 = 1.0f;

		/// <summary>
		/// Initializes a new instance of the <see cref="Tile" /> class.
		/// </summary>
		/// <param name="textureIndex">Index of the texture.</param>
		public Tile(Int16 textureIndex = 0) {
			TextureIndex = textureIndex;
			TileColor = GrfColor.FromArgb(255, 255, 255, 255);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Tile" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		public Tile(IBinaryReader data) {
			U1 = data.Float();
			U2 = data.Float();
			U3 = data.Float();
			U4 = data.Float();
			V1 = data.Float();
			V2 = data.Float();
			V3 = data.Float();
			V4 = data.Float();
			TextureIndex = data.Int16();
			LightmapIndex = data.UInt16();
			TileColor = data.GrfColor();
		}

		/// <summary>
		/// Gets the u1.
		/// </summary>
		public float U1 {
			get { return _u1; }
			set { _u1 = value; }
		}

		/// <summary>
		/// Gets the u2.
		/// </summary>
		public float U2 {
			get { return _u2; }
			set { _u2 = value; }
		}

		/// <summary>
		/// Gets the u3.
		/// </summary>
		public float U3 {
			get { return _u3; }
			set { _u3 = value; }
		}

		/// <summary>
		/// Gets the u4.
		/// </summary>
		public float U4 {
			get { return _u4; }
			set { _u4 = value; }
		}

		/// <summary>
		/// Gets the v1.
		/// </summary>
		public float V1 {
			get { return _v1; }
			set { _v1 = value; }
		}

		/// <summary>
		/// Gets the v2.
		/// </summary>
		public float V2 {
			get { return _v2; }
			set { _v2 = value; }
		}

		/// <summary>
		/// Gets the v3.
		/// </summary>
		public float V3 {
			get { return _v3; }
			set { _v3 = value; }
		}

		/// <summary>
		/// Gets the v4.
		/// </summary>
		public float V4 {
			get { return _v4; }
			set { _v4 = value; }
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
			writer.Write(_u1);
			writer.Write(_u2);
			writer.Write(_u3);
			writer.Write(_u4);
			writer.Write(_v1);
			writer.Write(_v2);
			writer.Write(_v3);
			writer.Write(_v4);
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
			tile._u1 = _u1;
			tile._u2 = _u2;
			tile._u3 = _u3;
			tile._u4 = _u4;
			tile._v1 = _v1;
			tile._v2 = _v2;
			tile._v3 = _v3;
			tile._v4 = _v4;
			tile.TileColor = new GrfColor(TileColor);
			tile.LightmapIndex = LightmapIndex;
			tile.TextureIndex = TextureIndex;

			return tile;
		}

		/// <summary>
		/// Resets the texture uv.
		/// </summary>
		public void ResetTextureUv() {
			_u1 = 1.0f;
			_u2 = 0.0f;
			_u3 = 1.0f;
			_u4 = 0.0f;
			_v1 = 0.0f;
			_v2 = 0.0f;
			_v3 = 1.0f;
			_v4 = 1.0f;
		}
	}
}