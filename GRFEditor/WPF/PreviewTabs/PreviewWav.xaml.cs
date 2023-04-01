using System;
using System.IO;
using System.Media;
using System.Windows;
using GRF.Core;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles.ListView;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewWav.xaml
	/// </summary>
	public partial class PreviewWav : FilePreviewTab, IDisposable {
		private readonly SoundPlayer _player = new SoundPlayer();

		public PreviewWav() {
			InitializeComponent();

			Binder.Bind(_checkBoxPlayAutomatically, () => GrfEditorConfiguration.AutomaticallyPlaySoundFiles);
			WpfUtils.AddMouseInOutEffectsBox(_checkBoxPlayAutomatically);
		}

		protected override void _load(FileEntry entry) {
			_labelHeader.Dispatch(p => p.Content = "Wav file : " + Path.GetFileName(entry.RelativePath));

			_player.Stream = new MemoryStream(entry.GetDecompressedData());

			if (_isCancelRequired()) return;

			if (GrfEditorConfiguration.AutomaticallyPlaySoundFiles) {
				_stopFile();
				_playFile();
			}
		}

		private void _playFile() {
			_player.Play();
		}

		private void _stopFile() {
			_player.Stop();
		}

		private void _buttonPlaySound_Click(object sender, RoutedEventArgs e) {
			_playFile();
		}

		private void _buttonStopSound_Click(object sender, RoutedEventArgs e) {
			_stopFile();
		}

		#region IDisposable members

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~PreviewWav() {
			Dispose(false);
		}

		protected void Dispose(bool disposing) {
			if (disposing) {
				if (_player != null) {
					_player.Dispose();
				}
			}
		}

		#endregion
	}
}