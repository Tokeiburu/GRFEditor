using System.ComponentModel;
using System.IO;
using GRF.Image;

namespace GRF.FileFormats.SprFormat.Builder {
	public class SprBuilderImageView : INotifyPropertyChanged {
		private int _displayID;
		private object _displayImage;
		private string _filename;
		private string _originalName;

		public int DisplayID {
			get { return _displayID; }
			set {
				_displayID = value;
				OnPropertyChanged("");
			}
		}

		public string OriginalName {
			get { return _originalName ?? ""; }
			set {
				_originalName = value;
				OnPropertyChanged("");
			}
		}

		public string Filename {
			get { return _filename; }
			set {
				_filename = value;
				OnPropertyChanged("");
			}
		}

		public string DisplayName {
			get { return Path.GetFileNameWithoutExtension(Filename) + (OriginalName == "" ? "" : " (" + OriginalName + ")"); }
		}

		public object DisplayImage {
			get { return _displayImage; }
			set {
				_displayImage = value;
				OnPropertyChanged("");
			}
		}

		public GrfImage Image { get; set; }

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		protected virtual void OnPropertyChanged(string propertyName) {
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}

		public void Update() {
			OnPropertyChanged("");
		}
	}
}