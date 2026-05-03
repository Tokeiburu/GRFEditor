using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ErrorManager;
using GRF;
using GRF.Core;
using GRF.GrfSystem;
using GRFEditor.ApplicationConfiguration;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using Utilities;
using Utilities.Services;
using Binder = GrfToWpfBridge.Binder;
using Path = System.IO.Path;

namespace GRFEditor.WPF {
	/// <summary>
	/// Interaction logic for SettingsDialog.xaml
	/// </summary>
	public partial class SettingsDialog : TkWindow {
		public static Func<Action<Brush>[]> GetBackgroundScrollViewers = null;
		private readonly EditorMainWindow _editor;
		protected GrfHolder _grfData;

		public SettingsDialog(GrfHolder grfData, EditorMainWindow editor)
			: base("Settings", "settings.ico") {
			_grfData = grfData;
			_editor = editor;

			InitializeComponent();

			UIPanelPreviewBackgroundPick(_qcsBackground);

			_initializeGeneralSettings();
			_initializeApplicationSettings();
			_initializeResourcesSettings();
		}

		private void _initializeGeneralSettings() {
			_comboBoxCompression.Init();
			_comboBoxEncryption.Init();
			_comboBoxWarningLevel.SelectedIndex = (int)Configuration.WarningLevel;
			_textBoxMaxThreads.Text = GrfEditorConfiguration.MaximumNumberOfThreads.ToString(CultureInfo.InvariantCulture);

			_comboBoxFormat.SelectedIndex = -1;

			if (_grfData.IsOpened) {
				_comboBoxFormat.SelectedItem = _grfData.Header.FormatView;
			}

			_comboBoxEncoding.Init(null, new TypeSetting<int>(v => GrfEditorConfiguration.EncodingCodepage = v, () => GrfEditorConfiguration.EncodingCodepage), new TypeSetting<Encoding>(v => EncodingService.DisplayEncoding = v, () => EncodingService.DisplayEncoding));

			WpfUtilities.AddMouseInOutHandEffect(_encodingImage);
			Binder.Bind(_cbOverrideExtractionPath, () => GrfEditorConfiguration.OverrideExtractionPath, delegate {
				_pbExtration.IsEnabled = GrfEditorConfiguration.OverrideExtractionPath;
			}, true);
			Binder.Bind(_pbExtration.TextBox, () => GrfEditorConfiguration.DefaultExtractingPath);
			Binder.Bind(_comboBoxStyles, () => GrfEditorConfiguration.ThemeIndex, v => _changeStyle(v));
		}

		private void _initializeApplicationSettings() {
			try {
				_tbConfig.Text = Path.GetFileName(GrfEditorConfiguration.ConfigAsker.ConfigFile);
				_tbPixelIndexation.Text = Methods.CanUseIndexed8 ? "Directly handled by GDI" : "Manually converted to Bgra32";
				_tbFramework.Text = RuntimeEnvironment.GetSystemVersion() + " - " + Methods.GetReadableRuntimeVersion();
			}
			catch {
			}

			_configSelect.Click += delegate {
				try {
					OpeningService.FileOrFolder(GrfEditorConfiguration.ConfigAsker.ConfigFile);
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			};
		}

		private void _initializeResourcesSettings() {
			_mViewer.SaveResourceMethod = v => GrfEditorConfiguration.Resources.SaveResources(v);
			_mViewer.LoadResourceMethod = () => GrfEditorConfiguration.Resources.LoadResources();
			GrfEditorConfiguration.Resources.Modified += () => _mViewer.LoadResourcesInfo();
			_mViewer.LoadResourcesInfo();
			_mViewer.CanDeleteMainGrf = false;
		}

		private void _changeStyle(int themeIndex) {
			try {
				GrfEditorConfiguration.ThemeIndex = themeIndex;
				Application.Current.Resources.MergedDictionaries.RemoveAt(Application.Current.Resources.MergedDictionaries.Count - 1);

				var path = "pack://application:,,,/" + Assembly.GetEntryAssembly().GetName().Name.Replace(" ", "%20") + ";component/WPF/Styles/";

				if (GrfEditorConfiguration.ThemeIndex == 0) {
					path += "StyleLightBlue.xaml";
				}
				else if (GrfEditorConfiguration.ThemeIndex == 1) {
					path += "StyleDark.xaml";
				}

				Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(path, UriKind.RelativeOrAbsolute) });
				ApplicationManager.OnThemeChanged();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		protected void _comboBoxWarningLevel_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			try {
				Configuration.WarningLevel = (ErrorLevel)_comboBoxWarningLevel.SelectedIndex;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		protected void _comboBoxFormat_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_comboBoxFormat.SelectedItem == null || !_grfData.IsOpened)
				return;

			try {
				var targetFormat = (GrfFormatView)_comboBoxFormat.SelectedItem;

				if (targetFormat == _grfData.Header.FormatView)
					return;

				_grfData.Commands.Begin();
				_grfData.Commands.ChangeVersion(targetFormat.Major, targetFormat.Minor);

				if (targetFormat == GrfFormatViews.Grf300) {
					if (_grfData.Header.Magic == GrfStrings.MasterOfMagic) {
						_grfData.Commands.ChangeHeader(GrfStrings.EventHorizon);
					}
				}
				else {
					if (_grfData.Header.Magic == GrfStrings.EventHorizon) {
						_grfData.Commands.ChangeHeader(GrfStrings.MasterOfMagic);
					}
				}
			}
			finally {
				_grfData.Commands.End();
			}
		}

