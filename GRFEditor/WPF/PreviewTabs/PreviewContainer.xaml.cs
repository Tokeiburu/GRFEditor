using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
		private readonly Dictionary<string, Grid> _grids = new Dictionary<string, Grid>();
		private readonly object _lock = new object();
		private readonly Queue<PreviewItem> _previewItems;
		private TkPath _currentPath;
		private GrfHolder _grfData;

		public PreviewContainer(Queue<PreviewItem> previewItems, EditorMainWindow editor) {
			_previewItems = previewItems;
			_editor = editor;

			InitializeComponent();

			_comboBoxEncoding.Init(null,
			                       new TypeSetting<int>(v => GrfEditorConfiguration.EncodingCodepage = v, () => GrfEditorConfiguration.EncodingCodepage),
			                       new TypeSetting<Encoding>(v => EncodingService.DisplayEncoding = v, () => EncodingService.DisplayEncoding)
				);

			_comboBoxEncoding.EncodingChanged += (s, enc) => {
				if (!_editor.SetEncoding(enc.Encoding.CodePage)) {
					enc.Cancel = true;
				}
			};

			_comboBoxPatchMode.SelectionChanged += _comboBoxPatchMode_SelectionChanged;
			_textBoxTargetGrf.TextChanged += _textBoxTargetGrf_TextChanged;
			
			_grids[".thor"] = _gridThor;
			_grids[".grf"] = _gridGrf;
		}

		#region IFolderPreviewTab Members

		public void Update() {
			Thread thread = new Thread(() => _load(_currentPath)) { Name = "GrfEditor - Preview container thread" };
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
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

		private void _load(TkPath currentSearch) {
			try {
				lock (_lock) {
					if (_previewItems.Count != 0 || currentSearch.GetFullPath() != _currentPath.GetFullPath()) return;

					_comboBoxEncoding.Dispatch(p => p.Refresh());
					_textBoxSourceFileName.Dispatch(p => p.Text = _grfData.FileName);

					if (_grfData.FileName.GetExtension() == ".thor") {
						_setTargetGrf();
						_selectPatchMode();
					}
					else {
						_tbMagicHeader.Dispatch(p => _tbMagicHeader.Text = _grfData.Header.Magic);
						_tbMagicHeader.Dispatch(p => _buttonMagicReset.IsEnabled = (_tbMagicHeader.Text != (_grfData.Header.IsCompatibleWith(3, 0) ? GrfStrings.EventHorizon : GrfStrings.MasterOfMagic)));
					}

					_setVisible();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _selectPatchMode() {
			_comboBoxPatchMode.Dispatch(delegate { _comboBoxPatchMode.SelectedIndex = _grfData.GetAttachedProperty<bool>("Thor.UseGrfMerging") ? 1 : 0; });
		}

		private void _comboBoxPatchMode_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_grfData == null) return;

			try {
				_grfData.Attached["Thor.UseGrfMerging"] = _comboBoxPatchMode.SelectedIndex == 1;

				if (_comboBoxPatchMode.SelectedIndex == 0) {
					_tbTarget.Visibility = Visibility.Collapsed;
					_textBoxTargetGrf.Visibility = Visibility.Collapsed;
				}
				else {
					_tbTarget.Visibility = Visibility.Visible;
					_textBoxTargetGrf.Visibility = Visibility.Visible;
				}

				_tkInfo.Visibility = _textBoxTargetGrf.Text == "" && _textBoxTargetGrf.Visibility == Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _setTargetGrf() {
			_textBoxTargetGrf.Dispatch(delegate { _textBoxTargetGrf.Text = (string) (_grfData.Attached["Thor.TargetGrf"] ?? ""); });
		}

		private void _textBoxTargetGrf_TextChanged(object sender, TextChangedEventArgs e) {
			_grfData.Attached["Thor.TargetGrf"] = _textBoxTargetGrf.Text;
			_tkInfo.Visibility = _textBoxTargetGrf.Text == "" && _textBoxTargetGrf.Visibility == Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;

			//if (_textBoxTargetGrf.Text != "") {
			//    _labelFind.Visibility = Visibility.Hidden;
			//}
		}

		private void _setVisible() {
			this.Dispatch(delegate {
				_grids.Values.ToList().ForEach(p => p.Visibility = Visibility.Collapsed);

				if (_grfData.FileName.GetExtension() == ".thor") {
					_labelPropetyType.Content = "Thor type properties";
					_grids[".thor"].Visibility = Visibility.Visible;
				}
				else {
					_labelPropetyType.Content = "Grf type properties";
					_grids[".grf"].Visibility = Visibility.Visible;
					_selectVersion();
				}
			});
		}

		private void _selectVersion() {
			_comboBoxFormat.Dispatch(delegate {
				_comboBoxFormat.SelectionChanged -= _comboBoxFormat_SelectionChanged;

				if (_grfData.Header.Is(3, 0)) {
					_comboBoxFormat.SelectedIndex = 0;
				}
				else if (_grfData.Header.Is(2, 0)) {
					_comboBoxFormat.SelectedIndex = 1;
				}
				else if (_grfData.Header.Is(1, 3)) {
					_comboBoxFormat.SelectedIndex = 2;
				}
				else if (_grfData.Header.Is(1, 2)) {
					_comboBoxFormat.SelectedIndex = 3;
				}

				_comboBoxFormat.SelectionChanged += _comboBoxFormat_SelectionChanged;
			});
		}

		protected void _comboBoxFormat_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_comboBoxFormat != null) {
				try {
					_grfData.Commands.Begin();

					switch(_comboBoxFormat.SelectedIndex) {
						case 0:
							_grfData.Commands.ChangeVersion(3, 0);

							if (_grfData.Header.Magic == GrfStrings.MasterOfMagic) {
								_grfData.Commands.ChangeHeader(GrfStrings.EventHorizon, _changeHeaderCallback);
							}
							break;
						case 1:
							_grfData.Commands.ChangeVersion(2, 0);

							if (_grfData.Header.Magic == GrfStrings.EventHorizon) {
								_grfData.Commands.ChangeHeader(GrfStrings.MasterOfMagic, _changeHeaderCallback);
							}
							break;
						case 2:
							_grfData.Commands.ChangeVersion(1, 3);

							if (_grfData.Header.Magic == GrfStrings.EventHorizon) {
								_grfData.Commands.ChangeHeader(GrfStrings.MasterOfMagic, _changeHeaderCallback);
							}
							break;
						case 3:
							_grfData.Commands.ChangeVersion(1, 2);

							if (_grfData.Header.Magic == GrfStrings.EventHorizon) {
								_grfData.Commands.ChangeHeader(GrfStrings.MasterOfMagic, _changeHeaderCallback);
							}
							break;
					}
				}
				finally {
					_grfData.Commands.End();
				}
			}
		}

		private void _tbTarget_GotFocus(object sender, RoutedEventArgs e) {
			//_labelFind.Visibility = Visibility.Hidden;
		}

		private void _tbTarget_LostFocus(object sender, RoutedEventArgs e) {
			//if (_textBoxTargetGrf.Text == "")
			//    _labelFind.Visibility = Visibility.Visible;
		}

		private void _buttonMagicEdit_Click(object sender, RoutedEventArgs e) {
			var input = WindowProvider.ShowWindow<MagicEditDialog>(new MagicEditDialog(_grfData.Header.Magic), WpfUtilities.TopWindow);

			if (input.DialogResult == true) {
				_grfData.Commands.ChangeHeader(input.OutputHeader, _changeHeaderCallback);
			}
		}

		private void _buttonMagicReset_Click(object sender, RoutedEventArgs e) {
			_grfData.Commands.ChangeHeader(_grfData.Header.IsCompatibleWith(3, 0) ? GrfStrings.EventHorizon : GrfStrings.MasterOfMagic, _changeHeaderCallback);
		}

		private void _changeHeaderCallback(string header, bool execute) {
			_tbMagicHeader.Text = header;
			_buttonMagicReset.IsEnabled = _tbMagicHeader.Text != (_grfData.Header.IsCompatibleWith(3, 0) ? GrfStrings.EventHorizon : GrfStrings.MasterOfMagic);
		}
	}
}