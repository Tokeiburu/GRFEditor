using GRF.FileFormats.RswFormat;
using System;
using System.Collections.Generic;

namespace GRF.Graphics {
	public static class Helper {
		public static double ToDegree(double angle) {
			return angle * (180f / Math.PI);
		}

		public static float ToDegree(float angle) {
			return (float)(angle * (180f / Math.PI));
		}

		public static double ToRad(double angle) {
			return angle * (Math.PI / 180f);
		}

		public static float ToRad(float angle) {
			return (float)(angle * (Math.PI / 180f));
		}

		public static TkVector3 ToRad(TkVector3 v) {
			return v * (float)(Math.PI / 180f);
		}

		public static TkVector3 ToDegree(TkVector3 v) {
			return v * (float)(180f / Math.PI);
		}

		public unsafe static void Copy(IntPtr basePtr, List<int> updatedIndices, int particleSize, ParticleInstance[] tempBuffer) {
			fixed (ParticleInstance* ptrTempBuffer = tempBuffer) {
				byte* ptr = (byte*)basePtr;
				for (int i = 0; i < updatedIndices.Count; i++) {
					int idx = updatedIndices[i];
					Buffer.MemoryCopy(&ptrTempBuffer[i], ptr + idx * particleSize, particleSize, particleSize);
				}
			}
		}
	}
}
