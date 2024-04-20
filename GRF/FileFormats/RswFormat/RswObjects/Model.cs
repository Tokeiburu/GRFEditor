using System.IO;
using GRF.Graphics;
using GRF.IO;
using Utilities;
using Utilities.Extension;

namespace GRF.FileFormats.RswFormat.RswObjects {
	/// <summary>
	/// Represents the model object type found in a RSW map.
	/// </summary>
	public class Model : RswObject {
		private RswHeader _header;
		private byte Unknown { get; set; }
		private int Unknown2 { get; set; }

		private Model() {
		}

		public Model(Rsw rsw) {
			_header = rsw.Header;
			Type = RswObjectType.Model;
		}

		public Model(IBinaryReader reader, RswHeader header) : base(RswObjectType.Model) {
			_header = header;

			if (_header.IsCompatibleWith(1, 3)) {
				Name = reader.String(40, '\0');
				AnimationType = reader.Int32();
				AnimationSpeed = reader.Float();

				if (AnimationSpeed < 0.0f || AnimationSpeed >= 100.0f)
					AnimationSpeed = 1.0f;

				BlockType = reader.Int32();

				if (_header.Version >= 2.6 && _header.BuildNumber >= 186) {
					Unknown = reader.Byte();
				}

				if (_header.Version >= 2.7) {
					Unknown2 = reader.Int32();

					if (Unknown2 != -1) {
						Z.F();
					}
				}
			}
			else {
				Name = "";
				AnimationType = 0;
				AnimationSpeed = 1.0f;
				BlockType = 0;
			}

			ModelName = reader.String(80, '\0');
			NodeName = reader.String(80, '\0');
			Position = reader.Vector3();
			Rotation = reader.Vector3();
			Scale = reader.Vector3();
		}

		public string Name { get; set; }
		public int AnimationType { get; set; }
		public float AnimationSpeed { get; set; }
		public int BlockType { get; private set; }

		public string ModelName { get; set; }
		public string NodeName { get; set; }

		public TkVector3 Rotation { get; set; }
		public TkVector3 Scale { get; set; }

		public override void Write(BinaryWriter writer) {
			base.Write(writer);
			if (_header.IsCompatibleWith(1, 3)) {
				writer.WriteANSI(Name, 40);
				writer.Write(AnimationType);
				writer.Write(AnimationSpeed);
				writer.Write(BlockType);

				if (_header.Version >= 2.6 && _header.BuildNumber >= 186) {
					writer.Write(Unknown);
				}

				if (_header.Version >= 2.7) {
					writer.Write(Unknown2);
				}
			}

			writer.WriteANSI(ModelName, 80);
			writer.WriteANSI(NodeName, 80);
			Position.Write(writer);
			Rotation.Write(writer);
			Scale.Write(writer);
		}

		public Model Copy() {
			var model = new Model();

			model._header = _header;
			model.Type = Type;
			model.Position = Position;

			model.Name = Name;
			model.AnimationType = AnimationType;
			model.AnimationSpeed = AnimationSpeed;
			model.BlockType = BlockType;
			model.ModelName = ModelName;
			model.NodeName = NodeName;
			model.Rotation = Rotation;
			model.Scale = Scale;
			model.Unknown = Unknown;
			model.Unknown2 = Unknown2;

			return model;
		}
	}
}