using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ErrorManager;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core.Avalon;
using TokeiLibrary.WPF.Styles;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace GRFEditor.WPF {
	/// <summary>
	/// Interaction logic for PatcherDialog.xaml
	/// </summary>
	public partial class ViewEncodingDialog : TkWindow {
		private Encoding _encoding = EncodingService.Korean;
		private int _encodingPage = 949;

		public ViewEncodingDialog()
			: base("Text encoding converter", "refresh.png", SizeToContent.Manual, ResizeMode.CanResize) {
			InitializeComponent();

			ShowInTaskbar = true;

			var items = new List<EncodingView> {
				new EncodingView { Encoding = null, FriendlyName = "Auto" }
			}.Concat(EncodingService.GetKnownEncodings()).ToList();
			items.RemoveAt(items.Count - 1);

			_cbEncodingSource.ItemsSource = items;
			_cbEncodingSource.SelectedIndex = 0;
			_cbEncodingSource.SelectionChanged += (e, a) => _updateDestination();

			_cbEncodingDest.Init(EncodingService.GetKnownEncodings(),
								 new TypeSetting<int>(v => _encodingPage = v, () => _encodingPage),
								 new TypeSetting<Encoding>(v => _encoding = v, () => _encoding));

			_cbEncodingDest.SelectedIndex = 1;
			_cbEncodingDest.EncodingChanged += (e, a) => _updateDestination();
			_tbSource.TextChanged += (e, a) => _updateDestination();

			_tbSource.PreviewKeyUp += _onCloseKey;
			_tbDest.PreviewKeyUp += _onCloseKey;

			AvalonHelper.Load(_tbSource);
			AvalonHelper.Load(_tbDest);
		}

		private void _onCloseKey(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape) {
				this.Close();
			}
		}

		private void _updateDestination() {
			try {
				var enc = _cbEncodingSource.SelectedItem as EncodingView;

				if (enc == null)
					return;

				if (enc.Encoding == null) {
					_tbDest.Text = _tbSource.Text.ToEncoding(_encoding);
				}
				else {
					_tbDest.Text = _encoding.GetString(enc.Encoding.GetBytes(_tbSource.Text));
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}