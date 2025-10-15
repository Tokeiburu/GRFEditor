using GRF.FileFormats.RswFormat.RswObjects;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.WPF;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GRFEditor.OpenGL.MapRenderers {
	public class ModelRenderer : Renderer {
		public readonly SharedRsmRenderer Renderer;
		public readonly Model Model;
		public Matrix4 MatrixCache;
		public bool IsHidden { get; set; }
		public bool IsMatrixCached { get; set; }
		public bool ReverseCullFace { get; set; }

		public ModelRenderer(Shader shader, Model model, SharedRsmRenderer renderer) {
			Shader = shader;
			Model = model;
			Renderer = renderer;
		}

		public void CalculateCachedMatrix() {
			if (IsMatrixCached)
				return;

			MatrixCache = Matrix4.Identity;
			MatrixCache = GLHelper.Scale(ref MatrixCache, new Vector3(1, 1, -1));

			if (Renderer.Gnd != null) {
				MatrixCache = GLHelper.Translate(ref MatrixCache, new Vector3(5 * Renderer.Gnd.Width + Model.Position.X, -Model.Position.Y, -10 - 5 * Renderer.Gnd.Height + Model.Position.Z));
				MatrixCache = GLHelper.Rotate(ref MatrixCache, -GLHelper.ToRad(Model.Rotation.Z), new Vector3(0, 0, 1));
				MatrixCache = GLHelper.Rotate(ref MatrixCache, -GLHelper.ToRad(Model.Rotation.X), new Vector3(1, 0, 0));
				MatrixCache = GLHelper.Rotate(ref MatrixCache, GLHelper.ToRad(Model.Rotation.Y), new Vector3(0, 1, 0));
				MatrixCache = GLHelper.Scale(ref MatrixCache, new Vector3(Model.Scale.X, -Model.Scale.Y, Model.Scale.Z));

				if (Renderer.Rsm.Version < 2.2) {
					MatrixCache = GLHelper.Translate(ref MatrixCache, new Vector3(-Renderer.Rsm.VerticesBox.Center.X, Renderer.Rsm.VerticesBox.Min.Y, -Renderer.Rsm.VerticesBox.Center.Z));
				}
				else {
					MatrixCache = GLHelper.Scale(ref MatrixCache, new Vector3(1, -1, 1));
				}

				if (MatrixCache[3, 0] * 0.2f < 0 ||
					MatrixCache[3, 0] * 0.2f > Renderer.Gnd.Header.Width * 2 ||
					MatrixCache[3, 2] * 0.2f < 0 ||
					MatrixCache[3, 2] * 0.2f > Renderer.Gnd.Header.Height * 2) {
					IsHidden = true;
				}
			}
			else {
				if (Renderer.Rsm.Version < 2.2) {
					MatrixCache = GLHelper.Translate(ref MatrixCache, -Renderer.Rsm.DrawnBox.Center);
					MatrixCache = GLHelper.Scale(ref MatrixCache, new Vector3(1, -1, 1));
				}
				else {
					MatrixCache = GLHelper.Translate(ref MatrixCache, Renderer.Rsm.DrawnBox.Center * new Vector3(1, -1, -1));
					//MatrixCache = GLHelper.Scale(ref MatrixCache, new Vector3(-1, 1, 1));
				}
			}

			if (Model != null && (Model.Scale.X * Model.Scale.Y * Model.Scale.Z * (Renderer.Rsm.Version >= 2.2 ? -1 : 1) < 0))
				ReverseCullFace = true;

			IsMatrixCached = true;
		}

		public ModelRenderer(RendererLoadRequest request, Rsm rsm, Shader shader) {
			Shader = shader;
			Renderer = new SharedRsmRenderer(request, shader, rsm);
		}

		public override void Load(OpenGLViewport viewport) {
			if (IsUnloaded)
				return;

			CalculateCachedMatrix();
			Renderer.Load(viewport);
			IsLoaded = true;
		}

		public override void Render(OpenGLViewport viewport) {
			if (IsUnloaded || IsHidden)
				return;
			if (!IsLoaded)
				Load(viewport);

			Renderer.Render(viewport, ref MatrixCache);
		}

		public override void Unload() {
			IsUnloaded = true;
			Renderer.Unload();
		}
	}
}
