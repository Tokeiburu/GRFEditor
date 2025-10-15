using GRFEditor.OpenGL.MapRenderers;
using GrfToWpfBridge;
using OpenTK;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TokeiLibrary;
using static GRFEditor.Tools.SpriteEditor.SpriteEditorTab;

namespace GRFEditor.OpenGL.WPF {
	/// <summary>
	/// Interaction logic for CloudEditTab.xaml
	/// </summary>
	public partial class CloudEditTab : TabItem {
		private readonly SkyEffect _skyEffect;

		public CloudEditTab() {
			InitializeComponent();
		}

		public CloudEditTab(SkyEffect skyEffect) {
			InitializeComponent();

			_skyEffect = skyEffect;

			if (skyEffect.OldCloudEffect == 0) {
				_tbAmount.TextNoEvent = skyEffect.Num.ToString();
				_tbAmount.TextChanged += delegate {
					int value = _tbAmount.GetInt();

					skyEffect.Num = value;
					skyEffect.IsModified = true;
				};

				_tbColor.Color = Color.FromArgb(255, (byte)(skyEffect.Color[0] * 255f), (byte)(skyEffect.Color[1] * 255f), (byte)(skyEffect.Color[2] * 255f));
				_tbColor.ColorChanged += (s, c) => {
					skyEffect.Color = new Vector3(c.R, c.G, c.B) / 255f;
				};
				_tbColor.PreviewColorChanged += (s, c) => {
					skyEffect.Color = new Vector3(c.R, c.G, c.B) / 255f;
				};

				_tbExpand_Rate.Multiplier = 0.01f;
				_tbAlpha_Inc_Time.Multiplier = 1f;
				_tbAlpha_Inc_Time_Extra.Multiplier = 1f;
				_tbAlpha_Inc_Speed.Multiplier = 0.01f;
				_tbAlpha_Dec_Time.Multiplier = 1f;
				_tbAlpha_Dec_Time_Extra.Multiplier = 1f;
				_tbAlpha_Dec_Speed.Multiplier = 0.01f;

				_bind(_tbSize, () => _skyEffect.ShaderParameters.Size, v => _skyEffect.ShaderParameters.Size = v);
				_bind(_tbSize_Extra, () => _skyEffect.ShaderParameters.Size_Extra, v => _skyEffect.ShaderParameters.Size_Extra = v);
				_bind(_tbExpand_Rate, () => _skyEffect.ShaderParameters.Expand_Rate, v => _skyEffect.ShaderParameters.Expand_Rate = v);
				_bind(_tbAlpha_Inc_Time, () => _skyEffect.ShaderParameters.Alpha_Inc_Time, v => _skyEffect.ShaderParameters.Alpha_Inc_Time = v);
				_bind(_tbAlpha_Inc_Time_Extra, () => _skyEffect.ShaderParameters.Alpha_Inc_Time_Extra, v => _skyEffect.ShaderParameters.Alpha_Inc_Time_Extra = v);
				_bind(_tbAlpha_Inc_Speed, () => _skyEffect.ShaderParameters.Alpha_Inc_Speed, v => _skyEffect.ShaderParameters.Alpha_Inc_Speed = v);
				_bind(_tbAlpha_Dec_Time, () => _skyEffect.ShaderParameters.Alpha_Dec_Time, v => _skyEffect.ShaderParameters.Alpha_Dec_Time = v);
				_bind(_tbAlpha_Dec_Time_Extra, () => _skyEffect.ShaderParameters.Alpha_Dec_Time_Extra, v => _skyEffect.ShaderParameters.Alpha_Dec_Time_Extra = v);
				_bind(_tbAlpha_Dec_Speed, () => _skyEffect.ShaderParameters.Alpha_Dec_Speed, v => _skyEffect.ShaderParameters.Alpha_Dec_Speed = v);
				_bind(_tbHeight, () => _skyEffect.ShaderParameters.Height, v => _skyEffect.ShaderParameters.Height = v);
				_bind(_tbHeight_Extra, () => _skyEffect.ShaderParameters.Height_Extra, v => _skyEffect.ShaderParameters.Height_Extra = v);

				_setupTooltip(_tbAmount, "Amount of clouds visible at any time. If negative, it will use a default value.");
				_setupTooltip(_tbColor, "Cloud color overlay. Each color channel is multiplied by this color, so if you pick red (255, 0, 0), all color channels other than red will be 0.");
				_setupTooltip(_tbSize, "The cloud size, multiplied by 2. If you put 5, the cloud size will be 10x10, which is equivalent to 2 gat cells or 1 ground cube.\nfloat size = Size + Rand(Size_Extra)");
				_setupTooltip(_tbSize_Extra, "Adds a random value to Size upton the creation of the cloud.\nfloat size = Size + Rand(Size_Extra)");
				_setupTooltip(_tbAlpha_Inc_Time, "The minimum amount of time the cloud can take to stay visible, divided by 100. If you put 500 for 5 seconds, the cloud will stay visible for 5 seconds before it starts fading.\nfloat alpha = (Alpha_Inc_Time + Rand(Alpha_Inc_Time_Extra)) * Alpha_Inc_Speed / 2.55\n\nNote: all time values are multiplied by 1.6 (500 for 5 seconds would actually be 8 seconds).");
				_setupTooltip(_tbAlpha_Inc_Time_Extra, "Adds a random extra value to Alpha_Inc_Time upon the creation of the cloud.\nfloat alpha = (Alpha_Inc_Time + Rand(Alpha_Inc_Time_Extra)) * Alpha_Inc_Speed / 2.55");
				_setupTooltip(_tbAlpha_Inc_Speed, "The amount of alpha value gain per second. If set to 1, then it will take 2.55 seconds to reach the maximum alpha value (2.55 is 255 alpha channel).\nSo that means your Alpha_Inc_Time should be greater or equal to 255 if you want your cloud to be fully visible.");
				_setupTooltip(_tbAlpha_Dec_Time, "If greater than Alpha_Inc_Time + extra, then this is the time the cloud will start to fade.\nIf set Alpha_Inc_Time is set to 500 and Alpha_Dec_Time is set to 600, the cloud will appear for 5 seconds,\nstay like that for 1 second, and then start disappearing afterwards.\n\nNote: all time values are multiplied by 1.6 (500 for 5 seconds would actually be 8 seconds).");
				_setupTooltip(_tbAlpha_Dec_Time_Extra, "Adds a random extra value to Alpha_Dec_Time upon the creation of the cloud.");
				_setupTooltip(_tbAlpha_Dec_Speed, "The amount of alpha value loss per second. Determines how quickly the cloud will fade; after reaching 0 alpha, the cloud will be 'destroyed' and ready to be created again.");
				_setupTooltip(_tbExpand_Rate, "Makes the cloud increase and decrease in size. The formula goes as such:\nFor 3 seconds, the cloud size will increase from Size to Size * (1 + Expand_Rate).\nAnd then for another 3 seconds, the cloud will decrease from Size to Size * (1 - Expand_Rate).");
				_setupTooltip(_tbHeight, "The Y position of the cloud (reversed). This value matches with the ground height.");
				_setupTooltip(_tbHeight_Extra, "Adds a random extra value to Height upon the creation of the cloud.");
			}
			else {
				_labelCloudType.Visibility = System.Windows.Visibility.Visible;
				_tbCloudType.Visibility = System.Windows.Visibility.Visible;

				foreach (var child in _grid.Children.OfType<FrameworkElement>()) {
					if (child.Tag == null || child.Tag.ToString() != "old")
						child.Visibility = Visibility.Collapsed;
				}

				_tbCloudType.TextNoEvent = _skyEffect.OldCloudEffect.ToString();
				_tbCloudType.TextChanged += delegate {
					int value = _tbCloudType.GetInt();
					_skyEffect.ImportOld(value);
					_skyEffect.OldCloudEffect = value;
					_skyEffect.IsModified = true;
				};

				_setupTooltip(_tbCloudType, 
					"1 = White clouds, -40 height\n" +
					"2 = White clouds, 0 height\n" +
					"3 = Einbech fog clouds, above ground\n" +
					"4 = White clouds, -40 height, floats to right\n" +
					"5 = Dark red clouds, -20 height\n" +
					"7 = Black clouds, -40 height\n" +
					"8 = Light pink clouds, -40 height\n" +
					"9 = Stars, -85 height\n" +
					"10 = Dark red clouds, -30 height\n" +
					"11-14 = Multi-colored clouds, -30 height\n" +
					"15 = Stars, +40 height");
			}

			Binder.Bind(_cbEnabled, () => _skyEffect.IsEnabled, v => _skyEffect.IsEnabled = v);
			
			Loaded += delegate {
				var border = WpfUtilities.FindChild<Border>(this, "_borderButton");

				if (border != null) {
					border.Visibility = System.Windows.Visibility.Collapsed;
					//border.PreviewMouseLeftButtonDown += (e, a) => { a.Handled = true; };
					//border.PreviewMouseLeftButtonUp += (e, a) => { OnClose(); };
				}

				var border2 = WpfUtilities.FindChild<Border>(this, "Border");

				if (border2 != null) {
					border2.PreviewMouseDown += delegate {
						if (Mouse.MiddleButton == MouseButtonState.Pressed) {
							OnClose();
						}
					};

					border2.ContextMenu = new ContextMenu();

					var menuItem = new MenuItem { Header = "Delete" };
					menuItem.Click += delegate { OnClose(); };
					border2.ContextMenu.Items.Add(menuItem);

					menuItem = new MenuItem { Header = "Delete all but this" };
					menuItem.Click += delegate {
						var cloudEdit = WpfUtilities.FindParentControl<CloudEditDialog>(this);

						if (cloudEdit != null) {
							cloudEdit.Tabs().ForEach(tab => {
								if (tab != this) {
									tab.OnClose();
								}
							});
						}
					};
					border2.ContextMenu.Items.Add(menuItem);

					menuItem = new MenuItem { Header = "Delete all" };
					menuItem.Click += delegate {
						var cloudEdit = WpfUtilities.FindParentControl<CloudEditDialog>(this);

						if (cloudEdit != null) {
							cloudEdit.Tabs().ForEach(tab => {
								tab.OnClose();
							});
						}
					};
					border2.ContextMenu.Items.Add(menuItem);
				}
			};
		}

