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
			_str.Commands.StoreAndExecute(new PositionCommand(frameIndex, frameCount, layerIndex, layerCount));
		}

		public void ChangeColor(int layerIndex, int keyIndex, float a, float r, float g, float b) {
			_str.Commands.StoreAndExecute(new ColorCommand(layerIndex, keyIndex, r, g, b, a));
		}

		public void ChangeColor(int layerIndex, int keyIndex, in GrfColor color) {
			_str.Commands.StoreAndExecute(new ColorCommand(layerIndex, keyIndex, color.R, color.G, color.B, color.A));
		}

		public void SetScaleBias(int layerIndex, int keyIndex, float bias) {
			_str.Commands.StoreAndExecute(new ScaleBiasCommand(layerIndex, keyIndex, bias));
		}

		public void SetAngleBias(int layerIndex, int keyIndex, float bias) {
			_str.Commands.StoreAndExecute(new RotateBiasCommand(layerIndex, keyIndex, bias));
		}

		public void SetOffsetBias(int layerIndex, int keyIndex, float bias) {
			_str.Commands.StoreAndExecute(new OffsetBiasCommand(layerIndex, keyIndex, bias));
		}

		public void SetAngle(int layerIndex, int keyIndex, float angle) {
			if (Math.Abs(angle - _str[layerIndex, keyIndex].Angle) < ChangeEpsilon)
				return;
			_str.Commands.StoreAndExecute(new RotateCommand(layerIndex, keyIndex, angle));
		}

		public void SetRotation(int layerIndex, int keyIndex, float angle) {
			SetAngle(layerIndex, keyIndex, angle);
		}

		public void SetSrcBlend(int layerIndex, int keyIndex, int blend) {
			if (blend == _str[layerIndex, keyIndex].SourceAlpha)
				return;
			_str.Commands.StoreAndExecute(new BlendCommand(layerIndex, keyIndex, blend, 0));
		}

		public void SetDstBlend(int layerIndex, int keyIndex, int blend) {
			if (blend == _str[layerIndex, keyIndex].DestinationAlpha)
				return;
			_str.Commands.StoreAndExecute(new BlendCommand(layerIndex, keyIndex, blend, 1));
		}

		public void SetOffset(int layerIndex, int keyIndex, float x, float y) {
			if (Math.Abs(x - _str[layerIndex, keyIndex].Offset.X) < ChangeEpsilon &&
				Math.Abs(y - _str[layerIndex, keyIndex].Offset.Y) < ChangeEpsilon)
				return;
			_str.Commands.StoreAndExecute(new OffsetCommand(layerIndex, keyIndex, x, y));
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

		public void SetVertices(int layerIndex, int keyIndex, float[] vertices) {
			if (Enumerable.SequenceEqual(vertices, _str[layerIndex, keyIndex].Xy))
				return;
			_str.Commands.StoreAndExecute(new CoordsCommand(layerIndex, keyIndex, vertices));
		}

		public void SetBezier(int layerIndex, int keyIndex, float[] bezierPoints) {
			if (Enumerable.SequenceEqual(bezierPoints, _str[layerIndex, keyIndex].Bezier))
				return;
			_str.Commands.StoreAndExecute(new BezierCommand(layerIndex, keyIndex, bezierPoints));
		}

		public void SetVertex(int layerIndex, int keyIndex, int point, float offset) {
			if (Math.Abs(offset - _str[layerIndex, keyIndex].Xy[point]) < ChangeEpsilon)
				return;
			_str.Commands.StoreAndExecute(new CoordsCommand(layerIndex, keyIndex, point, offset));
		}

		public void SetTextCoords(int layerIndex, int keyIndex, float[] coords) {
			if (Enumerable.SequenceEqual(coords, _str[layerIndex, keyIndex].Uv))
				return;
			_str.Commands.StoreAndExecute(new TextCoordsCommand(layerIndex, keyIndex, coords));
		}

		public void ChangeTextureIndex(int layerIndex, int keyIndex, int textureIndex) {
			if (textureIndex == _str[layerIndex, keyIndex].TextureIndex)
				return;
			_str.Commands.StoreAndExecute(new TextureCommand(layerIndex, keyIndex, textureIndex));
		}

		public void ChangeTextures(int layerIndex, List<string> textures) {
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

		public void ChangeAnimationType(int layerIndex, int keyIndex, int animationType) {
			_str.Commands.StoreAndExecute(new AnimationTypeCommand(layerIndex, keyIndex, animationType));
		}

		public void ChangeFps(int fps) {
			_str.Commands.StoreAndExecute(new FpsCommand(fps));
		}

		public void ChangeDelay(int layerIndex, int keyIndex, float interval) {
			if (Math.Abs(interval - _str[layerIndex, keyIndex].Delay) < ChangeEpsilon)
				return;
			_str.Commands.StoreAndExecute(new DelayCommand(layerIndex, keyIndex, interval));
		}

		public void ChangeMaxFrame(int maxFrame) {
			if (maxFrame == _str.MaxKeyFrame)
				return;
			_str.Commands.StoreAndExecute(new MaxFrameCommand(maxFrame));
		}

		public void SetInterpolated(int layerIndex, int keyIndex, bool isInterpolated) {
			if (keyIndex < 0 || _str[layerIndex, keyIndex].IsInterpolated == isInterpolated)
				return;
			_str.Commands.StoreAndExecute(new SetInterpolatedCommand(layerIndex, keyIndex, isInterpolated));
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
