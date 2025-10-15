using OpenTK;

namespace GRFEditor.OpenGL.MapComponents {
	public class MapRendererOptions {
		public bool Water { get; set; }
		public bool Ground { get; set; }
		public bool Gat { get; set; }
		public bool Objects { get; set; }
		public bool AnimateMap { get; set; }
		public bool Lightmap { get; set; }
		public bool Shadowmap { get; set; }
		public bool ShowFps { get; set; }
		public bool ShowBlackTiles { get; set; }
		public bool LubEffect { get; set; }
		public bool ViewStickToGround { get; set; }
		public bool UseClientPov { get; set; }
		public bool RenderSkymapFeature { get; set; }
		public bool RenderingMap { get; set; }
		public bool SmoothCamera { get; set; }
		public int FpsCap { get; set; }
		public bool EnableFaceCulling { get; set; }
		public bool MinimapMode { get; set; }
		public bool MinimapWaterOverride { get; set; }
		public int ForceShader { get; set; }
		public double RotateSpeed { get; set; }
		public float GatAlpha { get; set; }
		public float GatZBias { get; set; }
		public bool ShowWireframeView { get; set; }
		public bool ShowPointView { get; set; }

		public Vector4 MinimapWaterColor = new Vector4(0, 0, 128, 144) / 255f;	// rgba
		public Vector4 MinimapWalkColor = new Vector4(0.5f, 0.5f, 0.5f, 1f);
		public Vector4 MinimapNonWalkColor = new Vector4(0, 0, 0, 0.4f);

		public MapRendererOptions() {
			Water = true;
			Ground = true;
			Gat = true;
			Objects = true;
			AnimateMap = true;
			Lightmap = true;
			Shadowmap = true;
			ShowFps = false;
			ShowBlackTiles = false;
			LubEffect = true;
			ViewStickToGround = true;
			UseClientPov = false;
			RenderSkymapFeature = true;
			SmoothCamera = true;
			FpsCap = 60;
			EnableFaceCulling = false;
			MinimapMode = false;
			MinimapWaterOverride = false;
			ShowWireframeView = false;
			ShowPointView = false;
			RotateSpeed = 6;
			GatAlpha = 0.4f;
			GatZBias = 0f;
		}
	}
}
