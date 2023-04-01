using System;

namespace GRF.Graphics {
	public class GrfPoint3D : Object3D {
		private readonly Vertex _vertex;

		public GrfPoint3D(Vertex vertex) {
			_vertex = vertex;
		}

		public Vertex Vertex {
			get { return Matrix4.Multiply(Matrix, _vertex); }
		}
	}

	public abstract class Object3D {
		public Matrix4 Matrix { get; private set; }

		protected Object3D() {
			Matrix = Matrix4.Identity;
		}

		protected virtual void _transform() { }

		public void Translate(float x, float y, float z) {
			Matrix.SelfTranslate(x, y, z);
			_transform();
		}

		public void Scale(float x, float y, float z) {
			Matrix = Matrix4.Scale(Matrix, new Vertex(x, y, z));
			_transform();
		}
		
		public void RotateX(float degree) {
			Matrix = Matrix4.RotateX(Matrix, DegToRad(degree));
			_transform();
		}

		public void RotateY(float degree) {
			Matrix = Matrix4.RotateY(Matrix, DegToRad(degree));
			_transform();
		}

		public void RotateZ(float degree) {
			Matrix = Matrix4.RotateZ(Matrix, DegToRad(degree));
			_transform();
		}

		public static float DegToRad(float degree) {
			return (float) (degree * Math.PI / 180d);
		}

		public static float RagToDeg(float radian) {
			return (float) (radian * 180d / Math.PI);
		}
	}
}
