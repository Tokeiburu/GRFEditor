using System.Runtime.InteropServices;

namespace GRF.FileFormats.RswFormat {
	[StructLayout(LayoutKind.Sequential)]
	public struct ParticleInstance {
		public float PositionX;
		public float PositionY;
		public float PositionZ;
		public float Seed;
		public float LifeStart;
		public float LifeEnd;
		public float AlphaDecTime;
		public float ExpandDelay;
		public float UVStart;
	}
}
