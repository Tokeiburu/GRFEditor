using System;
using System.Globalization;
using ErrorManager;
using GRF.FileFormats.LubFormat;
using GRF.System;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core.Services;
using GrfToWpfBridge;
using TokeiLibrary;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for DecompilerSettings.xaml
	/// </summary>
	public partial class DecompilerSettings : FilePreviewTab, IErrorListener {
		private readonly PreviewService _service;

		public DecompilerSettings(PreviewService service) {
			_service = service;
			InitializeComponent();
			_labelHeader.Dispatch(p => p.Text = "Lub decompiler");

			_reloadLubDecompilerSettings();
			LubErrorHandler.AddListener(this);

			_tbTextLengthLimit.Text = GrfEditorConfiguration.TextLengthLimit.ToString(CultureInfo.InvariantCulture);

			Binder.Bind(_cbShowGrfHeader, () => GrfEditorConfiguration.ShowGrfEditorHeader, _reloadLubDecompilerSettings);
			Binder.Bind(_cbUseCodeReconstructor, () => GrfEditorConfiguration.UseCodeReconstructor, _reloadLubDecompilerSettings);
			Binder.Bind(_cbFunctionNumber, () => GrfEditorConfiguration.AppendFunctionId, _reloadLubDecompilerSettings);
			Binder.Bind(_cbGroupAllValues, () => GrfEditorConfiguration.GroupIfAllValues, _reloadLubDecompilerSettings);
			Binder.Bind(_cbGroupAllKeyValues, () => GrfEditorConfiguration.GroupIfAllKeyValues, _reloadLubDecompilerSettings);

			_tbTextLengthLimit.TextChanged += delegate {
				try {
					GrfEditorConfiguration.TextLengthLimit = Int32.Parse(_tbTextLengthLimit.Text);
					_reloadLubDecompilerSettings();
				}
				catch {
					GrfEditorConfiguration.TextLengthLimit = 80;
				}
			};
		}

		#region IErrorListener Members

		public void Handle(string exception) {
			_tbConsole.Dispatcher.Invoke(new Action(delegate {
				_tbConsole.AppendText("> " + exception + "\r\n");
				_tbConsole.ScrollToEnd();
			}));
		}

		public void Handle(string exception, ErrorLevel level) {
			_tbConsole.Dispatcher.Invoke(new Action(delegate {
				_tbConsole.AppendText("> " + exception + "\r\n");
				_tbConsole.ScrollToEnd();
			}));
		}

		#endregion

		private void _reloadLubDecompilerSettings() {
			Settings.LubDecompilerSettings.UseCodeReconstructor = GrfEditorConfiguration.UseCodeReconstructor;
			Settings.LubDecompilerSettings.AppendFunctionId = GrfEditorConfiguration.AppendFunctionId;
			Settings.LubDecompilerSettings.DecodeInstructions = GrfEditorConfiguration.DecodeInstructions;
			Settings.LubDecompilerSettings.GroupIfAllKeyValues = GrfEditorConfiguration.GroupIfAllKeyValues;
			Settings.LubDecompilerSettings.GroupIfAllValues = GrfEditorConfiguration.GroupIfAllValues;
			Settings.LubDecompilerSettings.TextLengthLimit = GrfEditorConfiguration.TextLengthLimit;
			_reloadOtherTabs();
		}

		private void _reloadOtherTabs() {
			if (_grfData != null)
				_service.InvalidateAllVisiblePreviewTabs(_grfData);
		}
	}
}