		private void _setupTooltip(UIElement control, string toolTip) {
			int row = Grid.GetRow(control);

			foreach (UIElement child in _grid.Children) {
				if (Grid.GetRow(child) == row && Grid.GetColumn(child) == 0) {
					if (child is TextBlock fb) {
						fb.MouseEnter += delegate {
							Mouse.OverrideCursor = Cursors.Hand;
							fb.Foreground = Application.Current.Resources["MouseOverTextBrush"] as SolidColorBrush;
							fb.SetValue(TextBlock.TextDecorationsProperty, TextDecorations.Underline);
						};

						ToolTip tooltip = new ToolTip { Content = toolTip };

						fb.ToolTip = tooltip;
						ToolTipService.SetBetweenShowDelay(fb, 30000);

						fb.MouseLeave += delegate {
							Mouse.OverrideCursor = null;
							fb.Foreground = Application.Current.Resources["TextForeground"] as SolidColorBrush;
							fb.SetValue(TextBlock.TextDecorationsProperty, null);
							tooltip.IsOpen = false;
						};

						fb.MouseLeftButtonUp += delegate {
							tooltip.IsOpen = true;
						};
					}
				}
			}
		}

		private void _bind(FloatTextBoxEdit tb, Func<float> get, Func<float, float> set) {
			tb.TextNoEvent = get().ToString();
			tb.TextChanged += delegate {
				float value = tb.GetFloat();
				set(value);
			};
		}

		public event TabEventHandler Close;

		public void OnClose() {
			_skyEffect.IsDeleted = true;

			foreach (var subEffect in _skyEffect.SubEffects) {
				subEffect.IsDeleted = true;
			}

			Close?.Invoke(this, "");
		}
	}
}
