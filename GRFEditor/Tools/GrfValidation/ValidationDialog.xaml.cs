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
using GRF;
using GRF.Core;
using GRF.Core.GroupedGrf;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core.Services;
using GrfToWpfBridge;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Extension;
using Utilities.Hash;
using AsyncOperation = GrfToWpfBridge.Application.AsyncOperation;
using OpeningService = Utilities.Services.OpeningService;

namespace GRFEditor.Tools.GrfValidation {
	/// <summary>
	/// Interaction logic for ValidationDialog.xaml
	/// </summary>
	public partial class ValidationDialog : TkWindow, IProgress, IDisposable {
		private readonly string[] _advancedView = new string[] { "List view", "Show the list view" };
		private readonly string[] _rawView = new string[] { "Raw view", "Show the raw text view" };
		private readonly AsyncOperation _asyncOperation;
		private readonly GrfHolder _grfHolder;
		private readonly Validation _validation = new Validation();
		private MultiGrfReader _metaGrf = new MultiGrfReader();
		private bool _requiresMetaGrfReload = true;

		public ValidationDialog(GrfHolder grfHolder)
			: base("Grf validation", "validity.ico", SizeToContent.Manual, ResizeMode.CanResize) {
			_grfHolder = grfHolder;
			InitializeComponent();

			_asyncOperation = new AsyncOperation(_progressBar);

			_loadSettingsFindErrors();
			_loadSettingsValidateContent();
			_loadSettingsValidateExtraction();
			_loadSettingsUI();

			_mViewer.SaveResourceMethod = v => GrfEditorConfiguration.MapExtractorResources = v;
			_mViewer.LoadResourceMethod = () => {
				var items = Methods.StringToList(GrfEditorConfiguration.MapExtractorResources);

				if (!items.Contains(GrfStrings.CurrentlyOpenedGrf + _grfHolder.FileName)) {
					items.RemoveAll(p => p.StartsWith(GrfStrings.CurrentlyOpenedGrf));
					items.Insert(0, GrfStrings.CurrentlyOpenedGrf + _grfHolder.FileName);
				}

				return items;
			};

			_mViewer.Modified += delegate {
				_requiresMetaGrfReload = true;
				_asyncOperation.SetAndRunOperation(new GrfThread(_updateResources, this, 200, null, false, true));
			};

			_mViewer.LoadResourcesInfo();
			_mViewer.CanDeleteMainGrf = false;

			Owner = WpfUtilities.TopWindow;

			Binder.Bind(_cbComparisonAlrightm);
		}

		#region IDisposable Members

		public void Dispose() {
			Dispose(true);
		}

		#endregion

		#region IProgress Members

		public float Progress { get; set; }
		public bool IsCancelling { get; set; }
		public bool IsCancelled { get; set; }

		#endregion

