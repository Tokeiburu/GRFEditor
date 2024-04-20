using System.Globalization;
using System.Text;
using GRF.FileFormats.GatFormat;
using GRF.FileFormats.RswFormat.RswObjects;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.MapRenderers;
using GRFEditor.OpenGL.WPF;
using OpenTK;

namespace GRFEditor.OpenGL {
	public static class ClipboardBE {
		public static string F2S(float v) {
			return v.ToString("0.0###########################", CultureInfo.InvariantCulture);
		}

		public static void RemoveLastComa(StringBuilder b) {
			b.Remove(b.Length - 3, 3);
			b.AppendLine();
		}
	}

	public interface ClipboardBE_Interface {
		void Print(StringBuilder b);
	}

	public class ClipboardBE_Cube : ClipboardBE_Interface {
		private readonly OpenGLViewport.SelectionTile _pos;
		private readonly Cube _cube;

		public ClipboardBE_Cube(OpenGLViewport.SelectionTile pos, Cube cube) {
			_pos = pos;
			_cube = cube;
		}

		public void Print(StringBuilder b) {
			b.AppendLine("  {");
			b.Append("   \"h1\": ");
			b.Append(ClipboardBE.F2S(_cube[0]));
			b.AppendLine(",");

			b.Append("   \"h2\": ");
			b.Append(ClipboardBE.F2S(_cube[1]));
			b.AppendLine(",");

			b.Append("   \"h3\": ");
			b.Append(ClipboardBE.F2S(_cube[2]));
			b.AppendLine(",");

			b.Append("   \"h4\": ");
			b.Append(ClipboardBE.F2S(_cube[3]));
			b.AppendLine(",");

			b.Append("   \"normal\": [");
			b.Append(ClipboardBE.F2S(_cube.Normal.X));
			b.Append(",");
			b.Append(ClipboardBE.F2S(_cube.Normal.Y));
			b.Append(",");
			b.Append(ClipboardBE.F2S(_cube.Normal.Z));
			b.AppendLine("],");

			b.AppendLine("   \"normals\": [");
			for (int i = 0; i < 4; i++) {
				b.Append("   [");
				b.Append(ClipboardBE.F2S(_cube.Normals[i].X));
				b.Append(",");
				b.Append(ClipboardBE.F2S(_cube.Normals[i].Y));
				b.Append(",");
				b.Append(ClipboardBE.F2S(_cube.Normals[i].Z));
				b.AppendLine("],");
			}
			ClipboardBE.RemoveLastComa(b);
			b.AppendLine("   ],");
			b.Append("   \"pos\": [");
			b.Append(_pos.X);
			b.Append(",");
			b.Append(_pos.Y);
			b.AppendLine("],");
			b.Append("   \"tileFront\": ");
			b.Append(_cube.TileFront);
			b.AppendLine(",");
			b.Append("   \"tileSide\": ");
			b.Append(_cube.TileSide);
			b.AppendLine(",");
			b.Append("   \"tileUp\": ");
			b.AppendLine(_cube.TileUp + "");
			b.AppendLine("  },");
		}
	}

	public class ClipboardBE_Gat : ClipboardBE_Interface {
		private readonly OpenGLViewport.SelectionTile _pos;
		private readonly Cell _cell;

		public ClipboardBE_Gat(OpenGLViewport.SelectionTile pos, Cell cell) {
			_pos = pos;
			_cell = cell;
		}

		public void Print(StringBuilder b) {
			b.AppendLine("  {");
			b.AppendLine("   \"gatType\": " + (int)_cell.Type + ",");
			b.Append("   \"h1\": ");
			b.Append(ClipboardBE.F2S(_cell[0]));
			b.AppendLine(",");

			b.Append("   \"h2\": ");
			b.Append(ClipboardBE.F2S(_cell[1]));
			b.AppendLine(",");

			b.Append("   \"h3\": ");
			b.Append(ClipboardBE.F2S(_cell[2]));
			b.AppendLine(",");

			b.Append("   \"h4\": ");
			b.Append(ClipboardBE.F2S(_cell[3]));
			b.AppendLine(",");

			b.Append("   \"pos\": [");
			b.Append(_pos.X);
			b.Append(",");
			b.Append(_pos.Y);
			b.AppendLine("]");
			b.AppendLine("  },");
		}
	}

	public class ClipboardBE_Tile : ClipboardBE_Interface {
		private readonly int _tileId;
		private readonly Tile _tile;

		public ClipboardBE_Tile(int tileId, Tile tile) {
			_tileId = tileId;
			_tile = tile;
		}

