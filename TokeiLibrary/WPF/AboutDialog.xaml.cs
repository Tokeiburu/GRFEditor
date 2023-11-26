using System;
using System.Windows.Controls;
using TokeiLibrary.WPF.Styles;

namespace TokeiLibrary.WPF {
	/// <summary>
	/// Interaction logic for AboutDialog.xaml
	/// </summary>
	public partial class AboutDialog : TkWindow {
		public TextBox AboutTextBox {
			get { return _textBlock; }
		}

		public AboutDialog() : base("About", "help.ico") {
			
		}
		public AboutDialog(string productVersion, string assemblyVersion, string author, string programName, string imageName) : base("About " + programName, "help.ico"){
			InitializeComponent();

			_imageBackground.Source = ApplicationManager.GetResourceImage(imageName);
			_labelSoftwareName.Text = programName;
			_labelSoftwareName_2.Text = programName;
			_textBlock.Text = String.Format(programName + "\n\nProduct version : " + productVersion + "\nAssembly version : " + assemblyVersion + "\nAuthor : " + author + "\n\n" + "This program was designed by " + author + ". " +
				"The software is provided \"as is\" and should be used at your own risk. The author will not be held responsible for any issues it may cause.");
		}

		public AboutDialog(string productVersion, string assemblyVersion, string author, string programName) : this(productVersion, assemblyVersion, author, programName, "aboutBackground3.jpg") {
		}
	}
}
