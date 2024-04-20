using System;
using System.Linq;
using GRF.FileFormats.SprFormat;
using GRF.FileFormats.SprFormat.Commands;
using GRF.Image;
using GRF.Image.Decoders;
using Utilities.Commands;

namespace GRF.FileFormats.ActFormat.Commands {
	public class CommandsHolder : AbstractCommand<IActCommand> {
		private readonly Act _act;

		public CommandsHolder(Act act) {
			_act = act;
		}

		public static string GetId(int actionIndex) {
			return "[" + actionIndex + "]";
		}

		public static string GetId(int actionIndex, int frameIndex) {
			return "[" + actionIndex + ", " + frameIndex + "]";
		}

		public static string GetId(int actionIndex, int frameIndex, int layerIndex) {
			return "[" + actionIndex + ", " + frameIndex + ", " + layerIndex + "]";
		}

		protected override void _execute(IActCommand command) {
			command.Execute(_act);
		}

		protected override void _undo(IActCommand command) {
			command.Undo(_act);
		}

		protected override void _redo(IActCommand command) {
			command.Execute(_act);
		}

		/// <summary>
		/// Backups the Act and execute the action
		/// </summary>
		/// <param name="action">The action.</param>
		public void Backup(Action<Act> action) {
			_act.Commands.StoreAndExecute(new BackupCommand(action));
		}

		/// <summary>
		/// Backups the Act and execute the action
		/// </summary>
		/// <param name="action">The action.</param>
		/// <param name="commandName">Name of the command.</param>
		public void Backup(Action<Act> action, string commandName) {
			_act.Commands.StoreAndExecute(new BackupCommand(action, commandName));
		}


		/// <summary>
		/// Backups the Act and execute the action
		/// </summary>
		/// <param name="action">The action.</param>
		/// <param name="commandName">Name of the command.</param>
		/// <param name="forceReload">if set to <c>true</c> [force reload].</param>
		public void Backup(Action<Act> action, string commandName, bool forceReload) {
			_act.Commands.StoreAndExecute(new BackupCommand(action, commandName, forceReload));
		}

		/// <summary>
		/// Begins the commands stack grouping.
		/// </summary>
		public void Begin() {
			_act.Commands.BeginEdit(new ActGroupCommand(_act, false));
		}

		/// <summary>
		/// Begins the commands stack grouping and apply commands as soon as they're received.
		/// </summary>
		public void BeginNoDelay() {
			_act.Commands.BeginEdit(new ActGroupCommand(_act, true));
		}

		/// <summary>
		/// Ends the commands stack grouping.
		/// </summary>
		public void End() {
			_act.Commands.EndEdit();
		}

		#region Transform

		/// <summary>
		/// Rotates all layers of the specified amount.
		/// </summary>
		/// <param name="rotation">The rotation.</param>
		public void Rotate(int rotation) {
			if (rotation == 0) return;

			_act.Commands.StoreAndExecute(new RotateCommand(rotation));
		}

		/// <summary>
		/// Rotates all layers in the action index.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="rotation">The rotation.</param>
		public void Rotate(int actionIndex, int rotation) {
			if (rotation == 0) return;

			_act.Commands.StoreAndExecute(new RotateCommand(rotation, actionIndex));
		}

		/// <summary>
		/// Rotates all layers in the frame specified.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="rotation">The rotation.</param>
		public void Rotate(int actionIndex, int frameIndex, int rotation) {
			if (rotation == 0) return;

			_act.Commands.StoreAndExecute(new RotateCommand(rotation, actionIndex, frameIndex));
		}

		/// <summary>
		/// Rotates the specified suframe.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layerIndex">Index of the layer.</param>
		/// <param name="rotation">The rotation.</param>
		public void Rotate(int actionIndex, int frameIndex, int layerIndex, int rotation) {
			if (rotation == 0) return;

			_act.Commands.StoreAndExecute(new RotateCommand(rotation, actionIndex, frameIndex, layerIndex));
		}

		/// <summary>
		/// Translates the entire Act by the specified offsets.
		/// </summary>
		/// <param name="offsetX">The offset X.</param>
		/// <param name="offsetY">The offset Y.</param>
		public void Translate(int offsetX, int offsetY) {
			if (offsetX == 0 && offsetY == 0) return;

			_act.Commands.StoreAndExecute(new TranslateCommand(offsetX, offsetY));
		}

		/// <summary>
		/// Translates the specified action by the specified offsets.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="offsetX">The offset X.</param>
		/// <param name="offsetY">The offset Y.</param>
		public void Translate(int actionIndex, int offsetX, int offsetY) {
			if (offsetX == 0 && offsetY == 0) return;

			_act.Commands.StoreAndExecute(new TranslateCommand(actionIndex, offsetX, offsetY));
		}

		/// <summary>
		/// Translates the specified frame by the specified offsets.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="offsetX">The offset X.</param>
		/// <param name="offsetY">The offset Y.</param>
		public void Translate(int actionIndex, int frameIndex, int offsetX, int offsetY) {
			if (offsetX == 0 && offsetY == 0) return;

			_act.Commands.StoreAndExecute(new TranslateCommand(actionIndex, frameIndex, offsetX, offsetY));
		}

		/// <summary>
		/// Translates the specified layer by the specified offsets.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layerIndex">Index of the layer.</param>
		/// <param name="offsetX">The offset X.</param>
		/// <param name="offsetY">The offset Y.</param>
		public void Translate(int actionIndex, int frameIndex, int layerIndex, int offsetX, int offsetY) {
			if (offsetX == 0 && offsetY == 0) return;

			_act.Commands.StoreAndExecute(new TranslateCommand(actionIndex, frameIndex, layerIndex, offsetX, offsetY));
		}