		public void Print(StringBuilder b) {
			b.Append("  \"");
			b.Append(_tileId);
			b.AppendLine("\": {");

			b.Append("  \"color\": [");
			b.Append(_tile.Color.X);
			b.Append(",");
			b.Append(_tile.Color.Y);
			b.Append(",");
			b.Append(_tile.Color.Z);
			b.Append(",");
			b.Append(_tile.Color.W);
			b.AppendLine("],");

			b.AppendLine("  \"lightmapIndex\": " + _tile.LightmapIndex + ",");
			b.AppendLine("  \"texCoords\": [");

			b.Append("  [");
			b.Append(ClipboardBE.F2S(_tile.TexCoords[0].X));
			b.Append(",");
			b.Append(ClipboardBE.F2S(_tile.TexCoords[0].Y));
			b.AppendLine("],");

			b.Append("  [");
			b.Append(ClipboardBE.F2S(_tile.TexCoords[1].X));
			b.Append(",");
			b.Append(ClipboardBE.F2S(_tile.TexCoords[1].Y));
			b.AppendLine("],");

			b.Append("  [");
			b.Append(ClipboardBE.F2S(_tile.TexCoords[3].X));
			b.Append(",");
			b.Append(ClipboardBE.F2S(_tile.TexCoords[3].Y));
			b.AppendLine("],");

			b.Append("  [");
			b.Append(ClipboardBE.F2S(_tile.TexCoords[2].X));
			b.Append(",");
			b.Append(ClipboardBE.F2S(_tile.TexCoords[2].Y));
			b.AppendLine("],");

			ClipboardBE.RemoveLastComa(b);
			b.AppendLine("  ],");
			b.AppendLine("  \"textureIndex\": " + _tile.TextureIndex);
			b.AppendLine("  },");
		}
	}

	public class ClipboardBE_Lightmap : ClipboardBE_Interface {
		private readonly int _lightmapIndex;
		private readonly byte[] _data;
		private readonly int _width;
		private readonly int _height;

		public ClipboardBE_Lightmap(int lightmapIndex, byte[] data, int width, int height) {
			_lightmapIndex = lightmapIndex;
			_data = data;
			_width = width;
			_height = height;
		}

		public void Print(StringBuilder b) {
			b.AppendLine("  \"" + _lightmapIndex + "\": {");
			b.AppendLine("  \"data\": [");

			for (int i = 0; i < _data.Length; i++) {
				if (i == _data.Length - 1)
					b.AppendLine(_data[i] + "");
				else {
					b.Append(_data[i]);
					b.Append(",");
				}
			}

			b.AppendLine("  ],");

			b.AppendLine("  \"width\": " + _width + ",");
			b.AppendLine("  \"height\": " + _height);
			b.AppendLine("  },");
		}
	}

	public class ClipboardBE_Texture : ClipboardBE_Interface {
		private readonly int _textureId;
		private readonly string _file;
		private readonly string _name;

		public int TextureId {
			get { return _textureId; }
		}

		public ClipboardBE_Texture(int textureId, string file, string name) {
			_textureId = textureId;
			_file = file;
			_name = name;
		}

		public void Print(StringBuilder b) {
			b.AppendLine("  \"" + _textureId + "\": {");

			b.AppendLine("   \"file\": \"" + _file.Replace("\\", "\\\\") + "\",");
			b.AppendLine("   \"name\": \"" + _name + "\"");
			b.AppendLine("  },");
		}
	}

	public class ClipboardBE_Object : ClipboardBE_Interface {
		private readonly RswObject _obj;
		private readonly Vector3 _centerObjects;

		public ClipboardBE_Object(RswObject obj, Vector3 centerObjects) {
			_obj = obj;
			_centerObjects = centerObjects;
		}

