using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ErrorManager;
using GRF.FileFormats.GatFormat;
using GRF.Graphics;
using GRFEditor.OpenGL.MapComponents;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using Utilities;
using ButtonState = OpenTK.Input.ButtonState;
using Clipboard = System.Windows.Clipboard;
using Control = System.Windows.Forms.Control;
using Key = System.Windows.Input.Key;
using Keyboard = System.Windows.Input.Keyboard;
using Matrix4 = OpenTK.Matrix4;
using UserControl = System.Windows.Controls.UserControl;
using Vertex = GRFEditor.OpenGL.MapComponents.Vertex;

namespace GRFEditor.OpenGL.WPF {
	public partial class OpenGLViewport : UserControl {
		private bool _selectingTiles;
		private Vector3 _mouseDragStart;
		private Vector3 _mouse3D;
		private List<SelectionTile> _selectionTiles = new List<SelectionTile>();
		private RenderInfo _selectionRi = new RenderInfo();

		public struct SelectionTile {
			public int X;
			public int Y;

			public SelectionTile(int x, int y) {
				X = x;
				Y = y;
			}

			public static SelectionTile operator -(SelectionTile a, SelectionTile b) {
				return new SelectionTile(a.X - b.X, a.Y - b.Y);
			}
		}

		private void _primary_KeyDown(object sender, KeyEventArgs e) {
			if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.C)) {
				_copySelection();
			}
			else if (Keyboard.IsKeyDown(Key.P)) {
				_camera.Mode = _camera.Mode == CameraMode.PerspectiveOpenGL ? CameraMode.PerspectiveDirectX : CameraMode.PerspectiveOpenGL;
			}
		}

		private void _selectionRenderSub() {
			if (!_selectionRi.VaoCreated() || _selectionRi.Vbo.Length == 0 || _selectionTiles.Count == 0)
				return;

			_selectionRi.BindVao();
			Shader_simple.Use();
			Shader_simple.SetMatrix4("projectionMatrix", Projection);
			Shader_simple.SetMatrix4("viewMatrix", View);
			Shader_simple.SetVector4("color", new Vector4(1, 0, 0, 1.0f));
			GL.LineWidth(1.0f);
			GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
			GL.DrawArrays(PrimitiveType.Triangles, 0, _selectionRi.Vbo.Length);
			GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
			Shader_simple.SetVector4("color", new Vector4(1, 0, 0, 0.25f));
			GL.DrawArrays(PrimitiveType.Triangles, 0, _selectionRi.Vbo.Length);
		}

		private void _selectionRender() {
			if (_request == null || !_request.IsMap || _request.Gnd == null)
				return;

			try {
				var mouseState = Mouse.GetState();

				if (mouseState.LeftButton == ButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftShift) && _editorWindow != null && _editorWindow.IsActive) {
					var pt = _primary.PointToClient(Control.MousePosition);
					int mouseX = (int)pt.X;
					int mouseY = (int)pt.Y;

					int[] viewPort = new int[] { 0, 0, _primary.Width, _primary.Height };
					Vector2 mousePosScreenSpace = new Vector2(mouseX, mouseY);
					mousePosScreenSpace.Y = _primary.Height - mousePosScreenSpace.Y;

					Vector4 viewport2 = new Vector4(viewPort[0], viewPort[1], viewPort[2], viewPort[3]);
					Vector3 retNear = GLHelper.UnProject(new Vector3(mousePosScreenSpace.X, mousePosScreenSpace.Y, 0.0f), ref View, ref Projection, ref viewport2);
					Vector3 retFar = GLHelper.UnProject(new Vector3(mousePosScreenSpace.X, mousePosScreenSpace.Y, 1.0f), ref View, ref Projection, ref viewport2);
					Ray mouseRay = new Ray(retNear, Vector3.Normalize(retFar - retNear));

					var gnd = _request.Gnd;
					_mouse3D = gnd.RayCast(mouseRay, RenderOptions.ShowBlackTiles, true);

					List<Vertex> verts = new List<Vertex>();
					_selectionTiles.Clear();

					if (!_selectingTiles) {
						_mouseDragStart = _mouse3D;
						_selectingTiles = true;
					}
					else if (_mouseDragStart == _mouse3D) {
					}
					else {
						var mouseDragEnd = _mouse3D;
						float dist = (float)(0.00008f * _camera.Distance);

						int tileMinX = (int)(Math.Min(_mouseDragStart.X, mouseDragEnd.X) / 10);
						int tileMaxX = (int)Math.Ceiling(Math.Max(_mouseDragStart.X, mouseDragEnd.X) / 10);

						int tileMaxY = _request.Gnd.Height - (int)(Math.Min(_mouseDragStart.Z, mouseDragEnd.Z) / 10) + 1;
						int tileMinY = _request.Gnd.Height - (int)Math.Ceiling(Math.Max(_mouseDragStart.Z, mouseDragEnd.Z) / 10) + 1;

						if (tileMinX >= 0 && tileMaxX < _request.Gnd.Width + 1 && tileMinY >= 0 && tileMaxY < _request.Gnd.Height + 1) {
							for (int x = tileMinX; x < tileMaxX; x++) {
								for (int y = tileMinY; y < tileMaxY; y++) {
									var cube = _request.Gnd[x, y];
									float offXDist = 0;
									float offYDist = 0;

									verts.Add(new Vertex(new Vector3(10 * x, -cube[2] + dist, 10 * _request.Gnd.Height - 10 * y - offYDist), new Vector2(0), cube.Normals[2]));
									verts.Add(new Vertex(new Vector3(10 * x + 10 + offXDist, -cube[1] + dist, 10 * _request.Gnd.Height - 10 * y + 10), new Vector2(0), cube.Normals[1]));
									verts.Add(new Vertex(new Vector3(10 * x + 10 + offXDist, -cube[3] + dist, 10 * _request.Gnd.Height - 10 * y - offYDist), new Vector2(0), cube.Normals[3]));

									verts.Add(new Vertex(new Vector3(10 * x, -cube[0] + dist, 10 * _request.Gnd.Height - 10 * y + 10), new Vector2(0), cube.Normals[0]));
									verts.Add(new Vertex(new Vector3(10 * x, -cube[2] + dist, 10 * _request.Gnd.Height - 10 * y - offYDist), new Vector2(0), cube.Normals[2]));
									verts.Add(new Vertex(new Vector3(10 * x + 10 + offXDist, -cube[1] + dist, 10 * _request.Gnd.Height - 10 * y + 10), new Vector2(0), cube.Normals[1]));

									if (cube.TileSide > -1 && x < _request.Gnd.Width - 1) {
										float dist2 = dist;
										var h1 = _request.Gnd[x + 1, y][0] + _request.Gnd[x + 1, y][2];
										var h0 = cube[1] + cube[3];
										
										if (h0 > h1)
											dist2 *= -1;

										verts.Add(new Vertex(new Vector3(10 * x + 10 + dist2, -cube[1] + dist, 10 * _request.Gnd.Height - 10 * y + 10), new Vector2(0), new Vector3(-1, 0, 0)));
										verts.Add(new Vertex(new Vector3(10 * x + 10 + dist2, -cube[3] + dist, 10 * _request.Gnd.Height - 10 * y - offYDist), new Vector2(0), new Vector3(-1, 0, 0)));
										verts.Add(new Vertex(new Vector3(10 * x + 10 + dist2, -_request.Gnd[x + 1, y][0] + dist, 10 * _request.Gnd.Height - 10 * y + 10), new Vector2(0), new Vector3(-1, 0, 0)));

										verts.Add(new Vertex(new Vector3(10 * x + 10 + dist2, -cube[3] + dist, 10 * _request.Gnd.Height - 10 * y - offYDist), new Vector2(0), new Vector3(-1, 0, 0)));
										verts.Add(new Vertex(new Vector3(10 * x + 10 + dist2, -_request.Gnd[x + 1, y][2] + dist, 10 * _request.Gnd.Height - 10 * y - offYDist), new Vector2(0), new Vector3(-1, 0, 0)));
										verts.Add(new Vertex(new Vector3(10 * x + 10 + dist2, -_request.Gnd[x + 1, y][0] + dist, 10 * _request.Gnd.Height - 10 * y + 10), new Vector2(0), new Vector3(-1, 0, 0)));
									}

									if (cube.TileFront > -1 && y < _request.Gnd.Height - 1) {
										float dist2 = dist;
										var h1 = _request.Gnd[x, y + 1][0] + _request.Gnd[x, y + 1][1];
										var h0 = cube[2] + cube[3];

										if (h0 > h1)
											dist2 *= -1;

										verts.Add(new Vertex(new Vector3(10 * x, -cube[2] + dist, 10 * _request.Gnd.Height - 10 * y - dist2), new Vector2(0), new Vector3(0, 0, 1)));
										verts.Add(new Vertex(new Vector3(10 * x + 10 + offXDist, -cube[3] + dist, 10 * _request.Gnd.Height - 10 * y - dist2), new Vector2(0), new Vector3(0, 0, 1)));
										verts.Add(new Vertex(new Vector3(10 * x, -_request.Gnd[x, y + 1][0] + dist, 10 * _request.Gnd.Height - 10 * y - dist2), new Vector2(0), new Vector3(0, 0, 1)));

										verts.Add(new Vertex(new Vector3(10 * x, -_request.Gnd[x, y + 1][0] + dist, 10 * _request.Gnd.Height - 10 * y - dist2), new Vector2(0), new Vector3(0, 0, 1)));
										verts.Add(new Vertex(new Vector3(10 * x + 10 + offXDist, -_request.Gnd[x, y + 1][1] + dist, 10 * _request.Gnd.Height - 10 * y - dist2), new Vector2(0), new Vector3(0, 0, 1)));
										verts.Add(new Vertex(new Vector3(10 * x + 10 + offXDist, -cube[3] + dist, 10 * _request.Gnd.Height - 10 * y - dist2), new Vector2(0), new Vector3(0, 0, 1)));
									}
								}
							}
						}

						_selectionTiles.Add(new SelectionTile { X = tileMinX, Y = tileMinY });
						_selectionTiles.Add(new SelectionTile { X = tileMaxX, Y = tileMaxY });
					}

					if (verts.Count == 0) {
						return;
					}

					if (!_selectionRi.VaoCreated()) {
						_selectionRi.CreateVao();
						_selectionRi.Vbo = new Vbo();
						_selectionRi.Vbo.SetData(verts, BufferUsageHint.StaticDraw);

						GL.EnableVertexAttribArray(0);
						GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
						Shader_simple.Use();
						Shader_simple.SetMatrix4("modelMatrix", Matrix4.Identity);
					}

					_selectionRi.BindVao();
					Shader_simple.Use();
					Shader_simple.SetVector4("color", new Vector4(1, 0, 0, 0.5f));
					Shader_simple.SetMatrix4("projectionMatrix", Projection);
					Shader_simple.SetMatrix4("viewMatrix", View);
					_selectionRi.Vbo.SetData(verts, BufferUsageHint.StaticDraw);
					GL.DrawArrays(PrimitiveType.Triangles, 0, _selectionRi.Vbo.Length);
				}
				else {
					if (_selectingTiles) {
						_copySelection();
					}

					_selectionRenderSub();
					_selectingTiles = false;
				}
			}
			catch { }
		}

		private void _copySelection() {
			try {
				if (_selectionTiles.Count == 0)
					return;

				List<SelectionTile> selection = new List<SelectionTile>();
				var gnd = _request.Gnd;

				if (_request.Gat == null) {
					_request.Gat = new Gat(ResourceManager.GetData(_request.Resource + ".gat"));
				}

				var gat = _request.Gat;
				var rsw = _request.Rsw;

				for (int x = _selectionTiles[0].X; x < _selectionTiles[1].X; x++) {
					for (int y = _selectionTiles[0].Y; y < _selectionTiles[1].Y; y++) {
						selection.Add(new SelectionTile { X = x, Y = y });
					}
				}

				if (selection.Count == 0)
					return;

				SelectionTile center = new SelectionTile { X = selection.Sum(p => p.X) / selection.Count, Y = selection.Sum(p => p.Y) / selection.Count };
				Vector3 centerObjects = new Vector3(center.X * 10 - 5 * gnd.Width, 0, center.Y * 10 - 5 * gnd.Height);
				StringBuilder clip = new StringBuilder();

				clip.AppendLine("{");
				clip.AppendLine(" \"center\": [");
				clip.AppendLine("  " + center.X + ",");
				clip.AppendLine("  " + center.Y + "");
				clip.AppendLine(" ],");

				List<ClipboardBE_Cube> cubes = new List<ClipboardBE_Cube>();
				List<ClipboardBE_Gat> gats = new List<ClipboardBE_Gat>();
				List<ClipboardBE_Tile> tiles = new List<ClipboardBE_Tile>();
				List<ClipboardBE_Texture> textures = new List<ClipboardBE_Texture>();
				List<ClipboardBE_Lightmap> lightmaps = new List<ClipboardBE_Lightmap>();
				List<ClipboardBE_Object> objects = new List<ClipboardBE_Object>();

				foreach (var n in selection) {
					var c = gnd[n.X, n.Y];

					cubes.Add(new ClipboardBE_Cube(n - center, c));

					for (int i = 0; i < 4; i++) {
						if (gat.InMap(2 * n.X + i % 2, 2 * n.Y + i / 2)) {
							var xx = 2 * n.X + i % 2;
							var yy = 2 * n.Y + i / 2;
							var g = gat.Cells[yy * gat.Width + xx];

							gats.Add(new ClipboardBE_Gat(new SelectionTile(xx - 2 * center.X, yy - 2 * center.Y), g));
						}
					}

					int[] tileIds = { c.TileUp, c.TileFront, c.TileSide };

					for (int i = 0; i < 3; i++) {
						if (tileIds[i] != -1) {
							var tile = gnd.Tiles[tileIds[i]];

							tiles.Add(new ClipboardBE_Tile(tileIds[i], tile));
							textures.Add(new ClipboardBE_Texture(tile.TextureIndex, gnd.Textures[tile.TextureIndex], @"¼ô\u0019"));
							lightmaps.Add(new ClipboardBE_Lightmap(tile.LightmapIndex, gnd.Lightmaps[tile.LightmapIndex], gnd.LightmapWidth, gnd.LightmapHeight));
						}
					}
				}

				foreach (var obj in rsw.Objects) {
					Vector3 pos = new Vector3(5 * gnd.Width + obj.Position.X, -obj.Position.Y, -(-10 - 5 * gnd.Height + obj.Position.Z));

					if (pos.X >= _selectionTiles[0].X * 10 && pos.X <= (_selectionTiles[1].X + 1) * 10 &&
						pos.Z >= 10 * gnd.Height - (_selectionTiles[1].Y + 1) * 10 + 10 && pos.Z <= 10 * gnd.Height - (_selectionTiles[0].Y + 0) * 10 + 10) {
						objects.Add(new ClipboardBE_Object(obj, centerObjects));
					}
				}

				textures = textures.GroupBy(p => p.TextureId).Select(p => p.First()).ToList();

				if (cubes.Count > 0) {
					clip.AppendLine(" \"cubes\": [");
					foreach (var v in cubes) {
						v.Print(clip);
					}
					ClipboardBE.RemoveLastComa(clip);
					clip.AppendLine(" ],");
				}

				if (lightmaps.Count > 0) {
					clip.AppendLine(" \"lightmaps\": {");
					foreach (var v in lightmaps) {
						v.Print(clip);
					}
					ClipboardBE.RemoveLastComa(clip);
					clip.AppendLine(" },");
				}

				if (textures.Count > 0) {
					clip.AppendLine(" \"textures\": {");
					foreach (var v in textures) {
						v.Print(clip);
					}
					ClipboardBE.RemoveLastComa(clip);
					clip.AppendLine(" },");
				}

				if (tiles.Count > 0) {
					clip.AppendLine(" \"tiles\": {");
					foreach (var v in tiles) {
						v.Print(clip);
					}
					ClipboardBE.RemoveLastComa(clip);
					clip.AppendLine(" },");
				}

				if (gats.Count > 0) {
					clip.AppendLine(" \"gats\": [");
					foreach (var v in gats) {
						v.Print(clip);
					}
					ClipboardBE.RemoveLastComa(clip);
					clip.AppendLine(" ],");
				}

				if (objects.Count > 0) {
					clip.AppendLine(" \"objects\": [");
					foreach (var v in objects) {
						v.Print(clip);
					}
					ClipboardBE.RemoveLastComa(clip);
					clip.AppendLine(" ],");
				}

				ClipboardBE.RemoveLastComa(clip);
				clip.AppendLine("}");

				Clipboard.SetText(clip.ToString());
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}
