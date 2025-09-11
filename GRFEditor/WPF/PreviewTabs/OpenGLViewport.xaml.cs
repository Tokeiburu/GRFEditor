using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GRF.Core;
using GRF.FileFormats.StrFormat;
using GRFEditor.OpenGL;
using GRFEditor.OpenGL.StrGroup;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Utilities.Tools;
using Matrix4 = OpenTK.Matrix4;
using Orientation = System.Windows.Controls.Orientation;
using UserControl = System.Windows.Controls.UserControl;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for OpenGLViewport.xaml
	/// </summary>
	public partial class OpenGLViewport : UserControl {
		private bool _loaded;
		private readonly List<GLObject> _objects = new List<GLObject>();
		private Shader _shaderPrimary;
		public Matrix4 View;
		public Matrix4 Projection;
		public ZoomEngine ZoomEngine = new ZoomEngine();

		public int FrameIndex { get; set; }
		public Str Str { get; set; }

		public OpenGLViewport() {
			InitializeComponent();

			_primary.Load += _primary_Load;
			_primary.Resize += _primary_Resize;
			_primary.Paint += _primary_Paint;
		}

		private void _render() {
			_primary_Resize(this, null);
			_primary_Paint(this, null);
		}

		private void _primary_Resize(object sender, EventArgs e) {
			if (!_loaded)
				return;

			_primary.MakeCurrent();
			GL.Viewport(0, 0, _primary.Width, _primary.Height);
		}

		private void _primary_Load(object sender, EventArgs e) {
			_primary.MakeCurrent();
			_shaderPrimary = new Shader("str.str.vert", "str.str.frag");

			var background = new GLBackground();
			var infiniteLineV = new GLLine(Orientation.Vertical);
			var infiniteLineH = new GLLine(Orientation.Horizontal);

			_objects.Add(background);
			_objects.Add(infiniteLineV);
			_objects.Add(infiniteLineH);

			foreach (var obj in _objects) {
				obj.Load(this);
			}

			_shaderPrimary.Use();

			GL.ClearColor(new Color4(1f, 0f, 0f, 1f));
			_loaded = true;
		}

		private void _loadStr(Str str, string relativePath, GrfHolder grf) {
			Grf = grf;
			RelativePath = relativePath;
			
			foreach (var layer in _objects.OfType<GLLayer>()) {
				for (int index = 0; index < layer.TextureIds.Count; index++) {
					var texture = layer.TextureIds[index];
					GL.DeleteTexture(texture);

					if (GLHelper.IndexedTextures.ContainsKey(layer.Layer.TextureNames[index])) {
						GLHelper.IndexedTextures.Remove(layer.Layer.TextureNames[index]);
					}

					if (GLHelper.IndexedImages.ContainsKey(layer.Layer.TextureNames[index])) {
						GLHelper.IndexedImages.Remove(layer.Layer.TextureNames[index]);
					}
				}
			}

			while (_objects.Count > 3) {
				_objects.RemoveAt(3);
			}

			Str = str;
			Str.ConvertInterpolatedFrames();

			for (int i = 0; i < Str.Layers.Count; i++) {
				Str.Layers[i].Index(Str.KeyFrameCount);
				var glLayer = new GLLayer(Str.Layers[i], i, _shaderPrimary);
				glLayer.Load(this);
				_objects.Add(glLayer);
			}
		}

		public string RelativePath { get; set; }
		public GrfHolder Grf { get; set; }

		private void _primary_Paint(object sender, PaintEventArgs e) {
			if (Str == null)
				return;
			if (e == null && !_loaded)
				return;

			_primary.MakeCurrent();
			GL.Clear(ClearBufferMask.ColorBufferBit);
			GL.Disable(EnableCap.Blend);

			View = Matrix4.Identity;
			Projection = Matrix4.CreateOrthographic(_primary.Width, _primary.Height, -1, 2);

			_shaderPrimary.Use();
			_shaderPrimary.SetVector4("colorMult", new Vector4(1, 1, 1, 1));

			if (FrameIndex < 0 || FrameIndex >= Str.KeyFrameCount)
				FrameIndex = 0;

			foreach (var obj in _objects) {
				obj.Draw(this);
			}

			// Swap the front and back buffers
			GL.Flush();
			_primary.SwapBuffers();
		}

		public void Load(Str str, string relativePath, GrfHolder grf) {
			_loadStr(str, relativePath, grf);
			_render();
		}

		public void Update() {
			_render();
		}
	}
}
