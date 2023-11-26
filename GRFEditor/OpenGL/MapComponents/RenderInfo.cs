using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GRFEditor.OpenGL.MapComponents {
	public class RenderInfo {
		public int Vao { get; private set; }
		public Vbo Vbo;
		public List<VboIndex> Indices = new List<VboIndex>();
		public Matrix4 Matrix = Matrix4.Identity;
		public Matrix4 MatrixSub = Matrix4.Identity;
		public List<Vertex> Vertices;
		public float[] RawVertices;

		public void Unload() {
			if (Vbo != null)
				Vbo.Unload();
			if (Vao > 0) {
				GL.DeleteVertexArray(Vao);
				OpenGLMemoryManager.DelVao(Vao);
			}
		}

		public void CreateVao() {
			Vao = GL.GenVertexArray();
			OpenGLMemoryManager.AddVao(Vao);
			GL.BindVertexArray(Vao);
		}

		public void BindVao() {
			GL.BindVertexArray(Vao);
		}

		public bool VaoCreated() {
			return Vao != 0;
		}
	}

	public struct VboIndex {
		public int Texture;
		public int Begin;
		public int Count;
	}

	public struct Vertex {
		public float[] data;

		public Vertex(Vector3 pos, Vector2 tex, Vector3 n) {
			data = new float[8];
			data[0] = pos.X;
			data[1] = pos.Y;
			data[2] = pos.Z;

			data[3] = tex.X;
			data[4] = tex.Y;

			data[5] = n.X;
			data[6] = n.Y;
			data[7] = n.Z;
		}

		public Vertex(Vector3 pos, Vector2 tex) {
			data = new float[5];
			data[0] = pos.X;
			data[1] = pos.Y;
			data[2] = pos.Z;

			data[3] = tex.X;
			data[4] = tex.Y;
		}

		public Vertex(Vector3 pos, Vector2 t1, Vector2 t2, Vector4 c1, Vector3 n) {
			data = new float[14];
			data[0] = pos.X;
			data[1] = pos.Y;
			data[2] = pos.Z;

			data[3] = t1.X;
			data[4] = t1.Y;

			data[5] = t2.X;
			data[6] = t2.Y;

			data[7] = c1.X;
			data[8] = c1.Y;
			data[9] = c1.Z;
			data[10] = c1.W;

			data[11] = n.X;
			data[12] = n.Y;
			data[13] = n.Z;
		}
	}

	public class OpenGLMemoryManager {
		public static Dictionary<int, int> VertexArrayObjects = new Dictionary<int, int>();
		public static Dictionary<int, int> VertexBufferObjects = new Dictionary<int, int>();
		public static Dictionary<int, int> TextureIds = new Dictionary<int, int>();

		public static void AddVao(int id) {
			if (VertexArrayObjects.ContainsKey(id)) {
				VertexArrayObjects[id]++;
			}
			else {
				VertexArrayObjects[id] = 1;
			}
		}

		public static void DelVao(int id) {
			if (VertexArrayObjects.ContainsKey(id)) {
				VertexArrayObjects[id]--;
			}
			else {
				Console.WriteLine("Attempted to remove a non-existing VAO: " + id);
			}
		}

		public static int AddVbo(int id) {
			if (VertexBufferObjects.ContainsKey(id)) {
				VertexBufferObjects[id]++;
			}
			else {
				VertexBufferObjects[id] = 1;
			}

			return id;
		}

		public static void DelVbo(int id) {
			if (VertexBufferObjects.ContainsKey(id)) {
				VertexBufferObjects[id]--;
			}
			else {
				Console.WriteLine("Attempted to remove a non-existing VBO: " + id);
			}
		}

		public static void AddTextureId(int id) {
			if (TextureIds.ContainsKey(id)) {
				TextureIds[id]++;
			}
			else {
				TextureIds[id] = 1;
			}
		}

		public static void DelTextureId(int id) {
			if (TextureIds.ContainsKey(id)) {
				TextureIds[id]--;
			}
			else {
				Console.WriteLine("Attempted to remove a non-existing texture: " + id);
			}
		}

		public static void PrintLeaked() {
			_print(VertexArrayObjects, "Vao");
			_print(VertexBufferObjects, "Vbo");
			_print(TextureIds, "TextureIds");
		}

		private static void _print(Dictionary<int, int> buffer, string name) {
			foreach (var entry in buffer.Where(p => p.Value > 0)) {
				Console.WriteLine(name + ": " + entry.Key + "=" + entry.Value);
			}
		}

		public static void Clear() {
			foreach (var entry in VertexArrayObjects.Where(p => p.Value > 0).Select(p => p.Key).ToList()) {
				DelVao(entry);
			}

			foreach (var entry in VertexBufferObjects.Where(p => p.Value > 0).Select(p => p.Key).ToList()) {
				DelVbo(entry);
			}

			foreach (var entry in TextureIds.Where(p => p.Value > 0).Select(p => p.Key).ToList()) {
				DelTextureId(entry);
			}
		}
	}
}
