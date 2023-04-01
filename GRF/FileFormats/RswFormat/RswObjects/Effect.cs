using System.IO;
using GRF.Graphics;
using GRF.IO;
using Utilities.Extension;

namespace GRF.FileFormats.RswFormat.RswObjects {
	/// <summary>
	/// Represents the effect object type found in a RSW map.
	/// </summary>
	public class Effect : RswObject {
		/// <summary>
		/// Initializes a new instance of the <see cref="Effect" /> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="position">The position.</param>
		/// <param name="effectNumber">The effect number.</param>
		/// <param name="emitSpeed">The emit speed.</param>
		/// <param name="parameters">The parameters.</param>
		public Effect(string name, Vertex position, int effectNumber, float emitSpeed, float[] parameters) {
			Name = name;
			Position = position;
			EffectNumber = effectNumber;
			EmitSpeed = emitSpeed;
			Param = parameters;
			Type = RswObjectType.Effect;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Effect" /> class.
		/// </summary>
		/// <param name="reader">The reader.</param>
		public Effect(IBinaryReader reader) : base(RswObjectType.Effect) {
			Name = reader.String(80, '\0');
			Position = reader.Vertex();
			EffectNumber = reader.Int32();
			EmitSpeed = reader.Float();
			Param = reader.ArrayFloat(4);
		}

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the type of the effect.
		/// </summary>
		public int EffectNumber { get; set; }

		/// <summary>
		/// Gets or sets the emit speed.
		/// </summary>
		public float EmitSpeed { get; set; }

		public float[] Param { get; private set; }

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public override void Write(BinaryWriter writer) {
			base.Write(writer);
			writer.WriteANSI(Name, 80);
			Position.Write(writer);
			writer.Write(EffectNumber);
			writer.Write(EmitSpeed);
			writer.Write(Param[0]);
			writer.Write(Param[1]);
			writer.Write(Param[2]);
			writer.Write(Param[3]);
		}
	}
}