		/// <summary>
		/// Sets the rotation.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layerIndex">Index of the layer.</param>
		/// <param name="rotation">The rotation.</param>
		public void SetRotation(int actionIndex, int frameIndex, int layerIndex, int rotation) {
			if (rotation == _act[actionIndex, frameIndex, layerIndex].Rotation) return;

			_act.Commands.StoreAndExecute(new RotateCommand(rotation - _act[actionIndex, frameIndex, layerIndex].Rotation, actionIndex, frameIndex, layerIndex));
		}

		/// <summary>
		/// Scales the entire Act. This method does not scale the anchors' offsets.
		/// </summary>
		/// <param name="scale">The scale.</param>
		public void Scale(float scale) {
			if (scale == 0) return;
			if (scale == 1) return;

			_act.Commands.StoreAndExecute(new ScaleCommand(scale, scale));
		}

		/// <summary>
		/// Scales the entire Act. This method does not scale the anchor's offsets.
		/// </summary>
		/// <param name="scaleX">The scale X.</param>
		/// <param name="scaleY">The scale Y.</param>
		public void Scale(float scaleX, float scaleY) {
			if (scaleX == 0 || scaleY == 0) return;
			if (scaleX == 1 && scaleY == 1) return;

			_act.Commands.StoreAndExecute(new ScaleCommand(scaleX, scaleY));
		}

		/// <summary>
		/// Scales an action.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="scaleX">The scale X.</param>
		/// <param name="scaleY">The scale Y.</param>
		public void Scale(int actionIndex, float scaleX, float scaleY) {
			if (scaleX == 0 || scaleY == 0) return;
			if (scaleX == 1 && scaleY == 1) return;

			_act.Commands.StoreAndExecute(new ScaleCommand(actionIndex, scaleX, scaleY));
		}

		/// <summary>
		/// Scales a frame.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="scaleX">The scale X.</param>
		/// <param name="scaleY">The scale Y.</param>
		public void Scale(int actionIndex, int frameIndex, float scaleX, float scaleY) {
			if (scaleX == 0 || scaleY == 0) return;
			if (scaleX == 1 && scaleY == 1) return;

			_act.Commands.StoreAndExecute(new ScaleCommand(actionIndex, frameIndex, scaleX, scaleY));
		}

		/// <summary>
		/// Scales a layer.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layerIndex">Index of the layer.</param>
		/// <param name="scaleX">The scale X.</param>
		/// <param name="scaleY">The scale Y.</param>
		public void Scale(int actionIndex, int frameIndex, int layerIndex, float scaleX, float scaleY) {
			if (scaleX == 0 || scaleY == 0) return;
			if (scaleX == 1 && scaleY == 1) return;

			_act.Commands.StoreAndExecute(new ScaleCommand(actionIndex, frameIndex, layerIndex, scaleX, scaleY));
		}

		/// <summary>
		/// Sets the scale of a layer.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layerIndex">Index of the layer.</param>
		/// <param name="scaleX">The scale X.</param>
		/// <param name="scaleY">The scale Y.</param>
		public void SetScale(int actionIndex, int frameIndex, int layerIndex, float scaleX, float scaleY) {
			_act.Commands.StoreAndExecute(new SetScaleCommand(actionIndex, frameIndex, layerIndex, scaleX, scaleY));
		}

		/// <summary>
		/// Sets the scale X of a layer.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layerIndex">Index of the layer.</param>
		/// <param name="scale">The scale.</param>
		public void SetScaleX(int actionIndex, int frameIndex, int layerIndex, float scale) {
			_act.Commands.StoreAndExecute(new SetScaleCommand(actionIndex, frameIndex, layerIndex, scale, _act[actionIndex, frameIndex, layerIndex].ScaleY));
		}

		/// <summary>
		/// Sets the scale Y of a layer.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layerIndex">Index of the layer.</param>
		/// <param name="scale">The scale.</param>
		public void SetScaleY(int actionIndex, int frameIndex, int layerIndex, float scale) {
			_act.Commands.StoreAndExecute(new SetScaleCommand(actionIndex, frameIndex, layerIndex, _act[actionIndex, frameIndex, layerIndex].ScaleX, scale));
		}

		#endregion

		#region Action

		/// <summary>
		/// Sets the interval for an action.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="speed">The speed.</param>
		public void SetInterval(int actionIndex, float speed) {
			if (_act[actionIndex].AnimationSpeed == speed)
				return;

			_act.Commands.StoreAndExecute(new AnimationSpeedCommand(actionIndex, speed));
		}

		/// <summary>
		/// Inserts an action at the specified index.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		public void ActionInsertAt(int actionIndex) {
			if (actionIndex < 0 || actionIndex > _act.NumberOfActions) throw new Exception("Cannot insert the action (the index must be between 0 and " + (_act.NumberOfActions - 1) + ".");

			_act.Commands.StoreAndExecute(new ActionCommand(actionIndex, ActionCommand.ActionEdit.InsertAt));
		}

		/// <summary>
		/// Replaces the action at the specified index.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="actionIndexTo">The action index destination</param>
		public void ActionReplaceTo(int actionIndex, int actionIndexTo) {
			if (actionIndex == actionIndexTo) return;
			if (actionIndex < 0 || actionIndex >= _act.NumberOfActions) throw new Exception("Cannot replace the action (the index must be between 0 and " + (_act.NumberOfActions - 1) + ".");
			if (actionIndexTo < 0 || actionIndexTo >= _act.NumberOfActions) throw new Exception("Cannot replace the action (the index must be between 0 and " + (_act.NumberOfActions - 1) + ".");

			_act.Commands.StoreAndExecute(new ActionCommand(actionIndex, actionIndexTo, ActionCommand.ActionEdit.ReplaceTo));
		}

