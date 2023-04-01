using System.Collections.Generic;
using System.IO;
using GRF.FileFormats.StrFormat;
using GRF.Image;
using GRF.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GRFEditor.WPF.PreviewTabs.GLGroup {
	public class GLLayer : GLObject {
		private List<int> _textureIds = null;
		private readonly StrLayer _layer;
		private readonly int _layerId;
		private readonly Shader _shader;

		public List<int> TextureIds {
			get { return _textureIds; }
		}

		public StrLayer Layer {
			get { return _layer; }
		}

		public GLLayer(StrLayer layer, int layerId, Shader shader) {
			_layer = layer;
			_layerId = layerId;
			_shader = shader;
		}

		public override void Load(OpenGLViewport viewport) {
			if (_textureIds == null) {
				_textureIds = new List<int>();
			}

			_textureIds.Clear();

			foreach (var texture in _layer.TextureNames) {
				//_textureIds.Add(GLHelper.LoadTexture(new GrfImage(ApplicationManager.GetResource(texture)), texture));
				GrfImage image = null;

				if (viewport.Grf != null) {
					var entry = viewport.Grf.FileTable.TryGet(GrfPath.Combine(Path.GetDirectoryName(viewport.RelativePath), texture));

					if (entry != null) {
						try {
							image = new GrfImage(entry.GetDecompressedData());
						}
						catch {
							// 
						}
					}
				}

				if (image != null) {
					_textureIds.Add(GLHelper.LoadTexture(image, texture));
				}
				else {
					_textureIds.Add(-1);
				}
			}

			//GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
			//GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
			//
			//GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			//GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			//
			//Shader = new Shader("shader_color.vert", "shader_color.frag");
			Shader = _shader;
			SetupShader();
		}

		public override void Draw(OpenGLViewport viewport) {
			var Inter = InterpolatedKeyFrame.Interpolate(viewport.Str, _layerId, viewport.FrameIndex);

			if (Inter == null)
				return;

			ChangeVertex(0, Inter.Vertices[2], -Inter.Vertices[6]);
			ChangeVertex(1, Inter.Vertices[1], -Inter.Vertices[5]);
			ChangeVertex(2, Inter.Vertices[0], -Inter.Vertices[4]);
			ChangeVertex(3, Inter.Vertices[3], -Inter.Vertices[7]);
			UpdateVertex();

			ChangeVertexCoord(0, Inter.TextCoords[0] + Inter.TextCoords[2], Inter.TextCoords[3] + Inter.TextCoords[1]);
			ChangeVertexCoord(1, Inter.TextCoords[0] + Inter.TextCoords[2], Inter.TextCoords[1]);
			ChangeVertexCoord(2, Inter.TextCoords[0], Inter.TextCoords[1]);
			ChangeVertexCoord(3, Inter.TextCoords[0], Inter.TextCoords[3] + Inter.TextCoords[1]);
			UpdateVertexCoords();

			Shader.SetVector4("colorMult", new Vector4(Inter.Color[0] / 255f, Inter.Color[1] / 255f, Inter.Color[2] / 255f, Inter.Color[3] / 255f));

			Matrix4 rotation = Matrix4.CreateRotationZ(GLHelper.ToRad(-Inter.Angle));

			Model = rotation;
			Model[3, 0] = (float)(Inter.Offset.X - 319);
			Model[3, 1] = (float)(-Inter.Offset.Y + 291);

			if (Inter.TextureIndex < 0 || Inter.TextureIndex >= _textureIds.Count || _textureIds[Inter.TextureIndex] < 0)
				return;

			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(GLHelper.GetOpenGlBlendFromDirectXSrc(Inter.SourceAlpha), GLHelper.GetOpenGlBlendFromDirectXDest(Inter.DestinationAlpha));

			GL.Enable(EnableCap.Texture2D);
			GL.BindTexture(TextureTarget.Texture2D, _textureIds[Inter.TextureIndex]);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			
			Shader.Use();

			Shader.SetMatrix4("model", Model);
			Shader.SetMatrix4("view", viewport.View);
			Shader.SetMatrix4("projection", viewport.Projection);

			GL.BindVertexArray(_vertexArrayObject);
			GL.DrawElements(PrimitiveType.Quads, _indices.Length, DrawElementsType.UnsignedInt, 0);
		}
	}
}
