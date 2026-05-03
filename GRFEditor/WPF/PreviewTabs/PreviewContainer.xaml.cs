using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ErrorManager;
using GRF;
using GRF.Core;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core.Services;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewContainer.xaml
	/// </summary>
	public partial class PreviewContainer : UserControl, IFolderPreviewTab {
		private readonly EditorMainWindow _editor;
		private readonly PreviewService _previewService;
		private TkPath _currentPath;
		private GrfHolder _grfData;
		private Func<bool> _isCancelRequired;
		private List<Grid> _typeGrids = new List<Grid>();

		public PreviewContainer(PreviewService previewService, EditorMainWindow editor) {
			_previewService = previewService;
			_editor = editor;

			InitializeComponent();

			_initEncodingUI();

			_typeGrids.Add(_gridThor);
			_typeGrids.Add(_gridGrf);
		}

		private void _initEncodingUI() {
			_comboBoxEncoding.Init(null,
				new TypeSetting<int>(v => GrfEditorConfiguration.EncodingCodepage = v, () => GrfEditorConfiguration.EncodingCodepage),
				new TypeSetting<Encoding>(v => EncodingService.DisplayEncoding = v, () => EncodingService.DisplayEncoding)
			);

			_comboBoxEncoding.EncodingChanged += (s, enc) => {
				if (!_editor.SetEncoding(enc.Encoding.CodePage)) {
					enc.Cancel = true;
				}
			};
		}

		#region IFolderPreviewTab Members

		public void Update() {
			var currentPath = _currentPath;
			_isCancelRequired = () => _previewService.QueueCount != 0 || _currentPath.GetFullPath() != currentPath.GetFullPath();

			Task.Run(delegate {
				try {
					_load();
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			});
		}

		public void Update(bool forceUpdate) {
			Update();
		}

		public void Load(GrfHolder grfData, TkPath currentPath) {
			_currentPath = currentPath;
			_grfData = grfData;

			if (IsVisible) {
				Update();
			}
		}

		#endregion

		private void _load() {
			if (_isCancelRequired()) return;

			this.Dispatch(delegate {
				_setupUI();
			});
		}

		private void _setupUI() {
			_comboBoxEncoding.Refresh();
			_textBoxSourceFileName.Text = _grfData.FileName;

			_typeGrids.ForEach(p => p.Visibility = Visibility.Hidden);

			switch (_grfData.FileName.GetExtension()) {
				case ".thor":
					_textBoxTargetGrf.Text = _grfData.GetAttachedProperty<string>("Thor.TargetGrf") ?? "";
					_comboBoxPatchMode.SelectedIndex = _grfData.GetAttachedProperty<bool>("Thor.UseGrfMerging") ? 1 : 0;
					_labelPropertyType.Content = "Thor type properties";
					_gridThor.Visibility = Visibility.Visible;
					break;
				case ".grf":
				case ".gpf":
				case ".rgz":
				default:
					_tbMagicHeader.Text = _grfData.Header.Magic;
					_buttonMagicReset.IsEnabled = !_isDefaultMagicHeader();
					_labelPropertyType.Content = "Grf type properties";
					_gridGrf.Visibility = Visibility.Visible;

					_comboBoxFormat.SelectionChanged -= _comboBoxFormat_SelectionChanged;
					_comboBoxFormat.SelectedItem = _grfData.Header.FormatView;
					_comboBoxFormat.SelectionChanged += _comboBoxFormat_SelectionChanged;
					break;
			}
		}

		#region Thor settings
		private void _comboBoxPatchMode_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_grfData == null) return;

			try {
				bool directoryPatchMode = _comboBoxPatchMode.SelectedIndex == 0;

				_grfData.Attached["Thor.UseGrfMerging"] = !directoryPatchMode;
				var visibility = directoryPatchMode ? Visibility.Collapsed : Visibility.Visible;
				_tbTarget.Visibility = visibility;
				_textBoxTargetGrf.Visibility = visibility;
				_tkInfo.Visibility = _textBoxTargetGrf.Text == "" && _textBoxTargetGrf.Visibility == Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _textBoxTargetGrf_TextChanged(object sender, TextChangedEventArgs e) {
			_grfData.Attached["Thor.TargetGrf"] = _textBoxTargetGrf.Text;
			_tkInfo.Visibility = _textBoxTargetGrf.Text == "" && _textBoxTargetGrf.Visibility == Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;
		}
		#endregion

		#region GRF settings
		private bool _isDefaultMagicHeader() {
			return _grfData.Header.Magic == _getDefaultMagicHeader();
		}

		private string _getDefaultMagicHeader() {
			return _grfData.Header.IsCompatibleWith(3, 0) ? GrfStrings.EventHorizon : GrfStrings.MasterOfMagic;
		}

		protected void _comboBoxFormat_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_comboBoxFormat.SelectedItem == null)
				return;

			try {
				var targetFormat = (GrfFormatView)_comboBoxFormat.SelectedItem;

				if (targetFormat == _grfData.Header.FormatView)
					return;

				_grfData.Commands.Begin();
				_grfData.Commands.ChangeVersion(targetFormat.Major, targetFormat.Minor);

				if (targetFormat == GrfFormatViews.Grf300) {
					if (_grfData.Header.Magic == GrfStrings.MasterOfMagic) {
						_grfData.Commands.ChangeHeader(GrfStrings.EventHorizon, _changeHeaderCallback);
					}
				}
				else {
					if (_grfData.Header.Magic == GrfStrings.EventHorizon) {
						_grfData.Commands.ChangeHeader(GrfStrings.MasterOfMagic, _changeHeaderCallback);
					}
				}
			}
			finally {
				_grfData.Commands.End();
			}
		}

		private void _buttonMagicEdit_Click(object sender, RoutedEventArgs e) {
			var input = WindowProvider.ShowWindow<MagicEditDialog>(new MagicEditDialog(_grfData.Header.Magic), WpfUtilities.TopWindow);

			if (input.DialogResult == true) {
				_grfData.Commands.ChangeHeader(input.OutputHeader, _changeHeaderCallback);
			}
		}

		private void _buttonMagicReset_Click(object sender, RoutedEventArgs e) {
			_grfData.Commands.ChangeHeader(_getDefaultMagicHeader(), _changeHeaderCallback);
		}

		private void _changeHeaderCallback(string magic, bool execute) {
			this.Dispatch(() => {
				_tbMagicHeader.Text = magic;
				_buttonMagicReset.IsEnabled = !_isDefaultMagicHeader();
			});
		}
		#endregion
	}
}