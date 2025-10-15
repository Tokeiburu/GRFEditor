using System;
using System.Collections.Generic;
using System.Linq;
using GRF.Graphics;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Matrix4 = OpenTK.Matrix4;

namespace GRFEditor.OpenGL.MapComponents {
	public class RenderInfo {
		/// <summary>
		/// Gets the vertex array object.
		/// </summary>
		/// <value>
		/// The vertex array object.
		/// </value>
		public int Vao { get; private set; }

		/// <summary>
		/// The vertex buffer object.
		/// </summary>
		public Vbo Vbo;
		public Vbo InstanceVbo;
		public List<VboIndex> Indices = new List<VboIndex>();
		public List<Vertex> Vertices;
		public float[] RawVertices;
		public Ebo Ebo;

		public void Unload() {
			if (Vbo != null)
				Vbo.Unload();
			if (Ebo != null)
				Ebo.Unload();
			if (InstanceVbo != null)
				InstanceVbo.Unload();
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
		public int MeshTextureIndice;
		public int Begin;
		public int Count;
	}

	public struct Vertex {
		public float[] data;

		public Vertex(int size) {
			data = new float[size];
		}

		public Vertex(in Vector3 pos, in Vector2 tex, in Vector3 n) {
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

		public Vertex(in Vector3 pos, in Vector2 tex, in Vector3 n, float twoSide) {
			data = new float[9];
			data[0] = pos.X;
			data[1] = pos.Y;
			data[2] = pos.Z;

			data[3] = tex.X;
			data[4] = tex.Y;

			data[5] = n.X;
			data[6] = n.Y;
			data[7] = n.Z;

			data[8] = twoSide;
		}

		public Vertex(in Vector3 pos, in Vector2 tex) {
			data = new float[5];
			data[0] = pos.X;
			data[1] = pos.Y;
			data[2] = pos.Z;

			data[3] = tex.X;
			data[4] = tex.Y;
		}

		public Vertex(in Vector3 pos, in Vector2 t1, in Vector2 t2, in Vector4 c1, in Vector3 n) {
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

		public override string ToString() {
			return "{(" + data[0] + "; " + data[1] + "; " + data[2] + ")}";
		}
	}

	public sealed class OpenGLMemoryManager {
		public Dictionary<int, int> VertexArrayObjects = new Dictionary<int, int>();
		public Dictionary<int, int> ElementBufferObjects = new Dictionary<int, int>();
		public Dictionary<int, int> VertexBufferObjects = new Dictionary<int, int>();
		public Dictionary<int, int> TextureIds = new Dictionary<int, int>();
		private static OpenGLMemoryManager _manager;

		public static Dictionary<object, OpenGLMemoryManager> _managers = new Dictionary<object, OpenGLMemoryManager>();

		public static void MakeCurrent(object context) {
			_manager = _managers[context];
		}

		public static void Remove(object context) {
			_managers.Remove(context);
		}

		public static void CreateInstance(object context) {
			_managers[context] = new OpenGLMemoryManager();
		}

		public static void AddVao(int id) {
			if (_manager.VertexArrayObjects.ContainsKey(id)) {
				_manager.VertexArrayObjects[id]++;
			}
			else {
				_manager.VertexArrayObjects[id] = 1;
			}
		}

		public static void DelVao(int id) {
			if (_manager.VertexArrayObjects.ContainsKey(id)) {
				_manager.VertexArrayObjects[id]--;

				if (_manager.VertexArrayObjects[id] == 0)
					_manager.VertexArrayObjects.Remove(id);
			}
			else {
				GLHelper.OnLog(() => "Error: " + "Attempted to remove a non-existing VAO: " + id);
			}
		}

		public static int AddVbo(int id) {
			if (_manager.VertexBufferObjects.ContainsKey(id)) {
				_manager.VertexBufferObjects[id]++;
			}
			else {
				_manager.VertexBufferObjects[id] = 1;
			}

			return id;
		}

		public static void DelVbo(int id) {
			if (_manager.VertexBufferObjects.ContainsKey(id)) {
				_manager.VertexBufferObjects[id]--;

				if (_manager.VertexBufferObjects[id] == 0)
					_manager.VertexBufferObjects.Remove(id);
			}
			else {
				GLHelper.OnLog(() => "Error: " + "Attempted to remove a non-existing VBO: " + id);
			}
		}

		public static int AddEbo(int id) {
			if (_manager.ElementBufferObjects.ContainsKey(id)) {
				_manager.ElementBufferObjects[id]++;
			}
			else {
				_manager.ElementBufferObjects[id] = 1;
			}

			return id;
		}

		public static void DelEbo(int id) {
			if (_manager.ElementBufferObjects.ContainsKey(id)) {
				_manager.ElementBufferObjects[id]--;

				if (_manager.ElementBufferObjects[id] == 0)
					_manager.ElementBufferObjects.Remove(id);
			}
			else {
				GLHelper.OnLog(() => "Error: " + "Attempted to remove a non-existing EBO: " + id);
			}
		}

		public static void AddTextureId(int id) {
			if (_manager.TextureIds.ContainsKey(id)) {
				_manager.TextureIds[id]++;
			}
			else {
				_manager.TextureIds[id] = 1;
			}
		}

		public static int GetTextureIdInstanceCount(int id) {
			if (_manager.TextureIds.ContainsKey(id)) {
				return _manager.TextureIds[id];
			}

			return 0;
		}

		public static void DelTextureId(int id) {
			if (_manager.TextureIds.ContainsKey(id)) {
				_manager.TextureIds[id]--;

				if (_manager.TextureIds[id] == 0)
					_manager.TextureIds.Remove(id);
			}
			else {
				// Happens with threads, not a big deal; the texture is deleted anyway
				//Console.WriteLine("Attempted to remove a non-existing texture: " + id);
			}
		}

		public static void PrintLeaked() {
			_print(_manager.VertexArrayObjects, "Vao");
			_print(_manager.VertexBufferObjects, "Vbo");
			_print(_manager.TextureIds, "TextureIds");
		}

		private static void _print(Dictionary<int, int> buffer, string name) {
			foreach (var entry in buffer.Where(p => p.Value > 0)) {
				Console.WriteLine(name + ": " + entry.Key + "=" + entry.Value);
			}
		}

		public static void Clear() {
			foreach (var entry in _manager.VertexArrayObjects.Where(p => p.Value > 0).Select(p => p.Key).ToList()) {
				DelVao(entry);
			}

			foreach (var entry in _manager.VertexBufferObjects.Where(p => p.Value > 0).Select(p => p.Key).ToList()) {
				DelVbo(entry);
			}

			foreach (var entry in _manager.TextureIds.Where(p => p.Value > 0).Select(p => p.Key).ToList()) {
				DelTextureId(entry);
			}
		}
	}
}
