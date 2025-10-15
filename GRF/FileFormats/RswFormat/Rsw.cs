using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.ContainerFormat;
using GRF.Core;
using GRF.FileFormats.GndFormat;
using GRF.FileFormats.RswFormat.RswObjects;
using GRF.IO;
using Utilities;

namespace GRF.FileFormats.RswFormat {
	/// <summary>
	/// RSW file.
	/// </summary>
	public class Rsw : IPrintable, IWriteableFile {
		private QuadTree _quadTree = new QuadTree();

		private Rsw() {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Rsw" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		public Rsw(MultiType data) : this(data.GetBinaryReader()) {
			LoadedPath = data.Path;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Rsw" /> class.
		/// </summary>
		/// <param name="reader">The reader.</param>
		public Rsw(IBinaryReader reader) {
			Objects = new List<RswObject>();
			ModelResources = new List<string>();
			LubEffects = new List<Effect>();

			Header = new RswHeader(reader);
			Water = new RswWater(reader, Header);
			Light = new RswLight(reader, Header);
			Ground = new RswGround(reader, Header);

			if (Header.Version >= 2.7) {
				int count = reader.Int32();
				reader.Forward(4 * count);
			}

			_loadOjbects(reader);

			ModelResources = Objects.Where(p => p.Type == RswObjectType.Model).Cast<Model>().Select(p => p.ModelName).Distinct().ToList();

			if (reader.CanRead && Header.IsCompatibleWith(2, 1)) {
				_quadTree = new QuadTree(reader);
			}
		}

		/// <summary>
		/// Gets or sets the loaded path.
		/// </summary>
		public string LoadedPath { get; set; }

		/// <summary>
		/// Gets the header.
		/// </summary>
		public RswHeader Header { get; private set; }

		/// <summary>
		/// Gets or sets the water.
		/// </summary>
		public RswWater Water { get; set; }

		/// <summary>
		/// Gets the light.
		/// </summary>
		public RswLight Light { get; private set; }

		/// <summary>
		/// Gets the ground.
		/// </summary>
		public RswGround Ground { get; private set; }

		/// <summary>
		/// Gets the quad tree.
		/// </summary>
		public QuadTree QuadTree {
			get { return _quadTree; }
			set {
				_quadTree = value;
			}
		}

		/// <summary>
		/// Gets or sets the objects.
		/// </summary>
		public List<RswObject> Objects { get; set; }

		/// <summary>
		/// Gets or sets the lub effects.
		/// </summary>
		public List<Effect> LubEffects { get; private set; }
		
		/// <summary>
		/// Gets or sets the model resources.
		/// </summary>
		public List<string> ModelResources { get; set; }

		#region IPrintable Members

		public string GetInformation() {
			return FileFormatParser.DisplayObjectProperties(this);
		}

		#endregion

		#region IWriteableFile Members

		public void Save() {
			GrfExceptions.IfNullThrow(LoadedPath, "_loadedPath");
			Save(LoadedPath);
		}

		public void Save(string filename) {
			GrfExceptions.IfNullThrow(filename, "filename");
			using (BinaryWriter stream = new BinaryWriter(new FileStream(filename, FileMode.Create))) {
				_save(stream);
			}
		}

		public void Save(Stream stream) {
			GrfExceptions.IfNullThrow(stream, "stream");
			_save(new BinaryWriter(stream));
		}

		#endregion

		public void RebuildQuadTree(int sizeX, int sizeY) {
			if (_quadTree == null)
				_quadTree = new QuadTree();

			QuadTree.GenerateQuadTree(sizeX, sizeY);
		}

		public void RebuildQuadTree(GrfHolder grf, int sizeX, int sizeY, Gnd gnd, float margin = 200) {
			if (_quadTree == null)
				_quadTree = new QuadTree();

			QuadTree.GenerateQuadTree(grf, sizeX, sizeY, gnd, this, margin);
		}

		public void PrintQuadTree(string filename) {
			if (_quadTree == null)
				throw new Exception("No quadtree has been loaded.");

			QuadTree.Print(filename);
		}

		/// <summary>
		/// Removes the quad tree.
		/// </summary>
		public void RemoveQuadTree() {
			_quadTree = null;
		}

		public static float WaterLevel(byte[] rsw) {
			if (rsw == null)
				return -1;

			ByteReader reader = new ByteReader(rsw);
			var header = new RswHeader(reader);
			var water = new RswWater(reader, header);

			return water.Level;
		}

		private void _save(BinaryWriter stream) {
			Header.Write(stream, Header);
			Water.Write(stream, Header);
			Light.Write(stream, Header);
			Ground.Write(stream, Header);

			if (Header.Version >= 2.7) {
				// ?? no idea what this data is
				stream.Write(0);
			}

			stream.Write(Objects.Count);

			foreach (RswObject obj in Objects) {
				obj.Write(stream);
			}

			if (Header.IsCompatibleWith(2, 1))
				QuadTree.Write(stream);
		}

		private void _loadOjbects(IBinaryReader reader) {
			int count = reader.Int32();

			for (int i = 0; i < count; i++) {
				RswObjectType type = (RswObjectType) reader.Int32();
				RswObject obj = null;

				switch (type) {
					case RswObjectType.Model:
						obj = new Model(reader, Header);
						break;
					case RswObjectType.Light:
						obj = new Light(reader);
						break;
					case RswObjectType.Sound:
						obj = new Sound(reader, Header);
						break;
					case RswObjectType.Effect:
						obj = new Effect(reader);

						if (((Effect)obj).EffectNumber == 974) {
							LubEffects.Add((Effect)obj);
						}

						break;
					default:
						continue;
				}

				Objects.Add(obj);
			}
		}

		/// <summary>
		/// Removes the objects.
		/// </summary>
		public void RemoveObjects() {
			Objects.Clear();
			LubEffects.Clear();
		}

		/// <summary>
		/// Resets the light.
		/// </summary>
		public void ResetLight() {
			Light = new RswLight();
		}

		/// <summary>
		/// Resets the ground.
		/// </summary>
		public void ResetGround() {
			Ground = new RswGround();
		}

		/// <summary>
		/// Resets the water.
		/// </summary>
		public void ResetWater() {
			Water = new RswWater();
		}

		/// <summary>
		/// Creates an empty RSW file.
		/// </summary>
		/// <param name="mapname">The mapname.</param>
		/// <returns>An empty RSW file.</returns>
		public static Rsw CreateEmpty(string mapname) {
			Rsw rsw = new Rsw();
			rsw.Objects = new List<RswObject>();
			rsw.ModelResources = new List<string>();

			rsw.LubEffects = new List<Effect>();
			rsw.Water = new RswWater();
			rsw.Light = new RswLight();
			rsw.Header = new RswHeader(mapname);
			rsw.Ground = new RswGround();
			return rsw;
		}
	}
}