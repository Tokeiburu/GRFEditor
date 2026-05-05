using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ErrorManager;
using GRF.Core;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core.Services;
using GrfToWpfBridge;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using AsyncOperation = GrfToWpfBridge.Application.AsyncOperation;
using OpeningService = Utilities.Services.OpeningService;

namespace GRFEditor.Tools.GrfValidation {
	/// <summary>
	/// Interaction logic for ValidationDialog.xaml
	/// </summary>
	public partial class ValidationDialog : TkWindow {
		private readonly AsyncOperation _asyncOperation;
		private readonly GrfHolder _grfHolder;
		private readonly ValidateGenericErrors _validateFindErrors = new ValidateGenericErrors();
		private readonly ValidateContentReader _validateContentReader = new ValidateContentReader();

		public ValidationDialog(GrfHolder grfHolder)
			: base("Grf validation", "validity.ico", SizeToContent.Manual, ResizeMode.CanResize) {
			_grfHolder = grfHolder;
			InitializeComponent();

			_asyncOperation = new AsyncOperation(_progressBar);

			_loadSettingsValidateContent();
			_loadSettingsUI();
			_setupResourceViewer();

			_progressBar.SetSpecialState(TkProgressBar.ProgressStatus.Finished);

			Owner = WpfUtilities.TopWindow;
		}

		private void _setupResourceViewer() {
			_mViewer.SaveResourceMethod = v => GrfEditorConfiguration.Resources.SaveResources(v);
			_mViewer.LoadResourceMethod = () => GrfEditorConfiguration.Resources.LoadResources();
			GrfEditorConfiguration.Resources.Modified += delegate {
				_mViewer.LoadResourcesInfo();
			};
			_mViewer.LoadResourcesInfo();
			_mViewer.CanDeleteMainGrf = false;
			_asyncOperation.SetAndRunOperation(prog => _updateResources(prog));
		}

		private void _loadSettingsUI() {
			_changeRawViewButton();

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_listViewResults, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.ImageColumnInfo { Header = "", DisplayExpression = "DataImage", SearchGetAccessor = "ValidationType", FixedWidth = 20, MaxHeight = 16 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Error code", DisplayExpression = "ValidationType", FixedWidth = 150, ToolTipBinding = "ValidationType", TextAlignment = TextAlignment.Center },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "File name", DisplayExpression = "DisplayRelativePath", FixedWidth = 250, ToolTipBinding = "ToolTipRelativePath", TextAlignment = TextAlignment.Left },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Description", DisplayExpression = "Description", FixedWidth = 150, ToolTipBinding = "ToolTipDescription", TextAlignment = TextAlignment.Left, IsFill = true },
			}, new DefaultListViewComparer<ValidationView>(), new string[] { "Default", "{StaticResource TextForeground}" });
		}

		private void _loadSettingsValidateContent() {
			Binder.Bind(_cbVcLoadEntries, () => GrfEditorConfiguration.VcLoadEntries, _updateValidationUIState, true);
			Binder.Bind(_cbVcSpriteIssues, () => GrfEditorConfiguration.VcSpriteIssues, _updateValidationUIState, true);
		}

		private void _updateValidationUIState() {
			bool loadEntries = GrfEditorConfiguration.VcLoadEntries;

			_cbVcSpriteIssues.IsEnabled = loadEntries;
			_cbVcResourcesModelFiles.IsEnabled = loadEntries;
			_cbVcResourcesMapFiles.IsEnabled = loadEntries;
			_cbVcInvalidImageFormat.IsEnabled = loadEntries;

			bool spriteIssues = loadEntries && GrfEditorConfiguration.VcSpriteIssues;

			_cbVcSpriteIssuesRle.IsEnabled = spriteIssues;
			_cbVcSpriteSoundIndex.IsEnabled = spriteIssues;
			_cbVcSpriteSoundMissing.IsEnabled = spriteIssues;
			_cbVcSpriteIndex.IsEnabled = spriteIssues;
		}

		protected override void OnClosing(CancelEventArgs e) {
			_asyncOperation.Cancel();
			base.OnClosing(e);
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void _buttonFindErrors_Click(object sender, RoutedEventArgs e) {
			ValidateResult result = new ValidateResult();
			_asyncOperation.SetAndRunOperation(prog => _validateFindErrors.FindErrors(prog, result, _grfHolder), _updateErrors, result);
		}

		private void _buttonValidateContent_Click(object sender, RoutedEventArgs e) {
			ValidateResult result = new ValidateResult();
			_asyncOperation.SetAndRunOperation(prog => _validateContentReader.ValidateContent(prog, result, _grfHolder), _updateErrors, result);
		}

		private void _updateErrors(object state) {
			try {
				var errors = (ValidateResult)state;
				List<ValidationView> errorsView = errors.Errors.Select(error => new ValidationView(error)).ToList();
				string rawView = String.Join("\r\n", errorsView);

				this.Dispatch(delegate {
					_tbResults.Text = rawView;
					_listViewResults.ItemsSource = errorsView;
					_tabControl.SelectedItem = _tabItemResults;
				});
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonRawView_Click(object sender, RoutedEventArgs e) {
			GrfEditorConfiguration.ValidationRawView = !GrfEditorConfiguration.ValidationRawView;
			_changeRawViewButton();
		}

		private void _changeRawViewButton() {
			bool isValidate = GrfEditorConfiguration.ValidationRawView;
			_buttonRawView.TextHeader = isValidate ? "List view" : "Raw view";
			_buttonRawView.TextDescription = isValidate ? "Show the list view" : "Show the raw text view";
			_listViewResults.Visibility = isValidate ? Visibility.Hidden : Visibility.Visible;
			_tbResults.Visibility = isValidate ? Visibility.Visible : Visibility.Hidden;
		}

		private void _listViewResults_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e) {
			var lvi = _listViewResults.GetObjectAtPoint<ListViewItem>(e.GetPosition(_listViewResults));

			if (lvi == null) {
				_cmResults.IsOpen = false;
				e.Handled = true;
			}
		}

		private void _menuItemSelect_Click(object sender, RoutedEventArgs e) {
			try {
				ValidationView view = _listViewResults.SelectedItem as ValidationView;

				if (view != null) {
					if (_grfHolder.FileTable.ContainsFile(view.RelativePath)) {
						PreviewService.Select(null, null, view.RelativePath);
					}
					else if (File.Exists(view.RelativePath)) {
						OpeningService.FileOrFolder(view.RelativePath);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _updateResources(ProgressObject prog) {
			try {
				GrfEditorConfiguration.Resources.Reload();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				prog.Finish();
			}
		}
	}
}