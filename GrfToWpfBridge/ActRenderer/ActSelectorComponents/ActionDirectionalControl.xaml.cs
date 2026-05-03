using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TokeiLibrary.WPF.Styles;

namespace GrfToWpfBridge.ActRenderer.ActSelectorComponents {
	/// <summary>
	/// Interaction logic for ActionDirectionalControl.xaml
	/// </summary>
	public partial class ActionDirectionalControl : UserControl {
		private IActIndexSelector _selector;
		private IFrameRendererEditor _editor;
		private List<FancyButton> _fancyButtons;
		private bool _isUpdating = false;

		public Grid DirectionalGrid => _directionalGrid;

		public ActionDirectionalControl() {
			InitializeComponent();

			_fancyButtons = new FancyButton[] { _fancyButton0, _fancyButton1, _fancyButton2, _fancyButton3, _fancyButton4, _fancyButton5, _fancyButton6, _fancyButton7 }.ToList();
			ActIndexSelectorHelper.BuildDirectionalActionSelectorUI(_fancyButtons, false);
			UpdateButtonSize();
		}

		public void Init(IActIndexSelector selector, IFrameRendererEditor editor) {
			CleanupEvents();

			_fancyButtons.ForEach(p => p.IsPressed = false);
			_selector = selector;
			_editor = editor;
			_selector.ActionChanged += _selector_ActionChanged;
			_editor.ActLoaded += _editor_ActLoaded;

			if (_editor.Act != null)
				_editor_ActLoaded(null);
		}

		public void CleanupEvents() {
			if (_selector != null) {
				_selector.ActionChanged -= _selector_ActionChanged;
			}

			if (_editor != null) {
				_editor.ActLoaded -= _editor_ActLoaded;

				if (_editor.Act != null) {
					_editor.Act.ActionCountChanged -= _act_ActionCountChanged;
				}
			}
		}

		private void _editor_ActLoaded(object sender) {
			if (_editor.Act != null)
				_editor.Act.ActionCountChanged += _act_ActionCountChanged;
			else
				_setDisabledButtons();
		}

		private void _act_ActionCountChanged(object sender) {
			_setDisabledButtons();
		}

		public static readonly DependencyProperty ButtonSizeProperty = DependencyProperty.Register("ButtonSize", typeof(double), typeof(ActionDirectionalControl), new PropertyMetadata(16.0, OnButtonSizeChanged));

		public double ButtonSize {
			get => (double)GetValue(ButtonSizeProperty);
			set => SetValue(ButtonSizeProperty, value);
		}

		private static void OnButtonSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var control = (ActionDirectionalControl)d;
			double newValue = (double)e.NewValue;

			control.UpdateButtonSize();
		}

		private void UpdateButtonSize() {
			if (_fancyButtons == null)
				return;

			double newSize = ButtonSize;

			foreach (var fb in _fancyButtons) {
				fb.Width = newSize;
				fb.Height = newSize;
			}
		}

		private void _fancyButton_Click(object sender, RoutedEventArgs e) {
			ValidateState();

			if (_isUpdating) return;

			int animationIndex = _selector.SelectedAction / 8;
			int oldFrameIndex = _selector.SelectedFrame;

			var fb = (FancyButton)sender;

			_fancyButtons.ForEach(p => p.IsPressed = false);
			fb.IsPressed = true;

			_selector.SelectedAction = animationIndex * 8 + _fancyButtons.IndexOf(fb);

			if (oldFrameIndex < _editor.Act[_selector.SelectedAction].Frames.Count)
				_selector.SelectedFrame = oldFrameIndex;
		}

		private void _selector_ActionChanged(int frameIndex) {
			ValidateState();

			int actionIndex = _selector.SelectedAction;
			_fancyButton_Click(_fancyButtons[actionIndex % 8], null);
			_setDisabledButtons();
		}

		private void _setDisabledButtons() {
			if (_editor.Act == null) {
				_fancyButtons.ForEach(p => p.IsButtonEnabled = false);
				return;
			}

			int baseAnimationIndex = _selector.SelectedAction / 8 * 8;

			for (int i = 0; i < 8; i++) {
				_fancyButtons[i].IsButtonEnabled = baseAnimationIndex + i < _editor.Act.NumberOfActions;
			}
		}

		private void ValidateState() {
			if (_selector == null)
				throw new Exception("No IActIndexSelector was set through the Init method.");
			if (_editor == null)
				throw new Exception("No IFrameRendererEditor was set through the Init method.");
		}

		public void Reset() {
			_fancyButtons.ForEach(p => p.IsPressed = false);
			_fancyButtons.ForEach(p => p.IsButtonEnabled = false);
		}
	}
}
