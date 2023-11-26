using System.Windows;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.OpenGL.MapGLGroup;
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
			InitializeComponent();

			Binder.Bind(_checkBoxRenderWater, () => GrfEditorConfiguration.MapRendererWater, v => GrfEditorConfiguration.MapRendererWater = v, () => _previewRsm.Reload(false));
			Binder.Bind(_checkBoxRenderGround, () => GrfEditorConfiguration.MapRendererGround, v => GrfEditorConfiguration.MapRendererGround = v, () => _previewRsm.Reload(false));
			Binder.Bind(_checkBoxRenderObjects, () => GrfEditorConfiguration.MapRendererObjects, v => GrfEditorConfiguration.MapRendererObjects = v, () => _previewRsm.Reload(false));
			Binder.Bind(_checkBoxAnimateMap, () => GrfEditorConfiguration.MapRendererAnimateMap, v => GrfEditorConfiguration.MapRendererAnimateMap = v, () => _previewRsm.Reload());
			Binder.Bind(_checkBoxLightmap, () => GrfEditorConfiguration.MapRendererLightmap, v => GrfEditorConfiguration.MapRendererLightmap = v, () => _previewRsm.Reload(false, true));
			Binder.Bind(_checkBoxShadowmap, () => GrfEditorConfiguration.MapRendererShadowmap, v => GrfEditorConfiguration.MapRendererShadowmap = v, () => _previewRsm.Reload(false, true));
			Binder.Bind(_checkBoxFps, () => GrfEditorConfiguration.MapRendererShowFps, v => GrfEditorConfiguration.MapRendererShowFps = v, () => _previewRsm.Reload(false, false, false));
			Binder.Bind(_checkBoxTipeUp, () => GrfEditorConfiguration.MapRendererTileUp, v => GrfEditorConfiguration.MapRendererTileUp = v, () => _previewRsm.Reload());
			Binder.Bind(_checkBoxRenderLub, () => GrfEditorConfiguration.MapRendererRenderLub, v => GrfEditorConfiguration.MapRendererRenderLub = v, () => _previewRsm.Reload(false));
			Binder.Bind(_checkBoxStickToGround, () => GrfEditorConfiguration.MapRendererStickToGround, v => GrfEditorConfiguration.MapRendererStickToGround = v, () => _previewRsm.Reload(false));
			Binder.Bind(_checkBoxRenderSkymap, () => GrfEditorConfiguration.MapRenderSkyMap, v => GrfEditorConfiguration.MapRenderSkyMap = v, () => _previewRsm.Reload(false));

			_checkBoxClientPerspective.IsChecked = MapRenderer.RenderOptions.UseClientPov;
			_checkBoxClientPerspective.Checked += delegate {
				MapRenderer.RenderOptions.UseClientPov = true;
			};
			_checkBoxClientPerspective.Unchecked += delegate {
				MapRenderer.RenderOptions.UseClientPov = false;
			};

			//Binder.Bind(_checkBoxClientPerspective, () => GrfEditorConfiguration.MapRendererClientPov, v => GrfEditorConfiguration.MapRendererClientPov = v, () => _previewRsm.Reload(false));

			WpfUtils.AddMouseInOutEffectsBox(_checkBoxRenderWater, _checkBoxRenderWater, _checkBoxRenderGround, _checkBoxRenderObjects, _checkBoxAnimateMap, _checkBoxLightmap, _checkBoxShadowmap, _checkBoxFps, _checkBoxTipeUp, _checkBoxRenderLub, _checkBoxStickToGround, _checkBoxClientPerspective, _checkBoxRenderSkymap);
		}

		public void Load(PreviewRsm previewRsm) {
			_previewRsm = previewRsm;
		}
	}
}
