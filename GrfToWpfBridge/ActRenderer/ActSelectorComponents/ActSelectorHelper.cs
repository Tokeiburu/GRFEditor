using ErrorManager;
using GRF.FileFormats.ActFormat.Commands;
using System;
using System.Linq;
using System.Windows.Controls;

namespace GrfToWpfBridge.ActRenderer.ActSelectorComponents {
	public static class ActSelectorHelper {
		public class ActSelectorData {
			public IFrameRendererEditor Editor;
			public ComboBox ActionComboBox;
			public ComboBox AnimationComboBox;
			public GRF.FileFormats.ActFormat.Act Act => Editor.Act;
			public bool LoadedEvents;
		}

		public static void InitSelectorComboBox(IFrameRendererEditor editor, ComboBox actionBox, ComboBox animationBox) {
			var data = new ActSelectorData();
			data.Editor = editor;
			data.ActionComboBox = actionBox;
			data.AnimationComboBox = animationBox;

			editor.ActLoaded += (s) => _editor_ActLoaded(data);

			if (editor.Act != null)
				_editor_ActLoaded(data);
		}

		private static void _editor_ActLoaded(ActSelectorData data) {
			if (data.Act == null) {
				data.AnimationComboBox.ItemsSource = null;
				data.ActionComboBox.ItemsSource = null;
				return;
			}

			bool updatesEnabled = true;

			if (!data.LoadedEvents) {
				data.Editor.IndexSelector.ActionChanged += actionIndex => {
					if (actionIndex > -1 && actionIndex < data.Act.NumberOfActions)
						data.ActionComboBox.SelectedIndex = actionIndex;
				};

				data.ActionComboBox.SelectionChanged += delegate {
					if (!updatesEnabled) return;
					if (data.ActionComboBox.SelectedIndex < 0) return;
					if (data.ActionComboBox.SelectedIndex >= data.Editor.Act.NumberOfActions) return;

					int actionIndex = data.ActionComboBox.SelectedIndex;
					int animationIndex = actionIndex / 8;

					updatesEnabled = false;
					data.AnimationComboBox.SelectedIndex = animationIndex;
					data.Editor.IndexSelector.SelectedAction = actionIndex;
					data.Editor.IndexSelector.SelectedFrame = 0;
					updatesEnabled = true;
				};

				data.AnimationComboBox.SelectionChanged += delegate {
					if (!updatesEnabled) return;
					if (data.AnimationComboBox.SelectedIndex < 0) return;

					int direction = data.ActionComboBox.SelectedIndex % 8;

					if (8 * data.AnimationComboBox.SelectedIndex + direction >= data.Act.NumberOfActions) {
						data.ActionComboBox.SelectedIndex = 8 * data.AnimationComboBox.SelectedIndex;
					}
					else {
						data.ActionComboBox.SelectedIndex = 8 * data.AnimationComboBox.SelectedIndex + direction;
					}
				};

				data.LoadedEvents = true;
			}

			var proxy = new WeakCommandIndexChangedProxy((s, c) => _actionCommands_CommandIndexChanged(s, c, data));
			data.Act.Commands.CommandIndexChanged += proxy.OnEvent;

			int actions = data.Act.NumberOfActions;

			data.AnimationComboBox.ItemsSource = data.Act.GetAnimations();
			data.ActionComboBox.ItemsSource = Enumerable.Range(0, actions);

			if (actions != 0) {
				data.ActionComboBox.SelectedIndex = data.Editor.SelectedAction;
				data.AnimationComboBox.SelectedIndex = data.Editor.SelectedAction / 8;
			}
		}

		public class WeakCommandIndexChangedProxy {
			private readonly WeakReference<Action<object, IActCommand>> _handlerRef;

			public WeakCommandIndexChangedProxy(Action<object, IActCommand> handler) {
				_handlerRef = new WeakReference<Action<object, IActCommand>>(handler);
			}

