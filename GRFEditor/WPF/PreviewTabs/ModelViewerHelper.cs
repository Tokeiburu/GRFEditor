using System;
using System.Windows.Media.Media3D;
using GRF.Graphics;
using Point = System.Windows.Point;

namespace GRFEditor.WPF.PreviewTabs {
	public static class ModelViewerHelper {
		public static Point ToPoint(this GRF.Graphics.Point textureVertex) {
			return new Point(textureVertex.X, textureVertex.Y);
		}

		public static Point ToPoint(float[] values, int index) {
			return new Point(values[index], values[index + 1]);
		}

		public static Vector3D ToVector3D(this Vertex vertex) {
			return new Vector3D(vertex.X, vertex.Y, vertex.Z);
		}

		public static Vector3D ToVector3D(float[] values, int index) {
			return new Vector3D(values[index], values[index + 1], values[index + 2]);
		}

		public static Point3D ToPoint3D(this Vertex position) {
			return new Point3D(position.X, position.Y, position.Z);
		}

		public static Point3D ToPoint3D(float[] values, int index) {
			return new Point3D(values[index], values[index + 1], values[index + 2]);
		}

		public static Matrix3D ToMatrix3D(this Matrix4 matrix) {
			return new Matrix3D(
				matrix[0], matrix[1], matrix[2], matrix[3],
				matrix[4], matrix[5], matrix[6], matrix[7],
				matrix[8], matrix[9], matrix[10], matrix[11],
				matrix[12], matrix[13], matrix[14], matrix[15]
				);
		}

		public static double ToRad(double angle) {
			return angle * (Math.PI / 180f);
		}

		public static float ToRad(float angle) {
			return (float) (angle * (Math.PI / 180f));
		}
	}
}