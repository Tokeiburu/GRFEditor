using System;
using System.ComponentModel;
using System.Windows.Media;
using TokeiLibrary;
using Utilities;

namespace GrfToWpfBridge.MultiGrf {
	public class TkPathView : INotifyPropertyChanged {
		private bool _fileNotFound;
		private TkPath _path;

		public TkPathView(TkPath path) {
			_path = path;

			ImageSource image;

			if (path.FilePath.ToLower().EndsWith(".grf")) {
				image = ApplicationManager.PreloadResourceImage("grf-16.png");
			}
			else if (path.FilePath.ToLower().EndsWith(".gpf")) {
				image = ApplicationManager.PreloadResourceImage("gpf-16.png");
			}
			else if (path.FilePath.ToLower().EndsWith(".rgz")) {
				image = ApplicationManager.PreloadResourceImage("rgz-16.png");
			}
			else if (path.FilePath.ToLower().EndsWith(".thor")) {
				image = ApplicationManager.PreloadResourceImage("thor-16.png");
			}
			else {
				image = ApplicationManager.PreloadResourceImage("folderClosed.png");
			}

			DataImage = image;
		}

		public ImageSource DataImage { get; set; }

		public TkPath Path {
			get { return _path; }
			set { _path = value; }
		}

		public string DisplayFileName {
			get { return String.IsNullOrEmpty(_path.FilePath) ? _path.RelativePath : _path.FilePath; }
		}

		public bool FileNotFound {
			get { return _fileNotFound; }
			set {
				_fileNotFound = value;
				Update();
			}
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		public void Update() {
			OnPropertyChanged("");
		}

		protected virtual void OnPropertyChanged(string propertyName) {
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}