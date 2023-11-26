using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace GRFEditor.Core.MapRenderer {
	public class MapTile {
		public Vector2 V1;
		public Vector2 V2;
		public Vector2 V3;
		public Vector2 V4;

		public List<Vector2> TexCoords = new List<Vector2>();

		public MapTile() {
			TexCoords.Add(V1);
			TexCoords.Add(V2);
			TexCoords.Add(V4);
			TexCoords.Add(V3);
		}
	}
}
