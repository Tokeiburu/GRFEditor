using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ErrorManager;
using Utilities;

namespace GRFEditor {
	/// <summary>
	/// Interaction logic for TempNameDebug.xaml
	/// </summary>
	public partial class TempNameDebug : Window {
		public TempNameDebug() {
			InitializeComponent();
		}

		private void _btLoad_Click(object sender, RoutedEventArgs e) {
			//_tbAddress.Text = "Checking...";
			//
			//var dllHandle = NativeMethods.LoadLibrary(@"C:\Users\Tokei\AppData\Roaming\GRF Editor\Encryption\cps.dll");
			//
			//var uncompress_handle = NativeMethods.GetProcAddress(dllHandle, _tbMethod.Text);
			//
			//_tbAddress.Text = uncompress_handle.ToString();
			//
			//NativeMethods.FreeLibrary(dllHandle);
		}
	}
}
