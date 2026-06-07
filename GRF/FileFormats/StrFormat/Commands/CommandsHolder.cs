using System;
using System.Collections.Generic;
using System.Linq;
using GRF.Graphics;
using GRF.Image;
using Utilities;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class CommandsHolder : AbstractCommand<IStrCommand> {
		public const float ChangeEpsilon = 0.0001f;
		private readonly Str _str;

		public CommandsHolder(Str str) {
			_str = str;
		}

		protected override void _execute(IStrCommand command) {
			command.Execute(_str);
		}

		protected override void _undo(IStrCommand command) {
			command.Undo(_str);
		}

		protected override void _redo(IStrCommand command) {
			command.Execute(_str);
		}

		public void SetEditorPosition(int frameIndex, int frameCount, int layerIndex, int layerCount) {
			_str.Commands.StoreAndExecute(new SetEditorPositionCommand(frameIndex, frameCount, layerIndex, layerCount));
		}

		public void SetColor(int layerIndex, int keyIndex, float a, float r, float g, float b) {
			_str.Commands.StoreAndExecute(new SetColorCommand(layerIndex, keyIndex, r, g, b, a));
		}

		public void SetColor(int layerIndex, int keyIndex, in GrfColor color) {
			_str.Commands.StoreAndExecute(new SetColorCommand(layerIndex, keyIndex, color.R, color.G, color.B, color.A));
		}

		public void SetScaleBias(int layerIndex, int keyIndex, float bias) {
			var keyFrame = _str[layerIndex, keyIndex];

			if (Math.Abs(bias - keyFrame.ScaleBias) < ChangeEpsilon)
				return;

			_str.Commands.StoreAndExecute(new SetPropertyCommand<float>(layerIndex, keyIndex, keyFrame.ScaleBias, bias, v => keyFrame.ScaleBias = v, "ScaleBias"));
		}

		public void SetAngleBias(int layerIndex, int keyIndex, float bias) {
			var keyFrame = _str[layerIndex, keyIndex];

			if (Math.Abs(bias - keyFrame.AngleBias) < ChangeEpsilon)
				return;

			_str.Commands.StoreAndExecute(new SetPropertyCommand<float>(layerIndex, keyIndex, keyFrame.AngleBias, bias, v => keyFrame.AngleBias = v, "AngleBias"));
		}

		public void SetOffsetBias(int layerIndex, int keyIndex, float bias) {
			var keyFrame = _str[layerIndex, keyIndex];

			if (Math.Abs(bias - keyFrame.OffsetBias) < ChangeEpsilon)
				return;

			_str.Commands.StoreAndExecute(new SetPropertyCommand<float>(layerIndex, keyIndex, keyFrame.OffsetBias, bias, v => keyFrame.OffsetBias = v, "OffsetBias"));
		}

		public void SetAngle(int layerIndex, int keyIndex, float angle) {
			var keyFrame = _str[layerIndex, keyIndex];

			if (Math.Abs(angle - keyFrame.Angle) < ChangeEpsilon)
				return;

			_str.Commands.StoreAndExecute(new SetPropertyCommand<float>(layerIndex, keyIndex, keyFrame.Angle, angle, v => keyFrame.Angle = v, "Angle"));
		}

		public void SetBlendSrc(int layerIndex, int keyIndex, int blend) {
			var keyFrame = _str[layerIndex, keyIndex];

			if (Math.Abs(blend - keyFrame.BlendSrc) < ChangeEpsilon)
				return;

			_str.Commands.StoreAndExecute(new SetPropertyCommand<int>(layerIndex, keyIndex, keyFrame.BlendSrc, blend, v => keyFrame.BlendSrc = v, "BlendSrc"));
		}

		public void SetBlendDst(int layerIndex, int keyIndex, int blend) {
			var keyFrame = _str[layerIndex, keyIndex];

			if (Math.Abs(blend - keyFrame.BlendDst) < ChangeEpsilon)
				return;

			_str.Commands.StoreAndExecute(new SetPropertyCommand<int>(layerIndex, keyIndex, keyFrame.BlendDst, blend, v => keyFrame.BlendDst = v, "BlendDst"));
		}

		public void SetOffset(int layerIndex, int keyIndex, float x, float y) {
			if (Math.Abs(x - _str[layerIndex, keyIndex].Offset.X) < ChangeEpsilon &&
				Math.Abs(y - _str[layerIndex, keyIndex].Offset.Y) < ChangeEpsilon)
				return;
			_str.Commands.StoreAndExecute(new SetOffsetCommand(layerIndex, keyIndex, x, y));
		}

		public void AddKey(int layerIndex, int keyIndex, StrKeyFrame frame) {
			_str.Commands.StoreAndExecute(new AddKeyCommand(layerIndex, keyIndex, frame));
		}

		public void SetKey(int layerIndex, StrKeyFrame frame) {
			_str.Commands.StoreAndExecute(new SetKeyCommand(layerIndex, frame.FrameIndex, frame));
		}

		public void SetKey(int layerIndex, int frameIndex, StrKeyFrame frame) {
			_str.Commands.StoreAndExecute(new SetKeyCommand(layerIndex, frameIndex, frame));
		}

		public void DeleteKey(int layerIndex, int keyIndex) {
			_str.Commands.StoreAndExecute(new DeleteKeyCommand(layerIndex, keyIndex));
		}

		#region Scale key frame
		public void ScalePivot(int layerIndex, int keyIndex, float x, float y) {
			ScalePivot(layerIndex, keyIndex, x, y, new TkVector2(Str.OffsetX, Str.OffsetY));
		}

		public void ScalePivot(int layerIndex, int keyIndex, float x, float y, TkVector2 pivot) {
			Scale(layerIndex, keyIndex, x, y, pivot, ScaleFromPivotCommand.PivotMode.Defined);
		}

		public void ScaleOrigin(int layerIndex, int keyIndex, float x, float y) {
			Scale(layerIndex, keyIndex, x, y, default, ScaleFromPivotCommand.PivotMode.Origin);
		}

		public void ScaleCenter(int layerIndex, int keyIndex, float x, float y) {
			Scale(layerIndex, keyIndex, x, y, default, ScaleFromPivotCommand.PivotMode.Center);
		}

		public void Scale(int layerIndex, int keyIndex, float x, float y, TkVector2 pivot, ScaleFromPivotCommand.PivotMode pivotMode) {
			if (Math.Abs(x - 1f) < ChangeEpsilon &&
				Math.Abs(y - 1f) < ChangeEpsilon)
				return;
			_str.Commands.StoreAndExecute(new ScaleFromPivotCommand(layerIndex, keyIndex, x, y, pivot, pivotMode));
		}
		#endregion

		#region Scale layer
		public void ScalePivot(int layerIndex, float x, float y) {
			ScalePivot(layerIndex, x, y, new TkVector2(Str.OffsetX, Str.OffsetY));
		}

		public void ScalePivot(int layerIndex, float x, float y, TkVector2 pivot) {
			Scale(layerIndex, x, y, pivot, ScaleFromPivotCommand.PivotMode.Defined);
		}

		public void ScaleOrigin(int layerIndex, float x, float y) {
			Scale(layerIndex, x, y, default, ScaleFromPivotCommand.PivotMode.Origin);
		}

		public void ScaleCenter(int layerIndex, float x, float y) {
			Scale(layerIndex, x, y, default, ScaleFromPivotCommand.PivotMode.Center);
		}

		public void Scale(int layerIndex, float x, float y, TkVector2 pivot, ScaleFromPivotCommand.PivotMode pivotMode) {
			if (Math.Abs(x - 1f) < ChangeEpsilon &&
				Math.Abs(y - 1f) < ChangeEpsilon)
				return;
			_str.Commands.StoreAndExecute(new ScaleFromPivotCommand(layerIndex, x, y, pivot, pivotMode));
		}
		#endregion

		#region Scale STR
		public void ScalePivot(float x, float y) {
			ScalePivot(x, y, new TkVector2(Str.OffsetX, Str.OffsetY));
		}

		public void ScalePivot(float x, float y, TkVector2 pivot) {
			Scale(x, y, pivot, ScaleFromPivotCommand.PivotMode.Defined);
		}

		public void ScaleOrigin(float x, float y) {
			Scale(x, y, default, ScaleFromPivotCommand.PivotMode.Origin);
		}

		public void ScaleCenter(float x, float y) {
			Scale(x, y, default, ScaleFromPivotCommand.PivotMode.Center);
		}

		public void Scale(float x, float y, TkVector2 pivot, ScaleFromPivotCommand.PivotMode pivotMode) {
			if (Math.Abs(x - 1f) < ChangeEpsilon &&
				Math.Abs(y - 1f) < ChangeEpsilon)
				return;
			_str.Commands.StoreAndExecute(new ScaleFromPivotCommand(x, y, pivot, pivotMode));
		}
		#endregion

		public void SetPositions(int layerIndex, int keyIndex, float[] positions) {
			if (Enumerable.SequenceEqual(positions, _str[layerIndex, keyIndex].Positions))
				return;
			_str.Commands.StoreAndExecute(new SetPositionsCommand(layerIndex, keyIndex, positions));
		}

		public void SetBezierPositions(int layerIndex, int keyIndex, float[] bezierPoints) {
			if (Enumerable.SequenceEqual(bezierPoints, _str[layerIndex, keyIndex].BezierPositions))
				return;
			_str.Commands.StoreAndExecute(new SetBezierCommand(layerIndex, keyIndex, bezierPoints));
		}

		public void SetPosition(int layerIndex, int keyIndex, int point, float offset) {
			var keyFrame = _str[layerIndex, keyIndex];

			if (Math.Abs(offset - keyFrame.Positions[point]) < ChangeEpsilon)
				return;

			_str.Commands.StoreAndExecute(new SetPropertyCommand<float>(layerIndex, keyIndex, keyFrame.Positions[point], offset, v => keyFrame.Positions[point] = v, "Position[" + point + "]"));
		}

		public void SetUVs(int layerIndex, int keyIndex, float[] uvs) {
			if (Enumerable.SequenceEqual(uvs, _str[layerIndex, keyIndex].UVs))
				return;
			_str.Commands.StoreAndExecute(new SetUVsCommand(layerIndex, keyIndex, uvs));
		}

		public void SetTextureIndex(int layerIndex, int keyIndex, int textureIndex) {
			var keyFrame = _str[layerIndex, keyIndex];

			if (Math.Abs(textureIndex - keyFrame.TextureIndex) < ChangeEpsilon)
				return;

			_str.Commands.StoreAndExecute(new SetPropertyCommand<float>(layerIndex, keyIndex, keyFrame.TextureIndex, textureIndex, v => keyFrame.TextureIndex = v, "TextureIndex"));
		}

		public void SetTextures(int layerIndex, List<string> textures) {
			_str.Commands.StoreAndExecute(new TexturesCommand(layerIndex, textures));
		}

		public void DeleteLayer(int layerIndex) {
			_str.Commands.StoreAndExecute(new DeleteLayerCommand(layerIndex));
		}

		public void InsertLayer(int layerIndex) {
			_str.Commands.StoreAndExecute(new InsertLayerCommand(layerIndex));
		}

		public void InsertLayer(int layerIndex, StrLayer layer) {
			_str.Commands.StoreAndExecute(new InsertLayerCommand(layerIndex, layer));
		}

		public void MoveLayer(int layerIndexSource, int layerIndexDest) {
			if (layerIndexSource == layerIndexDest || layerIndexSource == layerIndexDest - 1)
				return;

			_str.Commands.StoreAndExecute(new MoveLayerCommand(layerIndexSource, layerIndexDest));
		}

		public void SetAnimationType(int layerIndex, int keyIndex, AnimationType animationType) {
			var keyFrame = _str[layerIndex, keyIndex];

			if (Math.Abs(animationType - keyFrame.AnimationType) < ChangeEpsilon)
				return;

			_str.Commands.StoreAndExecute(new SetPropertyCommand<AnimationType>(layerIndex, keyIndex, keyFrame.AnimationType, animationType, v => keyFrame.AnimationType = v, "AnimType"));
		}

		public void SetFps(int fps) {
			if (_str.Fps == fps)
				return;

			_str.Commands.StoreAndExecute(new SetPropertyCommand<int>(_str.Fps, fps, v => _str.Fps = v, "FPS"));
		}

		public void SetDelay(int layerIndex, int keyIndex, float delay) {
			var keyFrame = _str[layerIndex, keyIndex];

			if (Math.Abs(delay - keyFrame.Delay) < ChangeEpsilon)
				return;

			_str.Commands.StoreAndExecute(new SetPropertyCommand<float>(layerIndex, keyIndex, keyFrame.Delay, delay, v => keyFrame.Delay = v, "Delay"));
		}

		public void SetMaxFrame(int maxFrame) {
			if (maxFrame == _str.MaxKeyFrame)
				return;
			_str.Commands.StoreAndExecute(new SetMaxFrameCommand(maxFrame));
		}

		public void SetInterpolated(int layerIndex, int keyIndex, bool isInterpolated) {
			var keyFrame = _str[layerIndex, keyIndex];

			if (keyIndex < 0 || keyFrame.IsInterpolated == isInterpolated)
				return;

			_str.Commands.StoreAndExecute(new SetPropertyCommand<bool>(layerIndex, keyIndex, keyFrame.IsInterpolated, isInterpolated, v => keyFrame.IsInterpolated = v, "Interpolation"));
		}

		public void FlipH(int layerIndex, int keyIndex) {
			_str.Commands.StoreAndExecute(new FlipHCommand(layerIndex, keyIndex, new TkVector2(Str.OffsetX, Str.OffsetY)));
		}

		public void FlipH(int layerIndex, int keyIndex, TkVector2 origin) {
			_str.Commands.StoreAndExecute(new FlipHCommand(layerIndex, keyIndex, origin));
		}

		public void FlipV(int layerIndex, int keyIndex) {
			_str.Commands.StoreAndExecute(new FlipVCommand(layerIndex, keyIndex, new TkVector2(Str.OffsetX, Str.OffsetY)));
		}

		public void FlipV(int layerIndex, int keyIndex, TkVector2 origin) {
			_str.Commands.StoreAndExecute(new FlipVCommand(layerIndex, keyIndex, origin));
		}

		public void FlipHSelf(int layerIndex, int keyIndex) {
			_str.Commands.StoreAndExecute(new FlipHSelfCommand(layerIndex, keyIndex));
		}

		public void FlipVSelf(int layerIndex, int keyIndex) {
			_str.Commands.StoreAndExecute(new FlipVSelfCommand(layerIndex, keyIndex));
		}

		public void FlipTextureH(int layerIndex, int keyIndex) {
			_str.Commands.StoreAndExecute(new FlipHTextureCommand(layerIndex, keyIndex));
		}

		public void FlipTextureV(int layerIndex, int keyIndex) {
			_str.Commands.StoreAndExecute(new FlipVTextureCommand(layerIndex, keyIndex));
		}

		public void CreateNew(int layerIndex, int frameIndex, bool interpolate = true) {
			_str.Commands.StoreAndExecute(new CreateNewKeyCommand(layerIndex, frameIndex, interpolate));
		}

		public void CreateEndKey(int layerIndex, int frameIndex) {
			_str.Commands.StoreAndExecute(new CreateEndKeyCommand(layerIndex, frameIndex));
		}

		public List<int> DeleteKeys(int layerIndex, int frameIndexStart, int frameCount) {
			var keyIndexes = _str[layerIndex].GetKeyIndexesInRange(frameIndexStart, frameCount, StrLayer.RangeSearchMode.Contained);

			if (keyIndexes.Count == 0)
				return keyIndexes;

			_str.Commands.StoreAndExecute(new DeleteKeysCommand(layerIndex, frameIndexStart, frameIndexStart + frameCount));
			return keyIndexes;
		}

		public void CenterOrigin(int layerIndex) {
			CenterOrigin(layerIndex, 0, true);
		}

		public void CenterOrigin(int layerIndex, int keyIndex, bool groupEdit = false) {
			var layer = _str[layerIndex];

			if (layer.KeyFrames.Count == 0)
				return;

			if (groupEdit) {
				_str.Commands.StoreAndExecute(new CenterOriginCommand(layerIndex));
			}
			else {
				if (keyIndex == -1)
					return;

				_str.Commands.StoreAndExecute(new CenterOriginCommand(layerIndex, keyIndex));
			}
		}

		public void Backup(Action<Str> action) {
			_str.Commands.StoreAndExecute(new BackupCommand(action));
		}

		public void Backup(Action<Str> action, string commandName) {
			_str.Commands.StoreAndExecute(new BackupCommand(action, commandName));
		}

		public void Backup(Action<Str> action, string commandName, bool forceReload) {
			_str.Commands.StoreAndExecute(new BackupCommand(action, commandName, forceReload));
		}

		/// <summary>
		/// Begins the commands stack grouping.
		/// </summary>
		public void Begin() {
			if (IsDelayed)
				return;

			_str.Commands.BeginEdit(new StrGroupCommand(_str, false));
		}

		/// <summary>
		/// Begins the commands stack grouping and apply commands as soon as they're received.
		/// </summary>
		public void BeginNoDelay() {
			_str.Commands.BeginEdit(new StrGroupCommand(_str, true));
		}

		/// <summary>
		/// Ends the commands stack grouping.
		/// </summary>
		public void End() {
			_str.Commands.EndEdit();
		}
	}
}
