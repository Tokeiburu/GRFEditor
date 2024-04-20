using System;
using System.IO;
using GRF.Graphics;
using GRF.IO;
using GRF.Image;
using Utilities.Extension;

namespace GRF.FileFormats.RswFormat.RswObjects {
	/// <summary>
	/// Represents the light object type found in a RSW map.
	/// </summary>
	public class Light : RswObject {
		public Light(IBinaryReader reader) : base(RswObjectType.Light) {
			Name = reader.String(80, '\0');
			Position = reader.Vector3();
			ColorVector = new TkVector3(reader.Float(), reader.Float(), reader.Float());
			Color = GrfColor.FromArgb(255,
				(byte)(ColorVector.X * 255f),
				(byte)(ColorVector.Y * 255f),
				(byte)(ColorVector.Z * 255f));
			Range = reader.Float();
		}

		public TkVector3 ColorVector { get; set; }

		public string Name { get; private set; }
		public GrfColor Color { get; private set; }
		public float Range { get; private set; }
		
		public float Custom_Range { get; set; }
		public float Custom_Intensity { get; set; }
		public float Custom_CutOff { get; set; }
		public bool Gives_Shadow { get; set; }

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public override void Write(BinaryWriter writer) {
			base.Write(writer);
			writer.WriteANSI(Name, 80);
			Position.Write(writer);
			writer.Write(ColorVector.X);
			writer.Write(ColorVector.Y);
			writer.Write(ColorVector.Z);
			writer.Write(Range);
		}

		public float RealRange() {
			float kC = 1;
			float kL = 2.0f / Custom_Range;
			float kQ = 1.0f / (Custom_Range * Custom_Range);
			float maxChannel = Math.Max(Math.Max(Color.R, Color.G), Color.B);
			float adjustedRange = (float)((-kL + Math.Sqrt(kL * kL - 4 * kQ * (kC - 128.0f * maxChannel * Custom_Intensity))) / (2 * kQ));
			return adjustedRange;
		}
	}
}