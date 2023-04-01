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

				if (_header.IsCompatibleWith(2, 6) && _header.BuildNumber >= 186) {
					Unknown = reader.Byte();
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
			Position = reader.Vertex();
			Rotation = reader.Vertex();
			Scale = reader.Vertex();
		}

		public string Name { get; set; }
		public int AnimationType { get; private set; }
		public float AnimationSpeed { get; private set; }
		public int BlockType { get; private set; }

		public string ModelName { get; set; }
		public string NodeName { get; set; }

		public Vertex Rotation { get; set; }
		public Vertex Scale { get; set; }

		public override void Write(BinaryWriter writer) {
			base.Write(writer);
			if (_header.IsCompatibleWith(1, 3)) {
				writer.WriteANSI(Name, 40);
				writer.Write(AnimationType);
				writer.Write(AnimationSpeed);
				writer.Write(BlockType);

				if (_header.IsCompatibleWith(2, 6) && _header.BuildNumber >= 186) {
					writer.Write(Unknown);
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

			return model;
		}

		public Matrix4 GetMatrix() {
			Matrix4 mat = Matrix4.Identity;

			Vertex up = new Vertex(0, 1, 0);

			mat = Matrix4.Scale(mat, Scale);
			mat = Matrix4.Scale(mat, 1 / 5f);
			//mat.SelfTranslate(Position.Z, Position.Y, Position.X);
			mat = Matrix4.Rotate(mat, up, Rotation.X);
			mat = Matrix4.Rotate(mat, up, Rotation.Y);
			mat = Matrix4.Rotate(mat, up, Rotation.Z);
			return mat;
		}
	}
}