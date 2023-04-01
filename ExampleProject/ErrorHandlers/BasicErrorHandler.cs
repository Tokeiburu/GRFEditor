using System;
using System.Windows.Forms;
using ErrorManager;

namespace ExampleProject.ErrorHandlers {
	public class BasicErrorHandler : IErrorHandler {
		public void Handle(Exception exception, ErrorLevel errorLevel) {
			Handle(exception.Message, errorLevel);
		}

		public void Handle(string exception, ErrorLevel errorLevel) {
			MessageBox.Show("An exception has been caught : \n\n" + exception + "\n\nException level : " + errorLevel);
		}

		public bool YesNoRequest(string message, string caption) {
			if (MessageBox.Show("The application requires your attention.\n\n" + message, caption, MessageBoxButtons.YesNo) == DialogResult.Yes)
				return true;
			return false;
		}
	}
}