			public void OnEvent(object sender, IActCommand command) {
				if (_handlerRef.TryGetTarget(out var handler)) {
					handler(sender, command);
				}
			}
		}

		private static void _actionCommands_CommandIndexChanged(object sender, IActCommand command, ActSelectorData data) {
			try {
				var actionCmd = GetCommand<ActionCommand>(command);

				if (actionCmd != null) {
					_updateActionSelection(data);

					// This has nothing to do with the ComboBox, but it needs to be executed after the selection has been updated.
					if (actionCmd.Executed &&
						(actionCmd.Edit == ActionCommand.ActionEdit.CopyAt ||
						 actionCmd.Edit == ActionCommand.ActionEdit.InsertAt ||
						 actionCmd.Edit == ActionCommand.ActionEdit.ReplaceTo ||
						 actionCmd.Edit == ActionCommand.ActionEdit.InsertAt)) {
						data.Editor.IndexSelector.SelectedAction = actionCmd.ActionIndexTo;
					}
				}

				var frameCmd = GetCommand<FrameCommand>(command);

				if (frameCmd != null) {
					if (frameCmd.Executed) {
						if (frameCmd.ActionIndexTo == data.Editor.IndexSelector.SelectedAction && frameCmd.Edit == FrameCommand.FrameEdit.ReplaceTo ||
							frameCmd.ActionIndexTo == data.Editor.IndexSelector.SelectedAction && frameCmd.Edit == FrameCommand.FrameEdit.Switch ||
							frameCmd.ActionIndexTo == data.Editor.IndexSelector.SelectedAction && frameCmd.Edit == FrameCommand.FrameEdit.CopyTo
							) {
							data.Editor.IndexSelector.SelectedFrame = frameCmd.FrameIndexTo;
						}
						else if (frameCmd.ActionIndex == data.Editor.IndexSelector.SelectedAction && frameCmd.Edit == FrameCommand.FrameEdit.InsertTo) {
							data.Editor.IndexSelector.SelectedFrame = frameCmd.FrameIndex;
						}
					}
				}

				var backupCmd = GetCommand<BackupCommand>(command);
				var actBackupCmd = GetCommand<ActEditCommand>(command);

				if (backupCmd != null || actBackupCmd != null) {
					_updateActionSelection(data);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private static void _updateActionSelection(ActSelectorData data) {
			try {
				int selectedAction = data.Editor.IndexSelector.SelectedAction;
				int frameIndex = data.Editor.IndexSelector.SelectedFrame;

				var animations = data.Editor.Act.GetAnimations();
				var actions = Enumerable.Range(0, data.Editor.Act.NumberOfActions);

				bool animationsChanged = animations.SequenceEqual(data.AnimationComboBox.Items.Cast<string>());

				if (animationsChanged) {
					data.AnimationComboBox.ItemsSource = null;
					data.AnimationComboBox.ItemsSource = animations;
				}

				bool actionsChanged = actions.Count() != data.ActionComboBox.Items.Count;

				if (actionsChanged) {
					data.ActionComboBox.ItemsSource = null;
					data.ActionComboBox.ItemsSource = actions;
				}

				// ?? Impossible
				if (selectedAction >= data.ActionComboBox.Items.Count) {
					data.ActionComboBox.SelectedIndex = data.ActionComboBox.Items.Count - 1;
				}

				if (data.ActionComboBox.SelectedIndex != selectedAction)
					data.ActionComboBox.SelectedIndex = selectedAction;

				if (data.AnimationComboBox.SelectedIndex != selectedAction / 8)
					data.AnimationComboBox.SelectedIndex = selectedAction / 8;

				data.Editor.FrameRenderer.Update();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public static T GetCommand<T>(IActCommand command) where T : class, IActCommand {
			var cmd = command as ActGroupCommand;

			if (cmd != null) {
				return cmd.Commands.FirstOrDefault(p => p.GetType() == typeof(T)) as T;
			}

			if (command is T) {
				return command as T;
			}

			return null;
		}
	}
}
