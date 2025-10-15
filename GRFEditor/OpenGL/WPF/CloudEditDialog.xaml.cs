using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ErrorManager;
using GRFEditor.OpenGL.MapRenderers;
using GrfToWpfBridge;
using OpenTK;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;

namespace GRFEditor.OpenGL.WPF {
	/// <summary>
	/// Interaction logic for CloudEditDialog.xaml
	/// </summary>
	public partial class CloudEditDialog : Window {
		private SkyMapRenderer _skyMapRenderer;
		private bool _eventSet = false;
		private bool _isInit = false;

		public CloudEditDialog() {
			InitializeComponent();

			WpfUtils.AddMouseInOutEffectsBox(_tbEnableStar, _tbEnableSkyMap);
			WindowStartupLocation = WindowStartupLocation.CenterOwner;

			_primary.SelectionChanged += (s, e) => {
				if (e.Source is TabControl && e.AddedItems.Count > 0) {
					TabItem newlySelectedItem = e.AddedItems[0] as TabItem;

					if (newlySelectedItem != null) {
						Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate (object param)
						{
							if (newlySelectedItem.IsKeyboardFocusWithin) {
								Keyboard.ClearFocus();
								newlySelectedItem.Focus();
							}
							return null;
						}), null);
					}
				}
			};
		}

		public void Init(OpenGLViewport viewport, bool first = true) {
			if (!_eventSet) {
				if (viewport._request.SkyMapRenderer == null) {
					return;
				}

				viewport.Loader.Loaded += request => {
					this.Dispatch(delegate {
						try {
							_primary.Items.Clear();

							if (request.IsMap)
								Init(viewport, false);
						}
						catch { }
					});
				};

				_eventSet = true;
			}

			_isInit = true;

			try {
				_skyMapRenderer = viewport._request.SkyMapRenderer;

				if (_skyMapRenderer.SkyMap == null) {
					_skyMapRenderer.SkyMap = new SkymapSettings();
				}

				_tbBg_Color.Color = Color.FromArgb((byte)(_skyMapRenderer.SkyMap.Bg_Color[3] * 255f), (byte)(_skyMapRenderer.SkyMap.Bg_Color[0] * 255f), (byte)(_skyMapRenderer.SkyMap.Bg_Color[1] * 255f), (byte)(_skyMapRenderer.SkyMap.Bg_Color[2] * 255f));
				_tbEnableStar.IsChecked = _skyMapRenderer.SkyMap.Star_Effect;
				_tbEnableStar.InvalidateVisual();
				_tbEnableSkyMap.IsChecked = _skyMapRenderer.IsValidSkyMap;
				
				foreach (var skyEffect in _skyMapRenderer.SkyMap.SkyEffects) {
					if (skyEffect.IsDeleted || skyEffect.IsStarEffect)
						continue;

					_addNewTab(skyEffect);
				}

				if (_primary.SelectedIndex < 0 && _primary.Items.Count > 0)
					_primary.SelectedIndex = 0;

				_btCopyToClipboard.Click += delegate {
					try {
						StringBuilder b = new StringBuilder();
						b.AppendLine("\t[\"" + Path.GetFileName(viewport._request.Resource) + ".rsw\"] = {");
						var skyMap = _skyMapRenderer.SkyMap;
						b.AppendLine("\t\tBG_Color = { " + (int)(skyMap.Bg_Color[0] * 255f) + ", " + (int)(skyMap.Bg_Color[1] * 255f) + ", " + (int)(skyMap.Bg_Color[2] * 255f) + " },");
						b.AppendLine("\t\tStar_Effect = " + (skyMap.Star_Effect ? "true" : "false") + ",");
						b.AppendLine("\t\tBG_Fog = true,");

						List<SkyEffect> oldSkyEffects = skyMap.SkyEffects.Where(p => p.OldCloudEffect > 0 && p.IsEnabled).ToList();

						if (oldSkyEffects.Count > 0) {
							b.AppendLine("\t\tOld_Cloud_Effect = { " + Methods.Aggregate(oldSkyEffects.Select(p => p.OldCloudEffect.ToString()).ToList(), ", ") + " },");
						}

						List<SkyEffect> newSkyEffects = skyMap.SkyEffects.Where(p => p.OldCloudEffect == 0 && p.IsEnabled).ToList();

						if (newSkyEffects.Count > 0) {
							b.AppendLine("\t\tCloud_Effect = {");

							for (int i = 0; i < newSkyEffects.Count; i++) {
								var skyEffect = newSkyEffects[i];

								b.AppendLine("\t\t\t[" + (i + 1) + "] = {");
								b.AppendLine("\t\t\t\tNum = " + skyEffect.Num.ToString() + ",");
								b.AppendLine("\t\t\t\tCullDist = 400,");
								b.AppendLine("\t\t\t\tColor = { " + (int)(skyEffect.Color[0] * 255f) + ", " + (int)(skyEffect.Color[1] * 255f) + ", " + (int)(skyEffect.Color[2] * 255f) + " },");
								b.AppendLine("\t\t\t\tSize = " + _toString(skyEffect.ShaderParameters.Size) + ",");
								b.AppendLine("\t\t\t\tSize_Extra = " + _toString(skyEffect.ShaderParameters.Size_Extra) + ",");
								b.AppendLine("\t\t\t\tAlpha_Inc_Time = " + _toString(skyEffect.ShaderParameters.Alpha_Inc_Time) + ",");
								b.AppendLine("\t\t\t\tAlpha_Inc_Time_Extra = " + _toString(skyEffect.ShaderParameters.Alpha_Inc_Time_Extra) + ",");
								b.AppendLine("\t\t\t\tAlpha_Inc_Speed = " + _toString(skyEffect.ShaderParameters.Alpha_Inc_Speed) + ",");
								b.AppendLine("\t\t\t\tAlpha_Dec_Time = " + _toString(skyEffect.ShaderParameters.Alpha_Dec_Time) + ",");
								b.AppendLine("\t\t\t\tAlpha_Dec_Time_Extra = " + _toString(skyEffect.ShaderParameters.Alpha_Dec_Time_Extra) + ",");
								b.AppendLine("\t\t\t\tAlpha_Dec_Speed = " + _toString(skyEffect.ShaderParameters.Alpha_Dec_Speed) + ",");
								b.AppendLine("\t\t\t\tHeight = " + _toString(skyEffect.ShaderParameters.Height) + ",");
								b.AppendLine("\t\t\t\tHeight_Extra = " + _toString(skyEffect.ShaderParameters.Height_Extra) + "");

								b.AppendLine("\t\t\t},");
							}

							b.AppendLine("\t\t}");
						}

						b.AppendLine("\t},");

						Clipboard.SetDataObject(b.ToString());
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
					}
				};

				if (first) {
					_tbBg_Color.ColorChanged += (s, c) => {
						if (_isInit) return;
						_skyMapRenderer.SkyMap.Bg_Color = new Vector4(c.R, c.G, c.B, c.A) / 255f;
					};
					_tbBg_Color.PreviewColorChanged += (s, c) => {
						if (_isInit) return;
						_skyMapRenderer.SkyMap.Bg_Color = new Vector4(c.R, c.G, c.B, c.A) / 255f;
					};

					_btAddOldSkyEffect.Click += delegate {
						_addSkyEffect(1);
					};

					_btAddNewSkyEffect.Click += delegate {
						_addSkyEffect(-1);
					};

					_tbEnableStar.Checked += delegate {
						if (_isInit) return;
						_skyMapRenderer.SkyMap.Star_Effect = true;
						if (_skyMapRenderer.SkyMap.SkyEffects.Count == 0 || !_skyMapRenderer.SkyMap.SkyEffects[0].IsStarEffect) {
							_skyMapRenderer.SkyMap.SkyEffects.Insert(0, SkyMapEffectTemplates.GetStarTemplate());
						}

						_skyMapRenderer.SkyMap.SkyEffects[0].IsEnabled = true;
					};
					_tbEnableStar.Unchecked += delegate {
						if (_isInit) return;
						_skyMapRenderer.SkyMap.Star_Effect = false;
						_skyMapRenderer.SkyMap.SkyEffects[0].IsEnabled = false;
					};

					_tbEnableSkyMap.Unchecked += delegate {
						if (_isInit) return;
						_skyMapRenderer.IsValidSkyMap = _tbEnableSkyMap.IsChecked == true;
					};
					_tbEnableSkyMap.Checked += delegate {
						if (_isInit) return;
						_skyMapRenderer.IsValidSkyMap = _tbEnableSkyMap.IsChecked == true;
					};
				}
			}
			finally {
				_isInit = false;
			}
		}

		private string _toString(float size) {
			string output = String.Format("{0:0.000}", size).Replace(",", ".").TrimEnd('0', '.');
			if (output == "")
				return "0";
			return output;
		}

		private void _addSkyEffect(int cloudType) {
			try {
				if (_tbEnableSkyMap.IsChecked == false) {
					_tbEnableSkyMap.IsChecked = true;
				}

				if (!_skyMapRenderer.IsValidSkyMap) {
					_skyMapRenderer.IsValidSkyMap = true;
				}

				SkyEffect effect = SkyMapEffectTemplates.GetTemplate(cloudType, 0);
				_skyMapRenderer.SkyMap.SkyEffects.Add(effect);
				_addNewTab(effect);

				if (_primary.Items.Count > 0)
					_primary.SelectedIndex = _primary.Items.Count - 1;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _addNewTab(SkyEffect skyEffect) {
			var tab = new CloudEditTab(skyEffect);
			tab.Header = skyEffect.IsStarEffect ? "Star" : "Sky";
			tab.Style = TryFindResource("TabItemSprite") as Style;

			tab.Close += (o, a) => _primary.Items.RemoveAt(Tabs().IndexOf(Tabs().First(p => ReferenceEquals(p, tab))));

			_primary.Items.Add(tab);
		}

		public List<CloudEditTab> Tabs() {
			return _primary.Items.Cast<TabItem>().Where(p => p is CloudEditTab).Cast<CloudEditTab>().ToList();
		}
	}
}
