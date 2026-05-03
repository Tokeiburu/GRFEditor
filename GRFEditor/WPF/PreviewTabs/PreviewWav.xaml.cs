using System;
using System.IO;
using System.Media;
using System.Windows;
using GRF.Core;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge;
using GrfToWpfBridge.PreviewTabs;
using TokeiLibrary;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewWav.xaml
	/// </summary>
	public partial class PreviewWav : FilePreviewTab, IDisposable {
		private readonly SoundPlayer _player = new SoundPlayer();

		public PreviewWav() {
			InitializeComponent();

			Binder.Bind(_checkBoxPlayAutomatically, () => GrfEditorConfiguration.AutomaticallyPlaySoundFiles);
			WpfUtilities.AddMouseInOutUnderline(_checkBoxPlayAutomatically);
			ErrorPanel = _errorPanel;
		}

		protected override void _load(FileEntry entry) {
			_setupUI(entry);

			_loadSoundPlayer(entry);

			if (_isCancelRequired()) return;

			if (GrfEditorConfiguration.AutomaticallyPlaySoundFiles) {
				_stopFile();
				_playFile();
			}
		}

		private void _loadSoundPlayer(FileEntry entry) {
			_player.Stop();
			_player.Stream?.Dispose();
			_player.Stream = new MemoryStream(entry.GetDecompressedData());
		}

		private void _setupUI(FileEntry entry) {
			this.Dispatch(delegate {
				_labelHeader.Text = "Wav file: " + entry.DisplayRelativePath;
			});
		}

		private void _playFile() {
			if (_player.Stream != null)
				_player.Stream.Position = 0;

			_player.Play();
		}

		private void _stopFile() => _player.Stop();
		private void _buttonPlaySound_Click(object sender, RoutedEventArgs e) => _playFile();
		private void _buttonStopSound_Click(object sender, RoutedEventArgs e) => _stopFile();

		#region IDisposable members

		public void Dispose() {
			_player?.Dispose();
		}

		#endregion
	}
}