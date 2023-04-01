using System;
using System.IO;
using GRF.FileFormats.SprFormat;
using GRF.IO;
using Utilities.Extension;

namespace GRF.FileFormats.ActFormat {
	public static class ActConverter {
		#region IActConverter Members

		public static void LoadActions(Act act, IBinaryReader reader, bool loadAnchors = true) {
			reader.Position = 4;
			int numberOfActions = reader.UInt16();
			reader.Position = 16;

			try {
				act.Actions.Capacity = numberOfActions;

				for (int i = 0; i < numberOfActions; i++) {
					Action action = new Action();
					int numberOfFrames = reader.Int32();
					action.Frames.Capacity = numberOfFrames;

					for (int j = 0; j < numberOfFrames; j++) {
						reader.Forward(32);
						Frame frame = new Frame();
						int numberOfLayers = reader.Int32();
						frame.Layers.Capacity = numberOfLayers;

						for (int k = 0; k < numberOfLayers; k++) {
							Layer layer = new Layer();

							layer.OffsetX = reader.Int32();
							layer.OffsetY = reader.Int32();
							layer.SpriteIndex = reader.Int32();
							layer.Mirror = reader.Int32() != 0;

							if (act.Header.IsCompatibleWith(2, 0)) {
								layer.Color.R = reader.Byte();
								layer.Color.G = reader.Byte();
								layer.Color.B = reader.Byte();
								layer.Color.A = reader.Byte();

								layer.ScaleX = reader.Float();
								layer.ScaleY = layer.ScaleX;

								if (act.Header.IsCompatibleWith(2, 4)) {
									layer.ScaleY = reader.Float();
								}

								layer.Rotation = reader.Int32();
								layer.SpriteType = (SpriteTypes) reader.Int32();

								if (act.Header.IsCompatibleWith(2, 5)) {
									layer.Width = reader.Int32();
									layer.Height = reader.Int32();
								}
							}

							// Overrides the width and height
							if (layer.SpriteIndex >= 0) {
								var image = act.Sprite.GetImage(layer.SpriteIndex, layer.SpriteType);

								if (image != null) {
									layer.Width = image.Width;
									layer.Height = image.Height;
								}
							}

							frame.Layers.Add(layer);
						}

						if (act.Header.IsCompatibleWith(2, 0)) {
							frame.SoundId = reader.Int32();
						}

						if (loadAnchors) {
							if (act.Header.IsCompatibleWith(2, 3)) {
								int count = reader.Int32();

								for (int k = 0; k < count; k++) {
									frame.Anchors.Add(new Anchor(reader.Bytes(4), reader.Int32(), reader.Int32(), reader.Int32()));
								}
							}
						}

						action.Frames.Add(frame);
					}

					act.Actions.Add(action);
				}

				if (act.Header.IsCompatibleWith(2, 1)) {
					int numberOfSounds = reader.Int32();

					for (int i = 0; i < numberOfSounds; i++) {
						act.SoundFiles.Add(reader.String(40, '\0'));
					}

					if (act.Header.IsCompatibleWith(2, 2)) {
						float speed = 1.0f;

						for (int i = 0; i < act.Actions.Count; i++) {
							if (!reader.CanRead) {
								act.Actions[i].AnimationSpeed = speed;
							}
							else {
								speed = act.Actions[i].AnimationSpeed = reader.Float();
							}
						}
					}
				}
			}
			catch {
				// Fix : 2015-04-06
				// This is not really a fix; it used to be part of the 
				// loader for the Act format. In some rare cases, the anchors
				// count is 0, yet... there is still data present that needs
				// to be ignored. This bug is only present for 0x203 and 0x204
				if (loadAnchors && (act.Header.Is(2, 3) || act.Header.Is(2, 4))) {
					act.Actions.Clear();
					LoadActions(act, reader, false);
				}
				else throw;
			}
		}

		public static void Save(Act act, string filename, Spr sprite) {
			Save(act, File.Create(filename), sprite, true);
		}

		public static void Save(Act act, Stream stream, Spr sprite, bool close) {
			BinaryWriter writer = new BinaryWriter(stream);

			try {
				act.Header.Write(writer);
				writer.Write((byte) 5);
				writer.Write((byte) 2);

				if (act.Actions.Count > UInt16.MaxValue)
					throw new OverflowException("The number of actions must be below " + UInt16.MaxValue);

				writer.Write((UInt16) act.Actions.Count);
				writer.Write(new byte[10]);

				for (int i = 0; i < act.Actions.Count; i++) {
					act[i].Write(writer, sprite);
				}

				writer.Write(act.SoundFiles.Count);

				foreach (string soundFile in act.SoundFiles) {
					writer.WriteANSI(soundFile, 40);
				}

				foreach (Action action in act.Actions) {
					writer.Write(action.AnimationSpeed);
				}
			}
			finally {
				if (close)
					writer.Close();
			}
		}

		#endregion
	}
}