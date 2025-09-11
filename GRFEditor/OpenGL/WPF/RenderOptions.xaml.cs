using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.WPF.PreviewTabs;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles.ListView;
using Binder = GrfToWpfBridge.Binder;

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
			Binder.Bind(_checkBoxRenderGat, () => GrfEditorConfiguration.MapRenderRenderGat, v => GrfEditorConfiguration.MapRenderRenderGat = v, () => _previewRsm.Reload());

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
				_checkBoxRenderSkymap, _checkBoxSmoothCamera, _checkBoxUnlimitedFps, _checkBoxFaceCulling, _checkBoxFSAA, _checkBoxRenderGat);

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
			_checkBoxRenderGat.ToolTip = "Enable or disable the gat tile renderer. This will show walkable tiles (you can right-click the setting to change the transparency value).";

			_checkBoxRenderGat.MouseRightButtonUp += delegate {
				var dialog = new GatOptions();
				dialog.Load(viewport._request);
				_openDialog(dialog, _checkBoxRenderGat);
			};
		}

		public void Load(PreviewRsm previewRsm) {
			_previewRsm = previewRsm;
		}

		private void _openDialog(Window dialog, FrameworkElement button) {
			dialog.WindowStyle = WindowStyle.None;
			var content = dialog.Content;

			Border border = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };
			dialog.Content = null;
			border.Child = content as UIElement;
			dialog.Content = border;
			dialog.Owner = null;
			dialog.ShowInTaskbar = false;
			dialog.ResizeMode = ResizeMode.NoResize;
			dialog.SnapsToDevicePixels = true;

			EditorMainWindow.Instance.Activated += delegate {
				dialog.Close();
			};

			Point p = button.PointToScreen(new Point(0, 0));
			var par = WpfUtilities.FindParentControl<Window>(button);

			dialog.Loaded += delegate {
				button.IsEnabled = false;
				dialog.WindowStartupLocation = WindowStartupLocation.Manual;

				int dpiXI = (int)typeof(SystemParameters).GetProperty("DpiX", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null);
				double dpiX = dpiXI;
				double ratio = dpiX / 96;

				p.X /= ratio;
				p.Y /= ratio;

				// The dialog's position scales with the DPI
				dialog.Left = p.X;
				dialog.Top = p.Y + button.ActualHeight;

				if (dialog.Left < 0) {
					dialog.Left = 0;
				}

				if (dialog.Top + dialog.Height > SystemParameters.WorkArea.Bottom) {
					dialog.Top = p.Y - dialog.ActualHeight;
				}

				if (dialog.Left + dialog.Width > SystemParameters.WorkArea.Right) {
					dialog.Left = p.X - dialog.ActualWidth;
				}

				if (dialog.Top < 0) {
					dialog.Top = 0;
				}

				dialog.Owner = par;
			};

			dialog.Closed += delegate {
				button.IsEnabled = true;
			};

			dialog.Show();
		}
	}
}