		/// <summary>
		/// Copies and inserts an action at the specified index.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="actionIndexTo">The action index destination</param>
		public void ActionCopyAt(int actionIndex, int actionIndexTo) {
			if (actionIndex < 0 || actionIndex >= _act.NumberOfActions) throw new Exception("Cannot replace the action (the index must be between 0 and " + (_act.NumberOfActions - 1) + ".");
			if (actionIndexTo < 0 || actionIndexTo > _act.NumberOfActions) throw new Exception("Cannot replace the action (the index must be between 0 and " + (_act.NumberOfActions) + ".");

			_act.Commands.StoreAndExecute(new ActionCommand(actionIndex, actionIndexTo, ActionCommand.ActionEdit.CopyAt));
		}

		/// <summary>
		/// Moves a range of actions.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="range">The range.</param>
		/// <param name="actionIndexTo">The action index destination</param>
		public void ActionMoveRange(int actionIndex, int range, int actionIndexTo) {
			if (actionIndex == actionIndexTo) return;
			if (actionIndex < 0 || actionIndex >= _act.NumberOfActions) throw new Exception("Cannot replace the action (the index must be between 0 and " + (_act.NumberOfActions - 1) + ".");
			if (actionIndexTo < 0 || actionIndexTo >= _act.NumberOfActions) throw new Exception("Cannot replace the action (the index must be between 0 and " + (_act.NumberOfActions - 1) + ".");
			if (actionIndex + range >= _act.NumberOfActions) throw new Exception("Cannot replace the action (the range value is too large).");
			if (actionIndexTo >= actionIndex && actionIndexTo < actionIndex + range) throw new Exception("Cannot replace the action (indexes are overlapping).");

			_act.Commands.StoreAndExecute(new ActionCommand(actionIndex, range, actionIndexTo, ActionCommand.ActionEdit.Move));
		}

		/// <summary>
		/// Switches an action with another.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="actionIndexTo">The action index destination</param>
		public void ActionSwitch(int actionIndex, int actionIndexTo) {
			if (actionIndex == actionIndexTo) return;
			if (actionIndex < 0 || actionIndex >= _act.NumberOfActions) throw new Exception("Cannot replace the action (the index must be between 0 and " + (_act.NumberOfActions - 1) + ".");
			if (actionIndexTo < 0 || actionIndexTo >= _act.NumberOfActions) throw new Exception("Cannot replace the action (the index must be between 0 and " + (_act.NumberOfActions - 1) + ".");

			_act.Commands.StoreAndExecute(new ActionCommand(actionIndex, actionIndexTo, ActionCommand.ActionEdit.Switch));
		}

		/// <summary>
		/// Switches a range of actions with another.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="range">The range.</param>
		/// <param name="actionIndexTo">The action index destination</param>
		public void ActionSwitchRange(int actionIndex, int range, int actionIndexTo) {
			if (actionIndex == actionIndexTo) return;
			if (actionIndex < 0 || actionIndex >= _act.NumberOfActions) throw new Exception("Cannot replace the action (the index must be between 0 and " + (_act.NumberOfActions - 1) + ".");
			if (actionIndexTo < 0 || actionIndexTo >= _act.NumberOfActions) throw new Exception("Cannot replace the action (the index must be between 0 and " + (_act.NumberOfActions - 1) + ".");
			if (actionIndex + range >= _act.NumberOfActions) throw new Exception("Cannot replace the action (the range value is too large).");
			if (actionIndexTo >= actionIndex && actionIndexTo < actionIndex + range) throw new Exception("Cannot replace the action (invalid indexes).");

			_act.Commands.StoreAndExecute(new ActionCommand(actionIndex, range, actionIndexTo, ActionCommand.ActionEdit.Switch));
		}

		/// <summary>
		/// Deletes an action.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		public void ActionDelete(int actionIndex) {
			if (_act.NumberOfActions <= 1) throw new Exception("Cannot remove the action (there must be at least one action).");
			if (actionIndex < 0 || actionIndex >= _act.NumberOfActions) throw new Exception("Cannot remove the action (the index must be between 0 and " + (_act.NumberOfActions - 1) + ".");

