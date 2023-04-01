using System;
using System.Collections.Generic;
using GRF.Image;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class CommandsHolder : AbstractCommand<IStrCommand> {
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

		public void ChangeColor(int layerIndex, int keyIndex, float a, float r, float g, float b) {
			_str.Commands.StoreAndExecute(new ColorCommand(layerIndex, keyIndex, r, g, b, a));
		}

		public void ChangeColor(int layerIndex, int keyIndex, GrfColor color) {
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
			_str.Commands.StoreAndExecute(new RotateCommand(layerIndex, keyIndex, angle));
		}

		public void SetRotation(int layerIndex, int keyIndex, float angle) {
			SetAngle(layerIndex, keyIndex, angle);
		}

		public void SetSrcBlend(int layerIndex, int keyIndex, int blend) {
			_str.Commands.StoreAndExecute(new BlendCommand(layerIndex, keyIndex, blend, 0));
		}

		public void SetDstBlend(int layerIndex, int keyIndex, int blend) {
			_str.Commands.StoreAndExecute(new BlendCommand(layerIndex, keyIndex, blend, 1));
		}

		public void SetOffset(int layerIndex, int keyIndex, float x, float y) {
			_str.Commands.StoreAndExecute(new OffsetCommand(layerIndex, keyIndex, x, y));
		}

		public void AddKey(int layerIndex, int keyIndex, StrKeyFrame frame) {
			_str.Commands.StoreAndExecute(new AddKeyCommand(layerIndex, keyIndex, frame));
		}

		public void DeleteKey(int layerIndex, int keyIndex) {
			_str.Commands.StoreAndExecute(new DeleteKeyCommand(layerIndex, keyIndex));
		}

		public void Scale(int layerIndex, int keyIndex, float x, float y) {
			_str.Commands.StoreAndExecute(new ScaleCommand(layerIndex, keyIndex, x, y));
		}

		public void SetVertices(int layerIndex, int keyIndex, float[] vertices) {
			_str.Commands.StoreAndExecute(new CoordsCommand(layerIndex, keyIndex, vertices));
		}

		public void SetBezier(int layerIndex, int keyIndex, float[] bezierPoints) {
			_str.Commands.StoreAndExecute(new BezierCommand(layerIndex, keyIndex, bezierPoints));
		}

		public void SetVertex(int layerIndex, int keyIndex, int point, float offset) {
			_str.Commands.StoreAndExecute(new CoordsCommand(layerIndex, keyIndex, point, offset));
		}

		public void SetTextCoords(int layerIndex, int keyIndex, float[] coords) {
			_str.Commands.StoreAndExecute(new TextCoordsCommand(layerIndex, keyIndex, coords));
		}

		public void ChangeTextureIndex(int layerIndex, int keyIndex, int textureIndex) {
			_str.Commands.StoreAndExecute(new TextureCommand(layerIndex, keyIndex, textureIndex));
		}

		public void ChangeTextures(int layerIndex, int keyIndex, List<string> textures) {
			_str.Commands.StoreAndExecute(new TexturesCommand(layerIndex, keyIndex, textures));
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
			_str.Commands.StoreAndExecute(new DelayCommand(layerIndex, keyIndex, interval));
		}

		public void ChangeMaxFrame(int maxFrame) {
			_str.Commands.StoreAndExecute(new MaxFrameCommand(maxFrame));
		}

		public void SetInterpolated(int layerIndex, int keyIndex, bool isInterpolated) {
			if (_str[layerIndex, keyIndex].IsInterpolated == isInterpolated) return;
			_str.Commands.StoreAndExecute(new SetInterpolatedCommand(layerIndex, keyIndex, isInterpolated));
		}

		public void FlipH(int layerIndex, int keyIndex) {
			_str.Commands.StoreAndExecute(new FlipHCommand(layerIndex, keyIndex, new Graphics.Point(319, 291)));
		}

		public void FlipH(int layerIndex, int keyIndex, Graphics.Point origin) {
			_str.Commands.StoreAndExecute(new FlipHCommand(layerIndex, keyIndex, origin));
		}

		public void FlipV(int layerIndex, int keyIndex) {
			_str.Commands.StoreAndExecute(new FlipVCommand(layerIndex, keyIndex, new Graphics.Point(319, 291)));
		}

		public void FlipV(int layerIndex, int keyIndex, Graphics.Point origin) {
			_str.Commands.StoreAndExecute(new FlipVCommand(layerIndex, keyIndex, origin));
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
