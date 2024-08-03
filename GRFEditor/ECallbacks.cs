using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GRF.ContainerFormat.Commands;
using GRF.Core;
using GRFEditor.Core.Services;
using TokeiLibrary;
using TokeiLibrary.WPF;

namespace GRFEditor {
	public partial class EditorMainWindow : Window {
		private void _loadEvents() {
			_positions.RedoExecuted += _positions_RedoExecuted;
			_positions.UndoExecuted += _positions_UndoExecuted;
			_positions.CommandExecuted += _positions_CommandExecuted;

			MouseDown += (s, e) => {
				var box = Keyboard.FocusedElement as TextBox;
				
				if (box != null && box.Name == "_tbEdit") {
					Keyboard.ClearFocus();
					Keyboard.Focus(_treeView.SelectedItem as TkTreeViewItem);
				}
			};
		}

		private void _grfHolder_ModifiedStateChanged(object sender, IContainerCommand<FileEntry> command) {
			Dispatcher.Invoke(new Action(() => _setupTitle(_grfHolder.IsNewGrf || _grfHolder.IsModified)));
			_buttonUndo.Dispatch(p => p.IsEnabled = _grfHolder.Commands.CanUndo);
			_buttonRedo.Dispatch(p => p.IsEnabled = _grfHolder.Commands.CanRedo);
		}

		private void _positions_RedoExecuted(object sender) {
			PreviewService.Select(_treeView, _items, _positions.GetCurrentPath().RelativePath);
			_buttonPositionRedo.IsEnabled = _positions.CanRedo;
			_buttonPositionUndo.IsEnabled = _positions.CanUndo;
			_buttonFancyBackward.Dispatch(p => p.IsButtonEnabled = _positions.CanUndo);
			_buttonFancyForward.Dispatch(p => p.IsButtonEnabled = _positions.CanRedo);
		}

		private void _positions_UndoExecuted(object sender) {
			PreviewService.Select(_treeView, _items, _positions.GetCurrentPath().RelativePath);
			_buttonPositionRedo.IsEnabled = _positions.CanRedo;
			_buttonPositionUndo.IsEnabled = _positions.CanUndo;
			_buttonFancyBackward.Dispatch(p => p.IsButtonEnabled = _positions.CanUndo);
			_buttonFancyForward.Dispatch(p => p.IsButtonEnabled = _positions.CanRedo);
		}

		private void _positions_CommandExecuted(object sender) {
			_buttonPositionRedo.IsEnabled = _positions.CanRedo;
			_buttonPositionUndo.IsEnabled = _positions.CanUndo;
			_buttonFancyBackward.Dispatch(p => p.IsButtonEnabled = _positions.CanUndo);
			_buttonFancyForward.Dispatch(p => p.IsButtonEnabled = _positions.CanRedo);
		}
	}
}