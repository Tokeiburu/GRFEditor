using GRF.Graphics;
using System;
using System.Collections.Generic;

namespace GRF.FileFormats.StrFormat.Commands {
	public class CenterOriginCommand : IStrCommand, IPosCommand {
		private readonly int _layerIndex;
		private readonly bool _groupEdit;
		private readonly int _keyIndex;
		private List<IStrCommand> _commands;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public CenterOriginCommand(int layerIndex) {
			_layerIndex = layerIndex;
			_keyIndex = -1;
			_groupEdit = true;
		}

		public CenterOriginCommand(int layerIndex, int keyIndex) {
			_layerIndex = layerIndex;
			_keyIndex = keyIndex;
			_groupEdit = false;
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Center origin";

		public void Execute(Str str) {
			if (_commands != null) {
				foreach (var command in _commands)
					command.Execute(str);

				return;
			}

			_commands = new List<IStrCommand>();
			var layer = str[_layerIndex];

			if (_groupEdit) {
				for (int keyIndex = 0; keyIndex < layer.KeyFrames.Count; keyIndex++) {
					_addCenterOriginKeyFrame(keyIndex, layer[keyIndex]);
				}
			}
			else {
				_addCenterOriginKeyFrame(_keyIndex, layer[_keyIndex]);
			}

			foreach (var command in _commands)
				command.Execute(str);
		}

		private void _addCenterOriginKeyFrame(int keyIndex, StrKeyFrame keyFrame) {
			var pointCenter = new TkVector2();

			for (int i = 0; i < 4; i++) {
				pointCenter.X += keyFrame.Positions[i];
				pointCenter.Y += keyFrame.Positions[i + 4];
			}

			pointCenter.X /= 4;
			pointCenter.Y /= 4;

			var vertices = new float[8];

			for (int i = 0; i < 4; i++) {
				vertices[i] = keyFrame.Positions[i] - pointCenter.X;
				vertices[i + 4] = keyFrame.Positions[i + 4] - pointCenter.Y;
			}

			_commands.Add(new SetPositionsCommand(_layerIndex, keyIndex, vertices));
		}

		public void Undo(Str str) {
			foreach (var command in _commands)
				command.Undo(str);
		}
	}
}