		protected void _textBoxMaxThreads_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				Settings.MaximumNumberOfThreads = GrfEditorConfiguration.MaximumNumberOfThreads = Int32.Parse(((TextBox)sender).Text);
			}
			catch {
			}
		}

		private void _buttonTreeExpandSpecific_Click(object sender, RoutedEventArgs e) {
			List<string> paths = Methods.StringToList(GrfEditorConfiguration.TreeBehaviorSpecificFolders);

			InputDialog dialog = new InputDialog("Enter the paths to expand for GRFs.", "Tree expansion", paths.Aggregate((a, b) => a + "\r\n" + b), false, false);
			dialog.Owner = this;
			dialog.TextBoxInput.AcceptsReturn = true;
			dialog.TextBoxInput.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			dialog.TextBoxInput.TextWrapping = TextWrapping.Wrap;
			dialog.TextBoxInput.Height = 200;
			dialog.TextBoxInput.MinHeight = 200;
			dialog.TextBoxInput.MaxHeight = 200;
			dialog.TextBoxInput.VerticalContentAlignment = VerticalAlignment.Top;

			if (dialog.ShowDialog() == true) {
				GrfEditorConfiguration.TreeBehaviorSpecificFolders = dialog.Input.Replace("\r", "").Replace("\n\n", "\n").Replace("\n", ",");
			}
		}

		private void _encodingImage_MouseDown(object sender, MouseButtonEventArgs e) {
			ErrorHandler.HandleException("If you're not sure about the encoding, just select Default. The encoding more or less matters, "
										 + "the only difference between encodings is how the file names will be shown. The actual data will not be affected by "
										 + "this.\n\nHowever, if you're working with Korean based file names, you should select the Korean encoding. The application will "
										 + "automatically convert the file names and if the file already exists, it will replace it (so be careful!).",
										 ErrorLevel.NotSpecified);
		}

		public static void UIPanelPreviewBackgroundPick(QuickColorSelector selector) {
			selector.PreviewColorChanged += _onSelectorOnPreviewColorChanged;
			selector.ColorChanged += _onSelectorOnPreviewColorChanged;
		}

		private static void _onSelectorOnPreviewColorChanged(object sender, Color value) {
			if (GrfEditorConfiguration.UIPanelPreviewBackground is SolidColorBrush) {
				if (((SolidColorBrush)GrfEditorConfiguration.UIPanelPreviewBackground).Color == value)
					return;
			}

			GrfEditorConfiguration.UIPanelPreviewBackground = new SolidColorBrush(value);

			if (GetBackgroundScrollViewers != null) {
				var scrollViewers = GetBackgroundScrollViewers();

				foreach (var viewer in scrollViewers) {
					viewer(GrfEditorConfiguration.UIPanelPreviewBackground);
				}
			}
		}

		public static void SetImagePreviewEvents(Rectangle previewPanel) {
			WpfUtilities.AddMouseInOutHandEffect(previewPanel);
		}

		private void _comboBoxEncoding_EncodingChanged(object sender, GrfToWpfBridge.Application.EncodingArgs enc) {
			if (!_editor.SetEncoding(enc.Encoding.CodePage)) {
				enc.Cancel = true;
			}
		}
	}
}