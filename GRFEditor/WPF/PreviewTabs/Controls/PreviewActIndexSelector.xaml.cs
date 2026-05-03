using GRF.Threading;
using GrfToWpfBridge.ActRenderer;
using GrfToWpfBridge.ActRenderer.ActSelectorComponents;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary;
using ActEditor.Core.WPF.EditorControls.ActSelectorComponents;
using static GRFEditor.WPF.PreviewTabs.PreviewAct;

namespace GRFEditor.WPF.PreviewTabs.Controls {
	/// <summary>
	/// Interaction logic for PreviewActIndexSelector.xaml
	/// </summary>
	public partial class PreviewActIndexSelector : UserControl, IActIndexSelector {
		private bool _handlersEnabled = true;
		private IFrameRendererEditor _editor;
		public IFrameRendererEditor Editor => _editor;
		private FancyButton _play = new FancyButton();
		private bool _firstInitDone;
		private ActRenderThread _thread;

		public PreviewActIndexSelector() {
			InitializeComponent();

			_setupPlayButtonUI();

			Unloaded += delegate {
				Stop();
			};

			Dispatcher.ShutdownStarted += delegate {
				_thread.IsTerminated = true;
				_thread.Resume();
			};
		}

		private void _setupPlayButtonUI() {
			_directionalControl.DirectionalGrid.Children.Add(_play);
			_play.SetValue(Grid.ColumnProperty, 1);
			_play.SetValue(Grid.RowProperty, 1);
			_play.Width = 16;
			_play.Height = 16;

			_updatePlay();
			_play.Click += _play_Click;
		}

		private int _selectedFrame;
		private int _selectedAction;

		public int SelectedAction {
			get => _selectedAction;
			set {
				if (value == _selectedAction)
					return;

				int max = _editor.Act.NumberOfActions;
				_selectedAction = (value % max + max) % max;

				// This should always be done on the main UI thread
				this.Dispatch(_ => {
					if (SelectedFrame >= _editor.Act[_selectedAction].Frames.Count)
						SelectedFrame = 0;

					OnActionChanged(_selectedAction);
				});
			}
		}

		public int SelectedFrame {
			get => _selectedFrame;
			set {
				if (value == _selectedFrame)
					return;

				var act = _editor.Act;
				var selectedAction = ActAnimation.SafeActionIndex(act, SelectedAction);
				int max = _editor.Act[selectedAction].NumberOfFrames;
				_selectedFrame = (value % max + max) % max;

				// This should always be done on the main UI thread
				this.Dispatch(_ => {
					OnFrameChanged(_selectedFrame);
				});
			}
		}

		public event FrameRendererEventDelegates.IndexChangedDelegate ActionChanged;
		public event FrameRendererEventDelegates.IndexChangedDelegate FrameChanged;
		public event FrameRendererEventDelegates.IndexChangedDelegate SpecialFrameChanged;

		public bool IsPlaying { get; private set; }

		public void OnSpecialFrameChanged(int frameIndex) {
			if (!_handlersEnabled) return;
			SpecialFrameChanged?.Invoke(frameIndex);
		}

		public event FrameRendererEventDelegates.AnimationStateEventHandler AnimationPlaying;

		public void OnAnimationPlaying(AnimationState state) {
			AnimationPlaying?.Invoke(state);
		}

		public void OnFrameChanged(int frameIndex) {
			if (!_handlersEnabled) return;
			FrameChanged?.Invoke(frameIndex);
		}

		public void OnActionChanged(int actionIndex) {
			_updateAction();
			if (!_handlersEnabled) return;
			ActionChanged?.Invoke(actionIndex);
		}

		private void _play_Click(object sender, RoutedEventArgs e) {
			if (IsPlaying)
				Stop();
			else
				Play();
		}

		public void Play() {
			if (IsPlaying) return;

			_play.Dispatch(delegate {
				_play.IsPressed = true;
				IsPlaying = true;
				_updatePlay();
				_thread.Resume();
			});
		}

		public class ActRenderThread : PausableThread {
			private IActIndexSelector _selector;
			private IFrameRendererEditor _editor;

			public bool IsEnabled { get; set; } = true;

			public ActRenderThread(IActIndexSelector selector, IFrameRendererEditor editor) {
				_selector = selector;
				_editor = editor;
			}

			public void Start() {
				GrfThread.Start(_start, "GRF - ActRenderThread thread starter");
			}

			private void _start() {
				while (!IsTerminated) {
					if (!_selector.IsPlaying)
						Pause();

					ActAnimation.DoThread(_selector, _editor);
				}
			}
		}

		public void Stop() {
			if (!IsPlaying) return;

			_play.Dispatch(delegate {
				_play.IsPressed = false;
				IsPlaying = false;
				_updatePlay();
			});
		}

		private void _updatePlay() {
			if (_play.IsPressed) {
				_play.ImagePath = "stop2.png";
				_play.ImageIcon.Width = 16;
				_play.ImageIcon.Stretch = Stretch.Fill;
			}
			else {
				_play.ImagePath = "play.png";
				_play.ImageIcon.Width = 16;
				_play.ImageIcon.Stretch = Stretch.Fill;
			}
		}

		private void _updateAction() {
			if (_editor.Act == null) return;

			if (SelectedAction >= _editor.Act.NumberOfActions) {
				int lastAnimationIndex = _editor.Act.NumberOfActions / 8 - 1;
				int baseAction = SelectedAction % 8;

				if (lastAnimationIndex < 0) {
					SelectedAction = baseAction;
				}
				else {
					SelectedAction = lastAnimationIndex * 8 + baseAction;
				}

				// Fallback
				if (SelectedAction >= _editor.Act.NumberOfActions) {
					SelectedAction = _editor.Act.NumberOfActions - 1;
				}
			}

			if (SelectedFrame >= _editor.Act[_editor.SelectedAction].NumberOfFrames && SelectedFrame > 0) {
				SelectedFrame = Math.Max(0, _editor.Act[SelectedAction].NumberOfFrames - 1);
			}
		}

		public void Init(IFrameRendererEditor editor, int selectedAction, int selectedFrame) {
			if (_thread == null) {
				_thread = new ActRenderThread(this, editor);
				_thread.Start();
			}

			if (editor.Act != null && selectedAction >= editor.Act.NumberOfActions) {
				selectedAction = ActAnimation.SafeActionIndex(editor.Act, selectedAction);
			}

			_editor = editor;
			_directionalControl.Init(this, _editor);
			if (!_firstInitDone) {
				ActSelectorHelper.InitSelectorComboBox(_editor, _comboBoxActionIndex, _comboBoxAnimationIndex);
				_comboBoxActionIndex.SelectionChanged += _comboBoxActionIndex_SelectionChanged;
			}
			if (editor.Act != null) {
				SelectedAction = selectedAction;
				SelectedFrame = selectedFrame;
				editor.IndexSelector.OnActionChanged(SelectedAction);
			}
			_firstInitDone = true;
		}

		private void _comboBoxActionIndex_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_comboBoxActionIndex.SelectedIndex < 0) return;
			if (_editor is FrameRendererEditor fre) {
				if (fre.IsLoading)
					return;

				fre.PreferedLoadingAction = _comboBoxActionIndex.SelectedIndex;
			}
		}

		public void DisableActionChange() {
			_directionalControl.Reset();
			_comboBoxActionIndex.IsEnabled = false;
			_comboBoxAnimationIndex.IsEnabled = false;
		}
	}
}
