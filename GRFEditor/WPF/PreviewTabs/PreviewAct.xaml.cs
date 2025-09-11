using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.Core;
using GRF.FileFormats.ActFormat;
using GRF.IO;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge;
using GrfToWpfBridge.Application;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using Utilities.Extension;
using Utilities.Services;
using Action = System.Action;
using Imaging = ActImaging.Imaging;

namespace GRFEditor.WPF.PreviewTabs {
	public class ScalingMode {
		public string Name { get; set; }
		public BitmapScalingMode Mode { get; set; }
		public bool IsAuto { get; set; }

		public override string ToString() {
			return Name;
		}
	}

	/// <summary>
	/// Interaction logic for PreviewAct.xaml
	/// </summary>
	public partial class PreviewAct : FilePreviewTab, IDisposable {
		private readonly ManualResetEvent _actThreadHandle = new ManualResetEvent(false);
		private readonly AsyncOperation _asyncOperation;
		private readonly List<FancyButton> _fancyButtons;
		private readonly object _lockAnimation = new object();
		private readonly Stopwatch _watch = new Stopwatch();
		private Act _act;
		private int _actThreadSleepDelay = 100;
		private int _actionIndex = -1;
		private bool _changedAnimationIndex;
		private int _frameIndex;
		private bool _isRunning = true;
		private bool _stopAnimation;
		private bool _threadIsEnabled = true;

		public PreviewAct(AsyncOperation asyncOperation) {
			_asyncOperation = asyncOperation;
			InitializeComponent();
			SettingsDialog.UIPanelPreviewBackgroundPick(_qcsBackground);
			_grid.UseLayoutRounding = false;

			_imagePreview.Dispatch(p => p.SetValue(RenderOptions.BitmapScalingModeProperty, Configuration.BestAvailableScaleMode));
			_imagePreview.Dispatch(p => p.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased));
			//_imagePreview.Dispatch(p => p.UseLayoutRounding = false);

			Binder.Bind(_buttonScale, () => GrfEditorConfiguration.PreviewActScaleType, v => GrfEditorConfiguration.PreviewActScaleType = v, delegate {
				_buttonScale.IsPressed = GrfEditorConfiguration.PreviewActScaleType;
			}, true);

			_fancyButtons = new FancyButton[] { _fancyButton0, _fancyButton1, _fancyButton2, _fancyButton3, _fancyButton4, _fancyButton5, _fancyButton6, _fancyButton7 }.ToList();
			BitmapSource image = ApplicationManager.PreloadResourceImage("arrow.png");
			BitmapSource image2 = ApplicationManager.PreloadResourceImage("arrowoblique.png");

			for (int index = 0; index < _fancyButtons.Count; ++index) {
				_fancyButtons[index].ImageIcon.Source = index % 2 == 0 ? image : image2;
				_fancyButtons[index].ImageIcon.Height = 16;
				_fancyButtons[index].ImageIcon.Width = 16;
				_fancyButtons[index].ImageIcon.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
				//_fancyButtons[index].ImageIcon.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Unspecified);
				_fancyButtons[index].ImageIcon.RenderTransformOrigin = new Point(0.5, 0.5);
				_fancyButtons[index].ImageIcon.RenderTransform = new RotateTransform { Angle = index / 2 * 90 + 90 };
			}

			IsVisibleChanged += (e, a) => _enableActThread = IsVisible;
			
			Dispatcher.ShutdownStarted += delegate {
				_isRunning = false;
				_enableActThread = true;
			};

			new Thread(_actAnimationThread) { Name = "GrfEditor - Sprite animation update thread" }.Start();
		}

		private bool _enableActThread {
			set {
				if (value) {
					if (!_threadIsEnabled)
						_actThreadHandle.Set();
				}
				else {
					if (_threadIsEnabled) {
						_threadIsEnabled = false;
						_actThreadHandle.Reset();
					}
				}
			}
		}

		public Action<Brush> BackgroundBrushFunction {
			get { return v => this.Dispatch(p => _scrollViewer.Background = v); }
		}

