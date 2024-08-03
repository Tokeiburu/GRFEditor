using System.Windows;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.WPF.PreviewTabs;
using GrfToWpfBridge;
using TokeiLibrary.WPF.Styles.ListView;

namespace GRFEditor.OpenGL.WPF {
	/// <summary>
	/// Interaction logic for RenderOptions.xaml
	/// </summary>
	public partial class RenderOptions : Window {
		private PreviewRsm _previewRsm;

		public RenderOptions() {
		}

		public RenderOptions(OpenGLViewport viewport) {
			InitializeComponent();

			Binder.Bind(_checkBoxRenderWater, () => GrfEditorConfiguration.MapRendererWater, v => GrfEditorConfiguration.MapRendererWater = v, () => _previewRsm.Reload(false));
			Binder.Bind(_checkBoxRenderGround, () => GrfEditorConfiguration.MapRendererGround, v => GrfEditorConfiguration.MapRendererGround = v, () => _previewRsm.Reload(false));
			Binder.Bind(_checkBoxRenderObjects, () => GrfEditorConfiguration.MapRendererObjects, v => GrfEditorConfiguration.MapRendererObjects = v, () => _previewRsm.Reload(false));
			Binder.Bind(_checkBoxAnimateMap, () => GrfEditorConfiguration.MapRendererAnimateMap, v => GrfEditorConfiguration.MapRendererAnimateMap = v, () => _previewRsm.Reload());
			Binder.Bind(_checkBoxLightmap, () => GrfEditorConfiguration.MapRendererLightmap, v => GrfEditorConfiguration.MapRendererLightmap = v, () => _previewRsm.Reload(false, true));
			Binder.Bind(_checkBoxShadowmap, () => GrfEditorConfiguration.MapRendererShadowmap, v => GrfEditorConfiguration.MapRendererShadowmap = v, () => _previewRsm.Reload(false, true));
			Binder.Bind(_checkBoxFps, () => GrfEditorConfiguration.MapRendererShowFps, v => GrfEditorConfiguration.MapRendererShowFps = v, () => _previewRsm.Reload(false, false, false));
			Binder.Bind(_checkBoxTileUp, () => GrfEditorConfiguration.MapRendererTileUp, v => GrfEditorConfiguration.MapRendererTileUp = v, () => _previewRsm.Reload());
			Binder.Bind(_checkBoxRenderLub, () => GrfEditorConfiguration.MapRendererRenderLub, v => GrfEditorConfiguration.MapRendererRenderLub = v, () => _previewRsm.Reload(false));
			Binder.Bind(_checkBoxStickToGround, () => GrfEditorConfiguration.MapRendererStickToGround, v => GrfEditorConfiguration.MapRendererStickToGround = v, () => _previewRsm.Reload(false));
			Binder.Bind(_checkBoxRenderSkymap, () => GrfEditorConfiguration.MapRenderSkyMap, v => GrfEditorConfiguration.MapRenderSkyMap = v, () => _previewRsm.Reload(false));
			Binder.Bind(_checkBoxSmoothCamera, () => GrfEditorConfiguration.MapRenderSmoothCamera, v => GrfEditorConfiguration.MapRenderSmoothCamera = v, () => _previewRsm.Reload(false));
			Binder.Bind(_checkBoxUnlimitedFps, () => GrfEditorConfiguration.MapRenderUnlimitedFps, v => GrfEditorConfiguration.MapRenderUnlimitedFps = v, () => _previewRsm.Reload(false));
			Binder.Bind(_checkBoxFaceCulling, () => GrfEditorConfiguration.MapRenderEnableFaceCulling, v => GrfEditorConfiguration.MapRenderEnableFaceCulling = v, () => _previewRsm.Reload(false, true));
			Binder.Bind(_checkBoxFSAA, () => GrfEditorConfiguration.MapRenderEnableFSAA, v => GrfEditorConfiguration.MapRenderEnableFSAA = v, () => _previewRsm.ReloadViewport());

			_checkBoxClientPerspective.IsChecked = viewport.RenderOptions.UseClientPov;
			_checkBoxClientPerspective.Checked += delegate {
				viewport.RenderOptions.UseClientPov = true;
			};
			_checkBoxClientPerspective.Unchecked += delegate {
				viewport.RenderOptions.UseClientPov = false;
			};

			//Binder.Bind(_checkBoxClientPerspective, () => GrfEditorConfiguration.MapRendererClientPov, v => GrfEditorConfiguration.MapRendererClientPov = v, () => _previewRsm.Reload(false));

			WpfUtils.AddMouseInOutEffectsBox(
				_checkBoxRenderWater, _checkBoxRenderWater, _checkBoxRenderGround, _checkBoxRenderObjects, _checkBoxAnimateMap, _checkBoxLightmap, 
				_checkBoxShadowmap, _checkBoxFps, _checkBoxTileUp, _checkBoxRenderLub, _checkBoxStickToGround, _checkBoxClientPerspective,
				_checkBoxRenderSkymap, _checkBoxSmoothCamera, _checkBoxUnlimitedFps, _checkBoxFaceCulling, _checkBoxFSAA);

			_checkBoxRenderWater.ToolTip = "Enable or disable the water renderer.";
			_checkBoxRenderGround.ToolTip = "Enable or disable the ground renderer (GND).";
			_checkBoxRenderObjects.ToolTip = "Enable or disable the models renderer.";
			_checkBoxAnimateMap.ToolTip = "Animates the models in the map. Disabling this option will improve your FPS but will take slightly longer to load.";
			_checkBoxLightmap.ToolTip = "Enable or disable the lightmap (this also affects the colormap).";
			_checkBoxShadowmap.ToolTip = "Enable or disable the shadowmap.";
			_checkBoxFps.ToolTip = "Show the FPS counter on the top right of the map. The counter refreshes every 200 ms.";
			_checkBoxTileUp.ToolTip = "Renders the TileUp tiles of cubes that aren't set as black tiles.";
			_checkBoxRenderLub.ToolTip = "Enable or disable the lub renderer. This setting will also impact the skymap.";
			_checkBoxStickToGround.ToolTip = "When moving in the map with the right-mouse button, this will adjust the height of the camera to match the ground.";
			_checkBoxRenderSkymap.ToolTip = "Enable or disable the sky map rendering.";
			_checkBoxSmoothCamera.ToolTip = "Enable or disable smooth camera movements.";
			_checkBoxUnlimitedFps.ToolTip = "Removes the 60 fps cap. Be warned, there is truly no limit and some people may experience a graphic driver crash.";
			_checkBoxFaceCulling.ToolTip = "Enable or disable face culling. Face culling will not render the back of the triangles (unless the models use two-side mode). The client does this by default.";
			_checkBoxClientPerspective.ToolTip = "Simulate the client's perspective. Though, this is not entirely accurate either.";
			_checkBoxFSAA.ToolTip = "Enable or disable anti-aliasing. This basically makes the edges less rough/pixalated.";
		}

		public void Load(PreviewRsm previewRsm) {
			_previewRsm = previewRsm;
		}
	}
}