		public void Print(StringBuilder b) {
			b.AppendLine("  {");
			b.AppendLine("   \"children\": [],");
			b.AppendLine("   \"components\": [");
			b.AppendLine("    {");
			b.AppendLine("     \"position\": [" + ClipboardBE.F2S(_obj.Position.X) + "," + ClipboardBE.F2S(_obj.Position.Y) + "," + ClipboardBE.F2S(_obj.Position.Z) + "],");

			var model = _obj as Model;
			var sound = _obj as Sound;
			var light = _obj as Light;
			var effect = _obj as Effect;

			if (model != null) {
			}

			switch(_obj.Type) {
				case RswObjectType.Model:
					b.AppendLine("     \"rotation\": [" + ClipboardBE.F2S(model.Rotation.X) + "," + ClipboardBE.F2S(model.Rotation.Y) + "," + ClipboardBE.F2S(model.Rotation.Z) + "],");
					b.AppendLine("     \"scale\": [" + ClipboardBE.F2S(model.Scale.X) + "," + ClipboardBE.F2S(model.Scale.Y) + "," + ClipboardBE.F2S(model.Scale.Z) + "],");
					break;
				case RswObjectType.Light:
				case RswObjectType.Effect:
				case RswObjectType.Sound:
					b.AppendLine("     \"rotation\": [0,0,0],");
					b.AppendLine("     \"scale\": [1,1,1],");
					break;
			}

			b.AppendLine("     \"type\": \"rswobject\"");

			b.AppendLine("    },");
			b.AppendLine("    {");

			switch(_obj.Type) {
				case RswObjectType.Model:
					b.AppendLine("      \"animSpeed\": " + ClipboardBE.F2S(model.AnimationSpeed) + ",");
					b.AppendLine("      \"animType\": " + model.AnimationType + ",");
					b.AppendLine("      \"blockType\": " + model.BlockType + ",");
					b.AppendLine("      \"fileName\": \"" + model.ModelName.Replace("\\", "\\\\") + "\",");
					b.AppendLine("      \"gatCollision\": true,");
					b.AppendLine("      \"gatStraightType\": 0,");
					b.AppendLine("      \"gatType\": -1,");
					b.AppendLine("      \"shadowStrength\": 1.0,");
					b.AppendLine("      \"type\": \"rswmodel\"");
					b.AppendLine("    },");
					b.AppendLine("    null,");
					b.AppendLine("    null,");
					b.AppendLine("    null");
					break;
				case RswObjectType.Sound:
					b.AppendLine("      \"cycle\": " + ClipboardBE.F2S(sound.Cycle) + ",");
					b.AppendLine("      \"fileName\": \"" + sound.WaveName.Replace("\\", "\\\\") + "\",");
					b.AppendLine("      \"height\": " + sound.Height + ",");
					b.AppendLine("      \"range\": " + ClipboardBE.F2S(sound.Range) + ",");
					b.AppendLine("      \"type\": \"rswsound\",");
					b.AppendLine("      \"vol\": " + ClipboardBE.F2S(sound.Volume) + ",");
					b.AppendLine("      \"width\": " + sound.Width);
					b.AppendLine("    },");
					b.AppendLine("    null,");
					b.AppendLine("    null");
					break;
				case RswObjectType.Light:
					b.AppendLine("     \"affectLightmap\": true,");
					b.AppendLine("     \"affectShadowMap\": true,");
					b.AppendLine("     \"color\": [" + ClipboardBE.F2S(light.ColorVector.X) + "," + ClipboardBE.F2S(light.ColorVector.Y) + "," + ClipboardBE.F2S(light.ColorVector.Z) + "],");
					b.AppendLine("     \"cutOff\": 0.5,");
					b.AppendLine("     \"direction\": [0.5773502588272095,-0.5773502588272095,0.5773502588272095],");
					b.AppendLine("     \"enabled\": true,");
					b.AppendLine("     \"falloff\": [[0.0,1.0],[1.0,0.0]],");
					b.AppendLine("     \"falloffStyle\": 0,");
					b.AppendLine("     \"givesShadow\": true,");
					b.AppendLine("     \"intensity\": 1.0,");
					b.AppendLine("     \"lightType\": 0,");
					b.AppendLine("     \"range\": " + ClipboardBE.F2S(light.Range) + ",");
					b.AppendLine("     \"shadowTerrain\": true,");
					b.AppendLine("     \"spotlightWidth\": 0.5,");
					b.AppendLine("     \"sunMatchRswDirection\": true,");
					b.AppendLine("     \"type\": \"rswlight\"");
					b.AppendLine("    },");
					b.AppendLine("    null,");
					b.AppendLine("    null");
					break;
				case RswObjectType.Effect:
					LubEffect lubEffect = effect.LubEffectAttached as LubEffect;
					if (lubEffect == null) {
						lubEffect = new LubEffect();
						lubEffect.Color = new Vector4(1);
						lubEffect.Life = new Vector2(5);
						lubEffect.Maxcount = 5;
						lubEffect.Pos = new Vector3(effect.Position.X, effect.Position.Y, effect.Position.Z);
						lubEffect.Size = new Vector2(20);
						lubEffect.Billboard_off = 1;
						lubEffect.Texture = @"effect\\smoke.bmp";
						lubEffect.Srcmode = 5;
						lubEffect.Destmode = 2;
						lubEffect.Zenable = true;
					}

					b.AppendLine("     \"id\": " + effect.EffectNumber + ",");
					b.AppendLine("     \"loop\": " + ClipboardBE.F2S(effect.EmitSpeed) + ",");
					b.AppendLine("     \"param1\": " + ClipboardBE.F2S(effect.Param[0]) + ",");
					b.AppendLine("     \"param2\": " + ClipboardBE.F2S(effect.Param[1]) + ",");
					b.AppendLine("     \"param3\": " + ClipboardBE.F2S(effect.Param[2]) + ",");
					b.AppendLine("     \"param4\": " + ClipboardBE.F2S(effect.Param[3]) + ",");
					b.AppendLine("     \"type\": \"rsweffect\"");
					b.AppendLine("    },");
					b.AppendLine("    null,");
					b.AppendLine("    null,");
					b.AppendLine("    {");
					b.AppendLine("     \"billboard_off\": " + lubEffect.Billboard_off + ",");
					b.AppendLine("     \"color\": [" + ClipboardBE.F2S(lubEffect.Color.X) + "," + ClipboardBE.F2S(lubEffect.Color.Y) + "," + ClipboardBE.F2S(lubEffect.Color.Z) + "," + ClipboardBE.F2S(lubEffect.Color.W) + "],");
					b.AppendLine("     \"destmode\": " + lubEffect.Destmode + ",");
					b.AppendLine("     \"dir1\": [" + ClipboardBE.F2S(lubEffect.Dir1.X) + "," + ClipboardBE.F2S(lubEffect.Dir1.Y) + "," + ClipboardBE.F2S(lubEffect.Dir1.Z) + "],");
					b.AppendLine("     \"dir2\": [" + ClipboardBE.F2S(lubEffect.Dir2.X) + "," + ClipboardBE.F2S(lubEffect.Dir2.Y) + "," + ClipboardBE.F2S(lubEffect.Dir2.Z) + "],");
					b.AppendLine("     \"gravity\": [" + ClipboardBE.F2S(lubEffect.Gravity.X) + "," + ClipboardBE.F2S(lubEffect.Gravity.Y) + "," + ClipboardBE.F2S(lubEffect.Gravity.Z) + "],");
					b.AppendLine("     \"life\": [" + ClipboardBE.F2S(lubEffect.Life.X) + "," + ClipboardBE.F2S(lubEffect.Life.Y) + "],");
					b.AppendLine("     \"maxcount\": " + lubEffect.Maxcount + ",");
					b.AppendLine("     \"pos\": [" + ClipboardBE.F2S(lubEffect.Pos.X) + "," + ClipboardBE.F2S(lubEffect.Pos.Y) + "," + ClipboardBE.F2S(lubEffect.Color.Z) + "],");
					b.AppendLine("     \"radius\": [" + ClipboardBE.F2S(lubEffect.Radius.X) + "," + ClipboardBE.F2S(lubEffect.Radius.Y) + "," + ClipboardBE.F2S(lubEffect.Radius.Z) + "],");
					b.AppendLine("     \"rate\": [" + ClipboardBE.F2S(lubEffect.Rate.X) + "," + ClipboardBE.F2S(lubEffect.Rate.Y) + "],");
					b.AppendLine("     \"rotate_angle\": [" + ClipboardBE.F2S(lubEffect.Rotate_angle.X) + "," + ClipboardBE.F2S(lubEffect.Rotate_angle.Y) + "," + ClipboardBE.F2S(lubEffect.Rotate_angle.Z) + "],");
					b.AppendLine("     \"size\": [" + ClipboardBE.F2S(lubEffect.Size.X) + "," + ClipboardBE.F2S(lubEffect.Size.Y) + "],");
					b.AppendLine("     \"speed\": " + ClipboardBE.F2S(lubEffect.Speed) + ",");
					b.AppendLine("     \"srcmode\": " + lubEffect.Srcmode + ",");
					b.AppendLine("     \"texture\": \"" + lubEffect.Texture.Replace("\\", "\\\\\\\\") + "\",");
					b.AppendLine("     \"type\": \"lubeffect\",");
					b.AppendLine("     \"zenable\": " + (lubEffect.Zenable ? 1 : 0) + "");
					b.AppendLine("    },");
					b.AppendLine("    null");
					break;
			}

			b.AppendLine("   ],");

			var pos = new Vector3(_obj.Position.X - _centerObjects.X, _obj.Position.Y - _centerObjects.Y, _obj.Position.Z - _centerObjects.Z);
			b.AppendLine("   \"relative_position\": [" + ClipboardBE.F2S(pos.X) + "," + ClipboardBE.F2S(pos.Y) + "," + ClipboardBE.F2S(pos.Z) + "],");

			switch(_obj.Type) {
				case RswObjectType.Model:
					b.AppendLine("   \"name\": \"" + model.Name.Replace("\\", "\\\\") + "\"");
					break;
				case RswObjectType.Sound:
					b.AppendLine("   \"name\": \"" + sound.Name.Replace("\\", "\\\\") + "\"");
					break;
				case RswObjectType.Light:
					b.AppendLine("   \"name\": \"" + light.Name.Replace("\\", "\\\\") + "\"");
					break;
				case RswObjectType.Effect:
					b.AppendLine("   \"name\": \"" + effect.Name.Replace("\\", "\\\\") + "\"");
					break;
			}

			b.AppendLine("  },");
		}
	}
}