		private void _actAnimationThread() {
			while (true) {
				if (!_isRunning)
					return;

				_watch.Reset();
				_watch.Start();

				lock (_lockAnimation) {
					if (!_stopAnimation)
						_displayNextFrame();
				}

				_watch.Stop();

				int delay = (int) (_actThreadSleepDelay - _watch.ElapsedMilliseconds);
				delay = delay < 0 ? 0 : delay;

				Thread.Sleep(delay);

				if (!_threadIsEnabled) {
					_actThreadHandle.WaitOne();

					if (!_threadIsEnabled)
						_threadIsEnabled = true;
				}
			}
		}

		protected override void _load(FileEntry entry) {
			byte[] dataDecompress;
			byte[] dataDecompressSpr;

			string actRelativePath = entry.RelativePath;

			try {
				dataDecompress = entry.GetDecompressedData();
			}
			catch (GrfException err) {
				if (err == GrfExceptions.__CorruptedOrEncryptedEntry) {
					_cancelAnimation();
					_labelHeader.Dispatch(p => p.Text = "Failed to decompressed data. Corrupted or encrypted entry.");
					return;
				}

				if (err == GrfExceptions.__ContainerBusy)
					return;

				throw;
			}

			_labelHeader.Dispatch(p => p.Text = "Animation : " + Path.GetFileName(actRelativePath));

			try {
				var sprEntry = _grfData.FileTable.TryGet(actRelativePath.ReplaceExtension(".spr"));

				if (sprEntry == null) {
					if (actRelativePath.StartsWith(EncodingService.FromAnyToDisplayEncoding(@"data\sprite\·Îºê\"))) {
						var dirs = GrfPath.SplitDirectories(actRelativePath).ToList();
						dirs.RemoveAt(dirs.Count - 1);
						dirs.RemoveAt(dirs.Count - 1);
						dirs.Add(dirs.Last() + ".spr");

						sprEntry = _grfData.FileTable.TryGet(GrfPath.Combine(dirs.ToArray()));
					}
				}

				if (sprEntry == null) {
					_cancelAnimation();
					return;
				}

				dataDecompressSpr = sprEntry.GetDecompressedData();
			}
			catch {
				//ErrorHandler.HandleException("Couldn't find the corresponding spr file : \n" + actRelativePath.ReplaceExtension(".spr"), ErrorLevel.Low);
				_cancelAnimation();
				return;
			}

			if (_isCancelRequired()) return;

			Act act = new Act(dataDecompress, dataDecompressSpr);
			act.Safe();

			if (_isCancelRequired()) return;

			IEnumerable<int> actions = Enumerable.Range(0, act.NumberOfActions);

			lock (_lockAnimation) {
				_stopAnimation = true;
			}

			lock (_lockAnimation) {
				_act = act;
				_changedAnimationIndex = true;
				_stopAnimation = false;
			}

			int oldActionIndex = (int) _comboBoxActionIndex.Dispatch(() => _comboBoxActionIndex.SelectedIndex);

			if (oldActionIndex < 0)
				oldActionIndex = 0;

			if (oldActionIndex >= act.NumberOfActions)
				oldActionIndex = 0;

			_comboBoxActionIndex.Dispatch(() => _comboBoxActionIndex.ItemsSource = actions);
			_comboBoxAnimationIndex.Dispatch(() => _comboBoxAnimationIndex.ItemsSource = act.GetAnimationStrings());
			_setDisabledButtons();

			if (_isCancelRequired()) return;

			_imagePreview.Dispatch(p => p.VerticalAlignment = VerticalAlignment.Top);
			_imagePreview.Dispatch(p => p.HorizontalAlignment = HorizontalAlignment.Left);
			_comboBoxActionIndex.Dispatch(p => p.SelectedIndex = oldActionIndex);
			_comboBoxActionIndex.Dispatch(p => p.Visibility = Visibility.Visible);
			_comboBoxAnimationIndex.Dispatch(p => p.SelectedIndex = oldActionIndex / 8);
			_imagePreview.Dispatch(p => p.Visibility = Visibility.Visible);
			_scrollViewer.Dispatch(p => p.Visibility = Visibility.Visible);

			int actionIndex = (int) _comboBoxActionIndex.Dispatch(() => _comboBoxActionIndex.SelectedIndex);

			if (actionIndex < 0)
				return;

			if ((int) _act[actionIndex].AnimationSpeed * 24 == 0 ||
			    float.IsNaN(_act[actionIndex].AnimationSpeed)) {
				if (_act[actionIndex].Frames[0].Layers[0].SpriteIndex < 0) {
					_imagePreview.Dispatch(p => p.Source = null);
					return;
				}

				_imagePreview.Dispatch(p => p.Source = _act.Sprite.Images[_act[actionIndex].Frames[0].Layers[0].SpriteIndex].Cast<BitmapSource>());
			}
			else {
				_actThreadSleepDelay = (int) (_act[actionIndex].AnimationSpeed * 24);
			}

			_enableActThread = true;
		}

		private void _cancelAnimation() {
			try {
				lock (_lockAnimation) {
					_stopAnimation = true;
				}

				_imagePreview.Dispatch(p => p.Visibility = Visibility.Hidden);
				_comboBoxActionIndex.Dispatch(p => p.ItemsSource = null);
				_comboBoxAnimationIndex.Dispatch(p => p.ItemsSource = null);
			}
			catch {
			}
		}

		private void _displayNextFrame() {
			try {
				if (_actionIndex < 0) {
					_enableActThread = false;
					return;
				}

				Act act = _act;
				int actionIndex = _actionIndex;

				if (actionIndex >= act.NumberOfActions) {
					_imagePreview.Dispatch(p => p.Source = null);
					return;
				}

				_frameIndex++;
				_frameIndex = _frameIndex >= act[actionIndex].NumberOfFrames ? 0 : _frameIndex;

				if (act[actionIndex].Frames[_frameIndex % act[actionIndex].NumberOfFrames].NumberOfLayers <= 0) {
					_imagePreview.Dispatch(p => p.Source = null);
					return;
				}

				List<Layer> layers = act[actionIndex].Frames[_frameIndex % act[actionIndex].NumberOfFrames].Layers.Where(p => p.SpriteIndex >= 0).ToList();

				if (layers.Count <= 0) {
					_imagePreview.Dispatch(p => p.Source = null);
					return;
				}

				bool isValid = (bool) _imagePreview.Dispatch(delegate {
					try {
						this.Dispatch(delegate {
							try {
								if (_changedAnimationIndex) {
									_frameIndex = 0;
									_changedAnimationIndex = false;
								}

								BitmapScalingMode mode = GrfEditorConfiguration.PreviewActScaleType ? BitmapScalingMode.HighQuality : BitmapScalingMode.NearestNeighbor;
								ImageSource source = Imaging.GenerateImage(act, actionIndex, _frameIndex, mode);

								_imagePreview.Margin = new Thickness(
									(int) (10 + _scrollViewer.ActualWidth / 2 - (double) source.Dispatch(() => source.Width) / 2),
									(int) (10 + _scrollViewer.ActualHeight / 2 - (double) source.Dispatch(() => source.Height) / 2),
									0, 0);
								_imagePreview.Source = source;
							}
							catch {
								_enableActThread = false;
								ErrorHandler.HandleException("Unable to load the animation.");
							}
						});

						return true;
					}
					catch {
						return false;
					}
				});

				if (!isValid)
					throw new Exception("Unable to load the animation.");
			}
			catch (Exception err) {
				_enableActThread = false;
				ErrorHandler.HandleException(err, ErrorLevel.Warning);
			}
			finally {
				if (_stopAnimation) {
					_enableActThread = false;
				}
			}
		}

		private void _comboBoxActionIndex_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			try {
				if (_isCancelRequired()) return;
				if (_comboBoxActionIndex.SelectedIndex < 0) return;

				if (!_stopAnimation) {
					int actionIndex = _comboBoxActionIndex.SelectedIndex;
					int animationIndex = actionIndex / 8;
					_disableEvents();
					_comboBoxAnimationIndex.SelectedIndex = animationIndex;
					_fancyButton_Click(_fancyButtons.First(p => p.Tag.ToString() == (actionIndex % 8).ToString(CultureInfo.InvariantCulture)), null);
					_setDisabledButtons();
					_enableEvents();

					if (actionIndex < 0)
						return;

					if ((int) _act[actionIndex].AnimationSpeed * 24 == 0 ||
					    float.IsNaN(_act[actionIndex].AnimationSpeed)) {
						if (_act[actionIndex].Frames[0].Layers[0].SpriteIndex < 0) {
							_imagePreview.Source = null;
							return;
						}

						_imagePreview.Source = _act.Sprite.Images[_act[actionIndex].Frames[0].Layers[0].SpriteIndex].Cast<BitmapSource>();
					}
					else {
						_actThreadSleepDelay = (int) (_act[actionIndex].AnimationSpeed * 24);
					}

					_actionIndex = actionIndex;
					_changedAnimationIndex = true;
					_enableActThread = true;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err, ErrorLevel.Warning);
			}
		}

		private void _buttonExportAsGif_Click(object sender, RoutedEventArgs e) {
			try {
				GrfToWpfBridge.Imaging.SaveTo(_act, _actionIndex, _entry.RelativePath, PathRequest.ExtractSetting, _asyncOperation);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _fancyButton_Click(object sender, RoutedEventArgs e) {
			int animationIndex = _comboBoxActionIndex.SelectedIndex / 8;

			_fancyButtons.ForEach(p => p.IsPressed = false);
			((FancyButton) sender).IsPressed = true;

			_comboBoxActionIndex.SelectedIndex = animationIndex * 8 + Int32.Parse(((FancyButton) sender).Tag.ToString());
		}

		private void _disableEvents() {
			_comboBoxAnimationIndex.SelectionChanged -= _comboBoxAnimationIndex_SelectionChanged;
			_fancyButtons.ForEach(p => p.Click -= _fancyButton_Click);
		}

		private void _enableEvents() {
			_comboBoxAnimationIndex.SelectionChanged += _comboBoxAnimationIndex_SelectionChanged;
			_fancyButtons.ForEach(p => p.Click += _fancyButton_Click);
		}

		private void _comboBoxAnimationIndex_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_comboBoxAnimationIndex.SelectedIndex < 0) return;

			int direction = _comboBoxActionIndex.SelectedIndex % 8;

			if (8 * _comboBoxAnimationIndex.SelectedIndex + direction >= _act.NumberOfActions) {
				_comboBoxActionIndex.SelectedIndex = 8 * _comboBoxAnimationIndex.SelectedIndex;
			}
			else {
				_comboBoxActionIndex.SelectedIndex = 8 * _comboBoxAnimationIndex.SelectedIndex + direction;
			}
		}

		private void _setDisabledButtons() {
			this.Dispatch(delegate {
				int animationIndex = _comboBoxActionIndex.SelectedIndex / 8;

				_fancyButtons.ForEach(p => p.IsButtonEnabled = true);

				if ((animationIndex + 1) * 8 > _act.NumberOfActions) {
					int toDisable = (animationIndex + 1) * 8 - _act.NumberOfActions;

					for (int i = 0; i < toDisable; i++) {
						int disabledIndex = 7 - i;
						_fancyButtons.First(p => Int32.Parse(p.Tag.ToString()) == disabledIndex).IsButtonEnabled = false;
					}
				}
			});
		}

		#region IDisposable members

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~PreviewAct() {
			Dispose(false);
		}

		protected void Dispose(bool disposing) {
			if (disposing) {
				if (_actThreadHandle != null) {
					_actThreadHandle.Close();
				}
			}
		}

		#endregion
	}
}