		private void _loadSettingsUI() {
			_changeRawViewButton();

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_listViewResults, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.ImageColumnInfo { Header = "", DisplayExpression = "DataImage", SearchGetAccessor = "ValidationType", FixedWidth = 20, MaxHeight = 24 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Error code", DisplayExpression = "ValidationType", FixedWidth = 150, ToolTipBinding = "ValidationType", TextAlignment = TextAlignment.Center },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "File name", DisplayExpression = "DisplayRelativePath", FixedWidth = 250, ToolTipBinding = "ToolTipRelativePath", TextAlignment = TextAlignment.Left },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Description", DisplayExpression = "Description", FixedWidth = 150, ToolTipBinding = "ToolTipDescription", TextAlignment = TextAlignment.Left, IsFill = true },
			}, new DefaultListViewComparer<ValidationView>(), new string[] { "Default", "{StaticResource TextForeground}" });
		}

		private void _loadSettingsValidateExtraction() {
			Binder.Bind(_cbVeIgnoreFilesNotFound, () => GrfEditorConfiguration.VeFilesNotFound);
			Binder.Bind(_cbVeFilesDifferentSize, () => GrfEditorConfiguration.VeFilesDifferentSize);
			Binder.Bind(_pbValidation, () => GrfEditorConfiguration.VeFolder);

			_cbComparisonAlrightm.Items.Add(new Crc32Hash());
			_cbComparisonAlrightm.Items.Add(new Md5Hash());
			_cbComparisonAlrightm.Items.Add(new FileSizeHash());
			_cbComparisonAlrightm.SelectedIndex = 0;

			WpfUtils.AddMouseInOutEffectsBox(_cbVeIgnoreFilesNotFound, _cbVeFilesDifferentSize);
		}

		private void _loadSettingsValidateContent() {
			Binder.Bind(_cbVcDecompressEntries, () => GrfEditorConfiguration.VcDecompressEntries);
			Binder.Bind(_cbVcLoadEntries, () => GrfEditorConfiguration.VcLoadEntries, delegate {
				bool enabled = GrfEditorConfiguration.VcLoadEntries;

				_cbVcSpriteIssues.IsEnabled = enabled;
				_cbVcResourcesModelFiles.IsEnabled = enabled;
				_cbVcResourcesMapFiles.IsEnabled = enabled;

				enabled = enabled && GrfEditorConfiguration.VcSpriteIssues;

				_cbVcSpriteIssuesRle.IsEnabled = enabled;
				_cbVcSpriteSoundIndex.IsEnabled = enabled;
				_cbVcSpriteSoundMissing.IsEnabled = enabled;
				_cbVcSpriteIndex.IsEnabled = enabled;
			}, true);
			Binder.Bind(_cbVcInvalidEntryMetadata, () => GrfEditorConfiguration.VcInvalidEntryMetadata);
			Binder.Bind(_cbVcSpriteIssues, () => GrfEditorConfiguration.VcSpriteIssues, delegate {
				bool enabled = GrfEditorConfiguration.VcLoadEntries && GrfEditorConfiguration.VcSpriteIssues;

				_cbVcSpriteIssuesRle.IsEnabled = enabled;
				_cbVcSpriteSoundIndex.IsEnabled = enabled;
				_cbVcSpriteSoundMissing.IsEnabled = enabled;
				_cbVcSpriteIndex.IsEnabled = enabled;
			}, true);

			Binder.Bind(_cbVcResourcesModelFiles, () => GrfEditorConfiguration.VcResourcesModelFiles);
			Binder.Bind(_cbVcResourcesMapFiles, () => GrfEditorConfiguration.VcResourcesMapFiles);
			Binder.Bind(_cbVcChecksum, () => GrfEditorConfiguration.VcZlibChecksum);
			Binder.Bind(_cbVcSpriteIssuesRle, () => GrfEditorConfiguration.VcSpriteIssuesRle);
			Binder.Bind(_cbVcSpriteSoundIndex, () => GrfEditorConfiguration.VcSpriteSoundIndex);
			Binder.Bind(_cbVcSpriteSoundMissing, () => GrfEditorConfiguration.VcSpriteSoundMissing);
			Binder.Bind(_cbVcSpriteIndex, () => GrfEditorConfiguration.VcSpriteIndex);

			WpfUtils.AddMouseInOutEffectsBox(_cbVcDecompressEntries, _cbVcLoadEntries, _cbVcInvalidEntryMetadata, _cbVcSpriteIssues,
											 _cbVcResourcesModelFiles, _cbVcResourcesMapFiles, _cbVcChecksum, _cbVcSpriteIssuesRle, _cbVcSpriteSoundIndex,
											 _cbVcSpriteSoundMissing, _cbVcSpriteIndex);
		}

		private void _loadSettingsFindErrors() {
			Binder.Bind(_cbFeNoExtension, () => GrfEditorConfiguration.FeNoExtension);
			Binder.Bind(_cbFeMissingSprAct, () => GrfEditorConfiguration.FeMissingSprAct);
			Binder.Bind(_cbFeEmptyFiles, () => GrfEditorConfiguration.FeEmptyFiles);
			Binder.Bind(_cbFeDb, () => GrfEditorConfiguration.FeDb);
			Binder.Bind(_cbFeSvn, () => GrfEditorConfiguration.FeSvn);
			Binder.Bind(_cbFeDuplicateFiles, () => GrfEditorConfiguration.FeDuplicateFiles);
			Binder.Bind(_cbFeDuplicatePaths, () => GrfEditorConfiguration.FeDuplicatePaths);
			Binder.Bind(_cbFeSpaceSaved, () => GrfEditorConfiguration.FeSpaceSaved);
			Binder.Bind(_cbFeInvalidFileTable, () => GrfEditorConfiguration.FeInvalidFileTable);
			Binder.Bind(_cbFeRootFiles, () => GrfEditorConfiguration.FeRootFiles);

			WpfUtils.AddMouseInOutEffectsBox(
				_cbFeNoExtension, _cbFeMissingSprAct, _cbFeEmptyFiles, _cbFeDb, _cbFeSvn,
				_cbFeDuplicateFiles, _cbFeDuplicatePaths, _cbFeSpaceSaved, _cbFeInvalidFileTable, _cbFeRootFiles);
		}

		protected override void OnClosing(CancelEventArgs e) {
			_asyncOperation.Cancel();
			base.OnClosing(e);
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void _buttonFindErrors_Click(object sender, RoutedEventArgs e) {
			List<Tuple<ValidationTypes, string, string>> errors = new List<Tuple<ValidationTypes, string, string>>();
			_asyncOperation.SetAndRunOperation(new GrfThread(() => _validation.FindErrors(errors, _grfHolder), _validation, 200, errors), _updateErrors);
		}

		private void _updateErrors(object state) {
			try {
				List<Tuple<ValidationTypes, string, string>> errors = (List<Tuple<ValidationTypes, string, string>>)state;
				errors = errors.Where(p => p != null).ToList();
				List<ValidationView> errorsView = errors.Select(error => new ValidationView(error)).ToList();
				StringBuilder builder = new StringBuilder();

				for (int index = 0; index < errorsView.Count; index++) {
					builder.Append(errorsView[index] + "\r\n");
				}

				_tbResults.Dispatch(p => p.Text = builder.ToString());
				_listViewResults.Dispatch(p => p.ItemsSource = errorsView);
				_tabControl.Dispatch(p => p.SelectedItem = _tabItemResults);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonValidateContent_Click(object sender, RoutedEventArgs e) {
			List<Tuple<ValidationTypes, string, string>> errors = new List<Tuple<ValidationTypes, string, string>>();
			_asyncOperation.SetAndRunOperation(new GrfThread(() => _validation.ValidateContent(errors, _grfHolder, ref _metaGrf), _validation, 200, errors), _updateErrors);
		}

		private void _buttonValidateExtraction_Click(object sender, RoutedEventArgs e) {
			List<Tuple<ValidationTypes, string, string>> errors = new List<Tuple<ValidationTypes, string, string>>();
			bool direction = _rbGrfFolder.IsChecked == true;
			IHash hash = _cbComparisonAlrightm.SelectedItem as IHash;
			bool ignoreFilesNotFound = _cbVeIgnoreFilesNotFound.IsChecked == true;
			string text = _pbValidation.Text;
			_asyncOperation.SetAndRunOperation(new GrfThread(() => _validation.ValidateExtraction(errors, _grfHolder, text, direction, hash, ignoreFilesNotFound), _validation, 200, errors), _updateErrors);
		}

		private void _buttonPrintHash_Click(object sender, RoutedEventArgs e) {
			List<Tuple<ValidationTypes, string, string>> errors = new List<Tuple<ValidationTypes, string, string>>();
			IHash hash = _cbComparisonAlrightm.SelectedItem as IHash;
			bool direction = _rbGrfFolder.IsChecked == true;
			string text = _pbValidation.Text;
			_asyncOperation.SetAndRunOperation(new GrfThread(() => _validation.ComputeHash(errors, _grfHolder, hash, direction, text), _validation, 200, errors), _updateErrors);
		}

		private void _buttonRawView_Click(object sender, RoutedEventArgs e) {
			GrfEditorConfiguration.ValidationRawView = !GrfEditorConfiguration.ValidationRawView;
			_changeRawViewButton();
		}

		private void _changeRawViewButton() {
			if (GrfEditorConfiguration.ValidationRawView) {
				_buttonRawView.Dispatch(p => p.TextHeader = _advancedView[0]);
				_buttonRawView.Dispatch(p => p.TextDescription = _advancedView[1]);
				_listViewResults.Visibility = Visibility.Hidden;
				_tbResults.Visibility = Visibility.Visible;
			}
			else {
				_buttonRawView.Dispatch(p => p.TextHeader = _rawView[0]);
				_buttonRawView.Dispatch(p => p.TextDescription = _rawView[1]);
				_listViewResults.Visibility = Visibility.Visible;
				_tbResults.Visibility = Visibility.Hidden;
			}
		}

		private void _listViewResults_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e) {
			object test = _listViewResults.InputHitTest(e.GetPosition(_listViewResults));

			if (test is ScrollViewer) {
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

		private void _updateResources() {
			try {
				Progress = -1;

				if (_requiresMetaGrfReload)
					_metaGrf.Update(_mViewer.Paths, _grfHolder);

				_requiresMetaGrfReload = false;
			}
			catch (Exception err) {
				_requiresMetaGrfReload = true;
				ErrorHandler.HandleException(err);
			}
			finally {
				Progress = 100;
			}
		}

		protected void Dispose(bool disposing) {
			if (disposing) {
				if (_metaGrf != null)
					_metaGrf.Dispose();
			}
		}

		#region Nested type: ValidationViewSorter

		internal class ValidationViewSorter : ListViewCustomComparer {
			public override int Compare(object x, object y) {
				try {
					ValidationView xc = (ValidationView)x;
					ValidationView yc = (ValidationView)y;

					string valx = String.Empty, valy = String.Empty;

					if (_sortColumn == null)
						return 0;

					switch (_sortColumn) {
						case "ValidationType":
							valx = xc.ValidationType;
							valy = yc.ValidationType;
							break;
						case "DisplayRelativePath":
							valx = xc.DisplayRelativePath;
							valy = yc.DisplayRelativePath;
							break;
						case "Description":
							valx = xc.Description;
							valy = yc.Description;
							break;
					}

					if (_direction == ListSortDirection.Ascending)
						return String.CompareOrdinal(valx, valy);

					return (-1) * String.CompareOrdinal(valx, valy);
				}
				catch (Exception) {
					return 0;
				}
			}
		}

		#endregion
	}
}