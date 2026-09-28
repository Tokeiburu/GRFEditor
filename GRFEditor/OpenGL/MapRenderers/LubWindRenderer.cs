using GRF.FileFormats.RswFormat.RswObjects;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.WPF;
using Lua.Structure;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace GRFEditor.OpenGL.MapRenderers {
	public class LubWindEffect {
		public Vector3 Position;
		public int ParticleNum;
		public Vector4 Color;
		public float Radius;
		public float Thickness;
		public float Height;
		public float Speed;
		public float FullAngle;
		public Vector2 RotateVector;
		public int SrcMode;
		public int DstMode;
		public string Texture;

		public void Load(SimplifiedLuaElement lua) {
			foreach (var property in lua.KeyValues) {
				var pv = property.Value;

				switch (property.Key.Trim('[', ']', '\"')) {
					case "pos":
						Position = new Vector3(pv[0].Cast<float>(), pv[1].Cast<float>(), pv[2].Cast<float>());
						break;
					case "particleNum":
						ParticleNum = pv.Cast<int>();
						break;
					case "color":
						Color = new Vector4(pv[1].Cast<float>(), pv[2].Cast<float>(), pv[3].Cast<float>(), pv[0].Cast<float>()) / 255.0f;
						break;
					case "radius":
						Radius = pv.Cast<float>();
						break;
					case "thickness":
						Thickness = pv.Cast<float>();
						break;
					case "height":
						Height = pv.Cast<float>();
						break;
					case "speed":
						Speed = pv.Cast<float>();
						break;
					case "fullAngle":
						FullAngle = pv.Cast<float>();
						break;
					case "rotateVector":
						RotateVector = new Vector2(pv[0].Cast<float>(), pv[1].Cast<float>());
						break;
					case "srcMode":
						SrcMode = pv.Cast<int>();
						break;
					case "destMode":
						DstMode = pv.Cast<int>();
						break;
					case "texture":
						Texture = pv.Value.Trim('\"', '[', ']').Replace("\\\\", "\\");
						break;

				}
			}
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct WindParticleParams {
		public float ParticleNum;
		public float Radius;
		public float Thickness;
		public float Height;
		public float Speed;
		public float FullAngle;
		public Vector2 RotateVector;
		public Vector3 BasePosition;
		public float Pad1;
	}

	public struct WindParticleInstance {
		public float Seed;
	}

	public class LubWindRenderer : Renderer {
		private LubWindEffect _effect;
		private readonly RendererLoadRequest _request;
		private WindParticleParams _params;
		private Matrix4 _modelMatrix = Matrix4.Identity;
		private RenderInfo _ri = new RenderInfo();
		private WindParticleInstance[] _particles = new WindParticleInstance[0];

		public LubWindRenderer(LubWindEffect effect, RendererLoadRequest request, Shader shader) {
			_effect = effect;
			_request = request;
			Shader = shader;
		}

		public override void Load(OpenGLViewport viewport) {
			if (IsUnloaded)
				return;

			Textures.Add(TextureManager.LoadTextureAsync(_effect.Texture, Rsm.RsmTexturePath + _effect.Texture.Replace("\\\\", "\\"), TextureRenderMode.LubTexture, _request));

			_modelMatrix = GLHelper.Scale(ref _modelMatrix, new Vector3(1, 1, -1));
			_modelMatrix = GLHelper.Translate(ref _modelMatrix, new Vector3(5 * _request.Gnd.Width + _effect.Position.X, -_effect.Position.Y, -10 - 5 * _request.Gnd.Height + _effect.Position.Z));

			const int segments = 20;

			List<Vertex> verts = new List<Vertex>();
			List<uint> indices = new List<uint>();

			for (int i = 0; i <= segments; i++) {
				float t = (float)i / segments;

				verts.Add(new Vertex(new Vector3(t, 0f, 0f), new Vector2(1f - t, 1.0f)));
				verts.Add(new Vertex(new Vector3(t, -1f, 0f), new Vector2(1f - t, 0.0f)));
			}

			for (int i = 0; i < segments; i++) {
				uint bottomLeft = (uint)(i * 2);
				uint topLeft = (uint)(i * 2 + 1);
				uint bottomRight = (uint)((i + 1) * 2);
				uint topRight = (uint)((i + 1) * 2 + 1);

				indices.Add(bottomLeft);
				indices.Add(bottomRight);
				indices.Add(topLeft);

				indices.Add(topLeft);
				indices.Add(bottomRight);
				indices.Add(topRight);
			}

			_ri.CreateVao();
			_ri.Vbo = new Vbo();
			_ri.Vbo.SetData(verts, BufferUsageHint.StaticDraw);

			GL.EnableVertexAttribArray(0);
			GL.EnableVertexAttribArray(1);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

			_ri.Ebo = new Ebo();
			_ri.Ebo.SetData(indices, BufferUsageHint.StaticDraw);

			_ri.InstanceVbo = new Vbo();
			_ri.InstanceVbo.Bind();
			GL.BufferData(BufferTarget.ArrayBuffer, _effect.ParticleNum * Marshal.SizeOf<WindParticleInstance>(), IntPtr.Zero, BufferUsageHint.StaticDraw);

			GL.EnableVertexAttribArray(2);
			GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, Marshal.SizeOf<WindParticleInstance>(), 0 * sizeof(float));
			GL.VertexAttribDivisor(2, 1);

			IsLoaded = true;
		}

		public override void Render(OpenGLViewport viewport) {
			throw new NotImplementedException();
		}

		public void Render(OpenGLViewport viewport, Ubo<WindParticleParams> ubo) {
			if (!IsLoaded) {
				Load(viewport);
			}

			_params.ParticleNum = _effect.ParticleNum;
			_params.Radius = _effect.Radius;
			_params.Thickness = _effect.Thickness;
			_params.Height = _effect.Height;
			_params.Speed = _effect.Speed;
			_params.FullAngle = _effect.FullAngle;
			_params.RotateVector = _effect.RotateVector;
			_params.BasePosition = _modelMatrix.Row3.Xyz;

			if (_params.ParticleNum <= 1)
				return;

			GL.BlendFuncSeparate(
				GLHelper.GetOpenGlBlendFromDirectXSrc2(_effect.SrcMode),
				GLHelper.GetOpenGlBlendFromDirectXDest2(_effect.DstMode),
				BlendingFactorSrc.One,
				BlendingFactorDest.OneMinusSrcAlpha
			);

			Shader.SetVector4("color", _effect.Color);

			ubo.SetData(ref _params);

			Textures[0].Bind();

			_ri.BindVao();
			_ri.InstanceVbo.Bind();

			if (_particles.Length != _effect.ParticleNum) {
				_particles = new WindParticleInstance[_effect.ParticleNum];

				for (int i = 0; i < _particles.Length; i++) {
					_particles[i].Seed = TkRandom.NextFloat();
				}

				GL.BufferData(BufferTarget.ArrayBuffer, _effect.ParticleNum * Marshal.SizeOf<WindParticleInstance>(), _particles, BufferUsageHint.StaticDraw);
			}

			_ri.Ebo.Bind();
			GL.DrawElementsInstanced(PrimitiveType.Triangles, _ri.Ebo.Length, DrawElementsType.UnsignedInt, IntPtr.Zero, _particles.Length);
		}

		public override void Unload() {
			throw new NotImplementedException();
		}
	}
}
