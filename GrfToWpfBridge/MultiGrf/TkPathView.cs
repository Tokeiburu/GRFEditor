using System;
using System.ComponentModel;
using System.Windows.Media;
using GRF;
using GRF.Core.GroupedGrf;
using TokeiLibrary;
using Utilities;
using Utilities.Extension;

namespace GrfToWpfBridge.MultiGrf {
	public class MultiGrfPathView : INotifyPropertyChanged {
		private bool _fileNotFound;
		private MultiGrfPath _resource;

		public MultiGrfPathView(MultiGrfPath resource) {
			_resource = resource;

			switch(resource.Path.GetExtension()) {
				case ".grf":
					DataImage = ApplicationManager.PreloadResourceImage("grf-16.png");
					break;
				case ".gpf":
					DataImage = ApplicationManager.PreloadResourceImage("gpf-16.png");
					break;
				case ".rgz":
					DataImage = ApplicationManager.PreloadResourceImage("rgz-16.png");
					break;
				case ".thor":
					DataImage = ApplicationManager.PreloadResourceImage("thor-16.png");
					break;
				default:
					DataImage = ApplicationManager.PreloadResourceImage("folderClosed.png");
					break;
			}
		}

		public ImageSource DataImage { get; set; }

		public MultiGrfPath Resource {
			get { return _resource; }
			set { _resource = value; }
		}

		public string DisplayFileName {
			get {
				return (_resource.IsCurrentlyLoadedGrf ? GrfStrings.CurrentlyOpenedGrfHeader : "") + _resource.Path;
			}
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