			_act.Commands.StoreAndExecute(new ActionCommand(actionIndex, ActionCommand.ActionEdit.RemoveAt));
		}

		#endregion

		#region Frame

		/// <summary>
		/// Sets the sound id for a frame.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="soundId">The sound id.</param>
		public void SetSoundId(int actionIndex, int frameIndex, int soundId) {
			if (_act[actionIndex, frameIndex].SoundId == soundId)
				return;

			_act.Commands.StoreAndExecute(new SoundIdCommand(actionIndex, frameIndex, soundId));
		}

		/// <summary>
		/// Inserts the a sound to the specified index.
		/// </summary>
		/// <param name="sound">The sound.</param>
		/// <param name="soundId">The sound id.</param>
		public void InsertSoundId(string sound, int soundId) {
			if (soundId < 0 || soundId > _act.SoundFiles.Count)
				return;

			_act.Commands.StoreAndExecute(new SoundIdCommand(sound, soundId, SoundIdCommand.SoundIdEdit.InsertAt));
		}

		/// <summary>
		/// Removes a sound id.
		/// </summary>
		/// <param name="soundId">The sound id.</param>
		public void RemoveSoundId(int soundId) {
			if (soundId < 0 || soundId >= _act.SoundFiles.Count)
				return;

			_act.Commands.StoreAndExecute(new SoundIdCommand(null, soundId, SoundIdCommand.SoundIdEdit.RemoveAt));
		}

		/// <summary>
		/// Sets the anchor position.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="offsetX">The offset X.</param>
		/// <param name="offsetY">The offset Y.</param>
		/// <param name="anchorIndex">The index of the anchor.</param>
		public void SetAnchorPosition(int actionIndex, int frameIndex, int offsetX, int offsetY, int anchorIndex) {
			_act.Commands.StoreAndExecute(new AnchorCommand(actionIndex, frameIndex, offsetX, offsetY, anchorIndex));
		}

		/// <summary>
		/// Inserts a frame at the specified index.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		public void FrameInsertAt(int actionIndex, int frameIndex) {
			if (frameIndex > _act[actionIndex].Frames.Count || frameIndex < 0)
				throw new Exception("Cannot add a frame to the index " + frameIndex + ". The frame index must be between 0 and " + _act[actionIndex].Frames.Count);

			_act.Commands.StoreAndExecute(new FrameCommand(actionIndex, frameIndex, FrameCommand.FrameEdit.InsertTo));
		}

		/// <summary>
		/// Copies and replaces a frame.
		/// </summary>
		/// <param name="actionIndexFrom">The action index source.</param>
		/// <param name="frameIndexFrom">The frame index source.</param>
		/// <param name="frameIndexTo">The frame index destination.</param>
		public void FrameReplace(int actionIndexFrom, int frameIndexFrom, int frameIndexTo) {
			_act.Commands.StoreAndExecute(new FrameCommand(actionIndexFrom, frameIndexFrom, actionIndexFrom, frameIndexTo, FrameCommand.FrameEdit.ReplaceTo));
		}

		/// <summary>
		/// Copies and appends a frame.
		/// </summary>
		/// <param name="actionIndexFrom">The action index source.</param>
		/// <param name="frameIndexFrom">The frame index source.</param>
		public void FrameCopy(int actionIndexFrom, int frameIndexFrom) {
			_act.Commands.StoreAndExecute(new FrameCommand(actionIndexFrom, frameIndexFrom, actionIndexFrom, _act[actionIndexFrom].Frames.Count, FrameCommand.FrameEdit.CopyTo));
		}

		/// <summary>
		/// copies and inserts a frame (to a different action).
		/// </summary>
		/// <param name="actionIndexFrom">The action index source.</param>
		/// <param name="frameIndexFrom">The frame index source.</param>
		/// <param name="actionIndexTo">The action index destination</param>
		/// <param name="frameIndexTo">The frame index destination.</param>
		public void FrameCopyTo(int actionIndexFrom, int frameIndexFrom, int actionIndexTo, int frameIndexTo) {
			_act.Commands.StoreAndExecute(new FrameCommand(actionIndexFrom, frameIndexFrom, actionIndexTo, frameIndexTo, FrameCommand.FrameEdit.CopyTo));
		}

		/// <summary>
		/// copies and inserts a frame.
		/// </summary>
		/// <param name="actionIndexFrom">The action index source.</param>
		/// <param name="frameIndexFrom">The frame index source.</param>
		/// <param name="frameIndexTo">The frame index destination.</param>
		public void FrameCopyTo(int actionIndexFrom, int frameIndexFrom, int frameIndexTo) {
			_act.Commands.StoreAndExecute(new FrameCommand(actionIndexFrom, frameIndexFrom, actionIndexFrom, frameIndexTo, FrameCommand.FrameEdit.CopyTo));
		}

		/// <summary>
		/// Move a range of frames.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="range">The range.</param>
		/// <param name="frameIndexTo">The frame index destination.</param>
		public void FrameMoveRange(int actionIndex, int frameIndex, int range, int frameIndexTo) {
			if (frameIndex == frameIndexTo) return;
			if (frameIndex < 0 || frameIndex >= _act.NumberOfActions) throw new Exception("Cannot replace the action (the index must be between 0 and " + (_act[actionIndex].NumberOfFrames - 1) + ".");
			if (frameIndexTo < 0 || frameIndexTo >= _act.NumberOfActions) throw new Exception("Cannot replace the action (the index must be between 0 and " + (_act[actionIndex].NumberOfFrames - 1) + ".");
			if (frameIndex + range >= _act.NumberOfActions) throw new Exception("Cannot replace the action (the range value is too large).");
			if (frameIndexTo >= frameIndex && frameIndexTo < frameIndex + range) throw new Exception("Cannot replace the action (indexes are overlapping).");

			_act.Commands.StoreAndExecute(new FrameCommand(actionIndex, frameIndex, range, frameIndexTo, FrameCommand.FrameEdit.MoveRange));
		}

		/// <summary>
		/// Switches a frame (from a different action).
		/// </summary>
		/// <param name="actionIndexFrom">The action index source.</param>
		/// <param name="frameIndexFrom">The frame index source.</param>
		/// <param name="actionIndexTo">The action index destination</param>
		/// <param name="frameIndexTo">The frame index destination.</param>
		public void FrameSwitch(int actionIndexFrom, int frameIndexFrom, int actionIndexTo, int frameIndexTo) {
			_act.Commands.StoreAndExecute(new FrameCommand(actionIndexFrom, frameIndexFrom, actionIndexTo, frameIndexTo, FrameCommand.FrameEdit.Switch));
		}

		/// <summary>
		/// Switches a frame.
		/// </summary>
		/// <param name="actionIndexFrom">The action index source.</param>
		/// <param name="frameIndexFrom">The frame index source.</param>
		/// <param name="frameIndexTo">The frame index destination.</param>
		public void FrameSwitch(int actionIndexFrom, int frameIndexFrom, int frameIndexTo) {
			_act.Commands.StoreAndExecute(new FrameCommand(actionIndexFrom, frameIndexFrom, actionIndexFrom, frameIndexTo, FrameCommand.FrameEdit.Switch));
		}

		/// <summary>
		/// Switches a range of frames with another.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="range">The range.</param>
		/// <param name="frameIndexTo">The frame index destination.</param>
		public void FrameSwitchRange(int actionIndex, int frameIndex, int range, int frameIndexTo) {
			if (frameIndex == frameIndexTo) return;
			if (frameIndex < 0 || frameIndex >= _act.NumberOfActions) throw new Exception("Cannot replace the action (the index must be between 0 and " + (_act[actionIndex].NumberOfFrames - 1) + ".");
			if (frameIndexTo < 0 || frameIndexTo >= _act.NumberOfActions) throw new Exception("Cannot replace the action (the index must be between 0 and " + (_act[actionIndex].NumberOfFrames - 1) + ".");
			if (frameIndex + range >= _act.NumberOfActions) throw new Exception("Cannot replace the action (the range value is too large).");
			if (frameIndexTo >= frameIndex && frameIndexTo < frameIndex + range) throw new Exception("Cannot replace the action (invalid indexes).");

			_act.Commands.StoreAndExecute(new FrameCommand(actionIndex, frameIndex, range, frameIndexTo, FrameCommand.FrameEdit.SwitchRange));
		}

		/// <summary>
		/// Deletes a frame.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		public void FrameDelete(int actionIndex, int frameIndex) {
			if (_act[actionIndex].Frames.Count <= 1)
				throw new Exception("Cannot remove the frame (at least one frame is required per action).");

			_act.Commands.StoreAndExecute(new FrameCommand(actionIndex, frameIndex));
		}

		/// <summary>
		/// Deletes a range of frames.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="count">The count.</param>
		public void FrameDeleteRange(int actionIndex, int frameIndex, int count) {
			if (count == 0) return;
			_act.Commands.StoreAndExecute(new FrameCommand(actionIndex, frameIndex, count));
		}

		#endregion

		#region Layers

		/// <summary>
		/// Sets the offsets of a layer.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layerIndex">Index of the layer.</param>
		/// <param name="offsetX">The offset X.</param>
		/// <param name="offsetY">The offset Y.</param>
		public void SetOffsets(int actionIndex, int frameIndex, int layerIndex, int offsetX, int offsetY) {
			if (offsetX - _act[actionIndex, frameIndex, layerIndex].OffsetX == 0 &&
			    offsetY - _act[actionIndex, frameIndex, layerIndex].OffsetY == 0)
				return;

			_act.Commands.StoreAndExecute(new TranslateCommand(actionIndex, frameIndex, layerIndex, offsetX - _act[actionIndex, frameIndex, layerIndex].OffsetX, offsetY - _act[actionIndex, frameIndex, layerIndex].OffsetY));
		}

		/// <summary>
		/// Sets the offset X of a layer.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layerIndex">Index of the layer.</param>
		/// <param name="offset">The offset.</param>
		public void SetOffsetX(int actionIndex, int frameIndex, int layerIndex, int offset) {
			if (offset - _act[actionIndex, frameIndex, layerIndex].OffsetX == 0)
				return;

			_act.Commands.StoreAndExecute(new TranslateCommand(actionIndex, frameIndex, layerIndex, offset - _act[actionIndex, frameIndex, layerIndex].OffsetX, 0));
		}

		/// <summary>
		/// Sets the offset Y of a layer.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layerIndex">Index of the layer.</param>
		/// <param name="offset">The offset.</param>
		public void SetOffsetY(int actionIndex, int frameIndex, int layerIndex, int offset) {
			if (offset - _act[actionIndex, frameIndex, layerIndex].OffsetY == 0)
				return;

			_act.Commands.StoreAndExecute(new TranslateCommand(actionIndex, frameIndex, layerIndex, 0, offset - _act[actionIndex, frameIndex, layerIndex].OffsetY));
		}

		/// <summary>
		/// Sets the mirror property of all the layers.
		/// </summary>
		/// <param name="mirror">if set to <c>true</c> [mirror].</param>
		public void SetMirror(bool mirror) {
			_act.Commands.StoreAndExecute(new MirrorCommand(mirror));
		}

		/// <summary>
		/// Sets the mirror property for a layer.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layerIndex">Index of the layer.</param>
		/// <param name="mirror">if set to <c>true</c> [mirror].</param>
		public void SetMirror(int actionIndex, int frameIndex, int layerIndex, bool mirror) {
			if (_act[actionIndex, frameIndex, layerIndex].Mirror == mirror)
				return;

			_act.Commands.StoreAndExecute(new MirrorCommand(actionIndex, frameIndex, layerIndex));
		}

		/// <summary>
		/// Sets the relative sprite id.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layerIndex">Index of the layer.</param>
		/// <param name="spriteId">The sprite id.</param>
		public void SetSpriteId(int actionIndex, int frameIndex, int layerIndex, int spriteId) {
			if (_act[actionIndex, frameIndex, layerIndex].SpriteIndex == spriteId)
				return;

			_act.Commands.StoreAndExecute(new SpriteIdCommand(actionIndex, frameIndex, layerIndex, spriteId));
		}

		/// <summary>
		/// Sets the sprite id of a layer based on the image absoltue index.
		/// This command may use a create a subgroup command.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layerIndex">Index of the layer.</param>
		/// <param name="absoluteId">The absolute id.</param>
		public void SetAbsoluteSpriteId(int actionIndex, int frameIndex, int layerIndex, int absoluteId) {
			var layer = _act[actionIndex, frameIndex, layerIndex];

			if (layer.GetAbsoluteSpriteId(_act.Sprite) == absoluteId)
				return;

			if (layer.IsIndexed8() && absoluteId < _act.Sprite.NumberOfIndexed8Images) {
				_act.Commands.StoreAndExecute(new SpriteIdCommand(actionIndex, frameIndex, layerIndex, absoluteId));
			}
			else if (layer.IsBgra32() && absoluteId >= _act.Sprite.NumberOfIndexed8Images) {
				_act.Commands.StoreAndExecute(new SpriteIdCommand(actionIndex, frameIndex, layerIndex, absoluteId - _act.Sprite.NumberOfIndexed8Images));
			}
			else {
				ActGroupCommand commands = new ActGroupCommand(_act);
				commands.Add(new SpriteTypeCommand(actionIndex, frameIndex, layerIndex, layer.IsIndexed8() ? 1 : 0));
				commands.Add(new SpriteIdCommand(actionIndex, frameIndex, layerIndex, layer.IsIndexed8() ? absoluteId - _act.Sprite.NumberOfIndexed8Images : absoluteId));
				_act.Commands.StoreAndExecute(commands);
			}
		}

		/// <summary>
		/// Sets the type of the sprite.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layerIndex">Index of the layer.</param>
		/// <param name="spriteType">Type of the sprite.</param>
		public void SetSpriteType(int actionIndex, int frameIndex, int layerIndex, int spriteType) {
			if (_act[actionIndex, frameIndex, layerIndex].SpriteTypeInt == spriteType)
				return;

			if (spriteType == 1 && _act[actionIndex, frameIndex, layerIndex].SpriteIndex >= _act.Sprite.NumberOfBgra32Images) {
				_act.InvalidateVisual();
				throw new Exception("Cannot set the sprite type to Bgra32 : the index of the sprite needs to be below " + _act.Sprite.NumberOfBgra32Images + ".");
			}

			if (spriteType == 0 && _act[actionIndex, frameIndex, layerIndex].SpriteIndex >= _act.Sprite.NumberOfIndexed8Images) {
				_act.InvalidateVisual();
				throw new Exception("Cannot set the sprite type to Indexed8 : the index of the sprite needs to be below " + _act.Sprite.NumberOfIndexed8Images + ".");
			}

			_act.Commands.StoreAndExecute(new SpriteTypeCommand(actionIndex, frameIndex, layerIndex, spriteType));
		}

		/// <summary>
		/// Sets the color of all layers.
		/// </summary>
		/// <param name="color">The color.</param>
		public void SetColor(GrfColor color) {
			if (_act.GetAllLayers().Count == 0)
				return;

			_act.Commands.StoreAndExecute(new ColorCommand(color));
		}

		/// <summary>
		/// Sets the color of a layer.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layerIndex">Index of the layer.</param>
		/// <param name="color">The color.</param>
		public void SetColor(int actionIndex, int frameIndex, int layerIndex, GrfColor color) {
			if (Equals(_act[actionIndex, frameIndex, layerIndex].Color, color))
				return;

			_act.Commands.StoreAndExecute(new ColorCommand(actionIndex, frameIndex, layerIndex, color));
		}

		/// <summary>
		/// Appends a layer to a frame.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layerIndexTo">The layer index to.</param>
		/// <param name="absoluteSpriteIndex">Absolute index of the sprite.</param>
		public void LayerAdd(int actionIndex, int frameIndex, int layerIndexTo, int absoluteSpriteIndex) {
			if (absoluteSpriteIndex < 0) return;
			if (layerIndexTo < 0) return;

			GrfImage image = _act.Sprite.Images[absoluteSpriteIndex];
			Layer layer = new Layer(image.GrfImageType == GrfImageType.Indexed8 ? absoluteSpriteIndex : absoluteSpriteIndex - _act.Sprite.NumberOfIndexed8Images, image);
			_act.Commands.StoreAndExecute(new FrameCommand(actionIndex, frameIndex, layerIndexTo, layer, FrameCommand.FrameEdit.AddTo));
		}

		/// <summary>
		/// Appends a layer to a frame.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="absoluteSpriteIndex">Absolute index of the sprite.</param>
		public void LayerAdd(int actionIndex, int frameIndex, int absoluteSpriteIndex) {
			if (absoluteSpriteIndex < 0) return;

			GrfImage image = _act.Sprite.Images[absoluteSpriteIndex];
			Layer layer = new Layer(image.GrfImageType == GrfImageType.Indexed8 ? absoluteSpriteIndex : absoluteSpriteIndex - _act.Sprite.NumberOfIndexed8Images, image);
			_act.Commands.StoreAndExecute(new FrameCommand(actionIndex, frameIndex, _act[actionIndex, frameIndex].Layers.Count, layer, FrameCommand.FrameEdit.AddTo));
		}

		/// <summary>
		/// Appends layers to a frame.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layers">The layers.</param>
		public void LayerAdd(int actionIndex, int frameIndex, Layer[] layers) {
			if (layers.Length == 0) return;

			_act.Commands.StoreAndExecute(new FrameCommand(actionIndex, frameIndex, _act[actionIndex, frameIndex].Layers.Count, layers, FrameCommand.FrameEdit.AddRange));
		}

		/// <summary>
		/// Inserts layers to a frame at the specified index.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layers">The layers.</param>
		/// <param name="addToIndex">The layer index destination.</param>
		public void LayerAdd(int actionIndex, int frameIndex, Layer[] layers, int addToIndex) {
			if (layers.Length == 0) return;

			_act.Commands.StoreAndExecute(new FrameCommand(actionIndex, frameIndex, addToIndex, layers, FrameCommand.FrameEdit.AddRange));
		}

		/// <summary>
		/// Switches a range of layers.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layerIndexFrom">The layer index source.</param>
		/// <param name="layerIndexTo">The layer index destination.</param>
		/// <returns></returns>
		public bool LayerSwitch(int actionIndex, int frameIndex, int layerIndexFrom, int layerIndexTo) {
			if (layerIndexFrom == layerIndexTo) return false;
			if (layerIndexFrom == _act[actionIndex, frameIndex].Layers.Count - 1 && layerIndexFrom == layerIndexTo - 1) return false;

			_act.Commands.StoreAndExecute(new FrameCommand(actionIndex, frameIndex, layerIndexFrom, 1, layerIndexTo, FrameCommand.FrameEdit.MoveTo));
			return true;
		}

		/// <summary>
		/// Switches a range of layers.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layerIndexFrom">The layer index source.</param>
		/// <param name="count">The count.</param>
		/// <param name="layerIndexTo">The layer index destination.</param>
		/// <returns></returns>
		public bool LayerSwitchRange(int actionIndex, int frameIndex, int layerIndexFrom, int count, int layerIndexTo) {
			if (count <= 0) return false;
			if (count == 1) {
				return LayerSwitch(actionIndex, frameIndex, layerIndexFrom, layerIndexTo);
			}
			if (layerIndexFrom <= layerIndexTo && layerIndexTo < layerIndexFrom + count) return false;
			if (layerIndexFrom == _act[actionIndex, frameIndex].Layers.Count - 1 && layerIndexFrom == layerIndexTo - 1) return false;

			_act.Commands.StoreAndExecute(new FrameCommand(actionIndex, frameIndex, layerIndexFrom, count, layerIndexTo, FrameCommand.FrameEdit.MoveTo));
			return true;
		}

		/// <summary>
		/// Mirrors the layers with at a specific offset (0 by default).
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layerIndexFrom">The layer index source.</param>
		/// <param name="offset">The mirror offset.</param>
		/// <param name="direction">The flip direction.</param>
		/// <returns></returns>
		public bool MirrorFromOffset(int actionIndex, int frameIndex, int layerIndexFrom, int offset, FlipDirection direction) {
			_act.Commands.StoreAndExecute(new FlipCommand(actionIndex, frameIndex, layerIndexFrom, offset, direction));
			return true;
		}

		/// <summary>
		/// Mirrors the layers with at a specific offset (0 by default).
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="offset">The mirror offset.</param>
		/// <param name="direction">The flip direction.</param>
		/// <returns></returns>
		public bool MirrorFromOffset(int actionIndex, int frameIndex, int offset, FlipDirection direction) {
			_act.Commands.StoreAndExecute(new FlipCommand(actionIndex, frameIndex, offset, direction));
			return true;
		}

		/// <summary>
		/// Mirrors the layers with at a specific offset (0 by default).
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="offset">The mirror offset.</param>
		/// <param name="direction">The flip direction.</param>
		/// <returns></returns>
		public bool MirrorFromOffset(int actionIndex, int offset, FlipDirection direction) {
			_act.Commands.StoreAndExecute(new FlipCommand(actionIndex, offset, direction));
			return true;
		}

		/// <summary>
		/// Mirrors the layers with at a specific offset (0 by default).
		/// </summary>
		/// <param name="offset">The mirror offset.</param>
		/// <param name="direction">The flip direction.</param>
		/// <returns></returns>
		public bool MirrorFromOffset(int offset, FlipDirection direction) {
			_act.Commands.StoreAndExecute(new FlipCommand(offset, direction));
			return true;
		}

		/// <summary>
		/// Deletes a layer.
		/// </summary>
		/// <param name="actionIndex">Index of the action.</param>
		/// <param name="frameIndex">Index of the frame.</param>
		/// <param name="layerIndex">Index of the layer.</param>
		public void LayerDelete(int actionIndex, int frameIndex, int layerIndex) {
			int count = _act[actionIndex, frameIndex].Layers.Count;

			if (layerIndex < 0 || layerIndex >= count) return;

			_act.Commands.StoreAndExecute(new LayerCommand(actionIndex, frameIndex, layerIndex, 1, LayerCommand.LayerEdit.RemoveRange));
		}

		#endregion

		#region Sprite

		/// <summary>
		/// Sets the sprite of the Act (this command should be avoided).
		/// </summary>
		/// <param name="sprite">The sprite.</param>
		public void SetSprite(Spr sprite) {
			_act.Commands.StoreAndExecute(new SpriteCommand(sprite));
		}

		/// <summary>
		/// Append a sprite image.
		/// </summary>
		/// <param name="image">The image.</param>
		public void SpriteAdd(GrfImage image) {
			_act.Commands.StoreAndExecute(new SpriteCommand(image.GrfImageType == GrfImageType.Indexed8 ? _act.Sprite.NumberOfIndexed8Images : _act.Sprite.NumberOfImagesLoaded - _act.Sprite.NumberOfIndexed8Images, image, SpriteCommand.SpriteEdit.InsertAt));
		}

		/// <summary>
		/// Insert a sprite at a specific position.
		/// </summary>
		/// <param name="position">The position.</param>
		/// <param name="image">The image.</param>
		public void SpriteInsertAt(int position, GrfImage image) {
			_act.Commands.StoreAndExecute(new SpriteCommand(position, image, SpriteCommand.SpriteEdit.InsertAt));
		}

		/// <summary>
		/// Overwrites a sprite at the absolute index.
		/// </summary>
		/// <param name="absoluteIndex">The absolute index.</param>
		/// <param name="image">The image.</param>
		public void SpriteReplaceAt(int absoluteIndex, GrfImage image) {
			_act.Commands.StoreAndExecute(new SpriteCommand(absoluteIndex, image, SpriteCommand.SpriteEdit.ReplaceAt));
		}

		/// <summary>
		/// Converts a sprite at the absolute index to the other type. This command also handles layer indexes.
		/// </summary>
		/// <param name="absoluteIndex">The absolute index.</param>
		/// <param name="type">The image type.</param>
		public void SpriteConvertAt(int absoluteIndex, GrfImageType type) {
			if (_act.Sprite.Images[absoluteIndex].GrfImageType == type)
				return;

			GrfImage image = _act.Sprite.Images[absoluteIndex];
			GrfImage imageConverted = _act.Sprite.Images[absoluteIndex].Copy();

			if (type == GrfImageType.Bgra32) {
				imageConverted.Palette[3] = 0;
				imageConverted.Convert(new Bgra32FormatConverter());
			}
			else {
				imageConverted.Convert(new Indexed8FormatConverter { ExistingPalette = _act.Sprite.Palette.BytePalette, Options = Indexed8FormatConverter.PaletteOptions.UseExistingPalette });
			}

			int relativeNewIndex = imageConverted.GrfImageType == GrfImageType.Indexed8 ? _act.Sprite.NumberOfIndexed8Images : _act.Sprite.NumberOfBgra32Images;
			Spr spr = _act.Sprite;

			try {
				Begin();

				Backup(act => {
					SpriteTypes spriteType = imageConverted.GrfImageType == GrfImageType.Indexed8 ? SpriteTypes.Indexed8 : SpriteTypes.Bgra32;

					foreach (Layer layer in act.GetAllLayers().Where(layer => layer.GetAbsoluteSpriteId(spr) == absoluteIndex)) {
						layer.SpriteType = spriteType;
						layer.SpriteIndex = relativeNewIndex;
					}

					_act.Sprite.Remove(absoluteIndex, act, EditOption.KeepCurrentIndexes);
					_act.Sprite.InsertAny(imageConverted);
					_act.Sprite.ShiftIndexesAbove(act, image.GrfImageType, -1,
						_act.Sprite.AbsoluteToRelative(absoluteIndex, image.GrfImageType == GrfImageType.Indexed8 ? 0 : 1));
				}, "Sprite convert", true);
			}
			catch {
				CancelEdit();
				throw;
			}
			finally {
				End();
			}
		}

		/// <summary>
		/// Set the palette of the Sprite file.
		/// </summary>
		/// <param name="palette">The palette.</param>
		public void SpriteSetPalette(byte[] palette) {
			_act.Commands.StoreAndExecute(new ChangePalette(palette));
		}

		/// <summary>
		/// Flips a sprite.
		/// </summary>
		/// <param name="relativeIndex">The relative index.</param>
		/// <param name="imageType">Type of the image.</param>
		/// <param name="direction">The direction.</param>
		public void SpriteFlip(int relativeIndex, int imageType, FlipDirection direction) {
			_act.Commands.StoreAndExecute(new Flip(imageType == 0 ? relativeIndex : relativeIndex - _act.Sprite.NumberOfIndexed8Images, direction));
		}

		/// <summary>
		/// Flips a sprite.
		/// </summary>
		/// <param name="absoluteIndex">The absolute index.</param>
		/// <param name="direction">The direction.</param>
		public void SpriteFlip(int absoluteIndex, FlipDirection direction) {
			_act.Commands.StoreAndExecute(new Flip(absoluteIndex, direction));
		}

		/// <summary>
		/// Inserts a sprite.
		/// </summary>
		/// <param name="relativeIndex">The relative index.</param>
		/// <param name="imageType">Type of the image.</param>
		/// <param name="image">The image.</param>
		public void SpriteInsert(int relativeIndex, int imageType, GrfImage image) {
			_act.Commands.StoreAndExecute(new Insert(imageType == 0 ? relativeIndex : relativeIndex - _act.Sprite.NumberOfIndexed8Images, image));
		}

		/// <summary>
		/// Inserts a sprite.
		/// </summary>
		/// <param name="absoluteIndex">The absolute index.</param>
		/// <param name="image">The image.</param>
		public void SpriteInsert(int absoluteIndex, GrfImage image) {
			_act.Commands.StoreAndExecute(new Insert(absoluteIndex, image));
		}

		/// <summary>
		/// Removes a sprite.
		/// </summary>
		/// <param name="relativeIndex">The relative index.</param>
		/// <param name="imageType">Type of the image.</param>
		public void SpriteRemove(int relativeIndex, int imageType) {
			_act.Commands.StoreAndExecute(new RemoveCommand(imageType == 0 ? relativeIndex : relativeIndex - _act.Sprite.NumberOfIndexed8Images));
		}

		/// <summary>
		/// Removes a sprite.
		/// </summary>
		/// <param name="relativeIndex">The relative index.</param>
		/// <param name="imageType">Type of the image.</param>
		public void SpriteRemove(int relativeIndex, GrfImageType imageType) {
			SpriteRemove(relativeIndex, imageType == GrfImageType.Indexed8 ? 0 : 1);
		}

		/// <summary>
		/// Removes a sprite.
		/// </summary>
		/// <param name="absoluteIndex">The absolute index.</param>
		public void SpriteRemove(int absoluteIndex) {
			_act.Commands.StoreAndExecute(new RemoveCommand(absoluteIndex));
		}

		/// <summary>
		/// Remove a ranged amount of sprites.
		/// </summary>
		/// <param name="position">The sprite position.</param>
		/// <param name="count">The count.</param>
		public void SpriteRemoveRange(int position, int count) {
			_act.Commands.StoreAndExecute(new SpriteCommand(position, count, SpriteCommand.SpriteEdit.RemoveRange));
		}

		#endregion
	}
}