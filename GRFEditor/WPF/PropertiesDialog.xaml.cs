using System;
using System.Windows;
using ErrorManager;
using GRF.Core;
using GRF.FileFormats;
using TokeiLibrary.WPF.Styles;
using Utilities.Services;

namespace GRFEditor.WPF {
	/// <summary>
	/// Interaction logic for PropertiesDialog.xaml
	/// </summary>
	public partial class PropertiesDialog : TkWindow {
		public PropertiesDialog(GrfHolder grfData, object selectedObject = null) : base("Properties", "properties.ico") {
			InitializeComponent();

			try {
				if (selectedObject == null) {
					Prop = FileFormatParser.DisplayObjectProperties(grfData);
					Prop = "DisplayEncoding = " + EncodingService.DisplayEncoding.WebName;
				}
				else {
					Prop = "# GRF File Entry Properties #";
					Prop = FileFormatParser.DisplayObjectPropertiesFromEntry(grfData, (FileEntry) selectedObject);
				}
			}
			catch (Exception er) {
				ErrorHandler.HandleException(er);
			}
		}

		public string Prop {
			set { _properties.Text += value + "\r\n"; }
		}

		protected void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}
	}
}