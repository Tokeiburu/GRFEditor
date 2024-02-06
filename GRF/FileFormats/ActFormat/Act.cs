using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.FileFormats.ActFormat.Commands;
using GRF.FileFormats.SprFormat;
using GRF.Graphics;
using GRF.IO;
using GRF.Image;
using Utilities.Extension;

namespace GRF.FileFormats.ActFormat {
	// ACT files only load the animation when required, otherwise the data isn't processed
	public class Act : IEnumerable<Action>, IDisposable {
		#region Delegates

		public delegate void InvalidateVisualDelegate(object sender);

		#endregion

		private readonly List<string> _soundFiles = new List<string>();

		private List<Action> _actions = new List<Action>();
		private bool _disposed; // IDisposable member

		public Act() : this(new Spr()) {
		}

		public Act(Spr spr) {
			Sprite = spr;
			Commands = new CommandsHolder(this);

			Header = new ActHeader();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Act" /> class.
		/// </summary>
		/// <param name="actData">The act data.</param>
		public Act(MultiType actData)
			: this(actData, new Spr()) {
		}

		public Act(MultiType actData, Spr spr)
			: this(actData.Data, spr, true) {
			LoadedPath = actData.Path;
		}

		public Act(MultiType actData, MultiType sprData)
			: this(actData.Data, sprData.Data) {
			LoadedPath = actData.Path;
		}

		public Act(Act act) {
			foreach (var action in act) {
				Actions.Add(new Action(action));
			}

			Commands = new CommandsHolder(this);
			Name = act.Name;
			AnchoredTo = act.AnchoredTo;
			Sprite = new Spr(act.Sprite);
		}

		internal Act(byte[] actData, byte[] sprData)
			: this(actData, new Spr(sprData), true) {
		}

		internal Act(byte[] data, Spr spr, bool partialReadAllowed) {
			Commands = new CommandsHolder(this);
			Sprite = spr;

			try {
				ByteReader reader = new ByteReader(data);
				Header = new ActHeader(reader);
				ActConverter.LoadActions(this, reader);
			}
			catch (Exception err) {
				if (partialReadAllowed)
					ErrorHandler.HandleException(err);
				else
					throw;
			}
		}

		public CommandsHolder Commands { get; private set; }

		public ActHeader Header { get; set; }

		public double Version {
			get { return Header.Version; }
		}

		public List<Action> Actions {
			get { return _actions; }
			set { _actions = value; }
		}

		public Action this[int index] {
			get { return Actions[index]; }
		}

		public Layer this[ActIndex index] {
			get { return this[index.ActionIndex, index.FrameIndex, index.LayerIndex]; }
		}

		public Frame this[int ai, int frameIndex] {
			get { return this[ai].Frames[frameIndex]; }
			set { this[ai].Frames[frameIndex] = value; }
		}

		public Layer this[int ai, int frameIndex, int layerIndex] {
			get { return this[ai].Frames[frameIndex].Layers[layerIndex]; }
			set { this[ai].Frames[frameIndex].Layers[layerIndex] = value; }
		}

		public int NumberOfActions {
			get { return Actions.Count; }
		}

		public string LoadedPath { get; set; }

		public Spr Sprite { get; internal set; }
		public Act AnchoredTo { get; set; }
		public string Name { get; set; }

		public List<string> SoundFiles {
			get { return _soundFiles; }
		}

		#region IDisposable Members

		public void Dispose() {
			Dispose(true);
		}

		#endregion

		#region IEnumerable<Action> Members

		public IEnumerator<Action> GetEnumerator() {
			for (int i = 0; i < NumberOfActions; i++) {
				yield return this[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion

		public event InvalidateVisualDelegate VisualInvalidated;
		public event InvalidateVisualDelegate SpriteVisualInvalidated;
		public event InvalidateVisualDelegate SpritePaletteInvalidated;

		public void OnSpritePaletteInvalidated() {
			InvalidateVisualDelegate handler = SpritePaletteInvalidated;
			if (handler != null) handler(this);
		}

		public void OnSpriteVisualInvalidated() {
			InvalidateVisualDelegate handler = SpriteVisualInvalidated;
			if (handler != null) handler(this);
		}

		public void OnVisualInvalidated() {
			InvalidateVisualDelegate handler = VisualInvalidated;
			if (handler != null) handler(this);
		}

		public static Act DebugLoad(byte[] entireData, Spr spr) {
			return new Act(entireData, spr, false);
		}

		public void Safe() {
			AllActions(a => {
				if (a.Frames.Count == 0) {
					a.Frames.Add(new Frame());
				}
			});
		}

		public Layer TryGetLayer(int ai, int frameIndex, int layerIndex) {
			if (ai < NumberOfActions &&
			    frameIndex < this[ai].NumberOfFrames &&
			    layerIndex < this[ai, frameIndex].NumberOfLayers) {
				return this[ai, frameIndex, layerIndex];
			}

			return null;
		}

		public Layer TryGetLayer(ActIndex index) {
			return TryGetLayer(index.ActionIndex, index.FrameIndex, index.LayerIndex);
		}

		public Frame TryGetFrame(int ai, int frameIndex) {
			if (ai < NumberOfActions &&
			    frameIndex < this[ai].NumberOfFrames) {
				return this[ai, frameIndex];
			}

			return null;
		}

		public Action TryGetAction(int ai) {
			if (ai < NumberOfActions) {
				return this[ai];
			}

			return null;
		}

		public void SetSprite(Spr spr) {
			Sprite = spr;
		}

		protected virtual void Dispose(bool disposing) {
			if (!_disposed) {
				if (disposing) {
				}

				_disposed = true;
			}
		}

		public void RemoveLayer(int action, int frame, int layer) {
			if (Actions[action].Frames[frame].Layers.Count > layer)
				Actions[action].Frames[frame].Layers.RemoveAt(layer);
		}

		public void ClearLayers(int action, int frame) {
			Actions[action].Frames[frame].Layers.Clear();
		}

		public void AddLayer(int action, int frame, Layer layer, int layerIndex = -1) {
			if (layerIndex < 0) {
				Actions[action].Frames[frame].Layers.Add(layer);
			}
			else {
				Actions[action].Frames[frame].Layers.Insert(layerIndex, layer);
			}
		}

		public void InsertAction(int ai, List<Action> actions) {
			Actions.InsertRange(ai, actions);
		}

		public Action AddAction() {
			Actions.Add(new Action { Frames = new List<Frame> { new Frame() } });
			return Actions[Actions.Count - 1];
		}

		public Action AddAction(Action action) {
			Actions.Add(action);
			return Actions[Actions.Count - 1];
		}

		public void DeleteActions(int sourceIndex, int length) {
			Actions.RemoveRange(sourceIndex, length);
		}

		public Action AddAction(Action action, int index) {
			Actions.Insert(index, action);
			return Actions[index];
		}

		public void SetAction(int index, Action action) {
			if (index == Actions.Count) {
				AddAction(action);
			}
			else if (index > Actions.Count || index < 0) {
				throw new Exception("The index is below 0 or greater than the number of possible actions.");
			}
			else {
				Actions[index] = action;
			}
		}

		public List<Frame> RemoveFrames(int ai, int startFrameIndex, int count) {
			List<Frame> frames = this[ai].Frames.Skip(startFrameIndex).Take(count).ToList();
			this[ai].Frames.RemoveRange(startFrameIndex, count);
			return frames;
		}

		public List<Layer> RemoveLayers(int ai, int frameIndex, int startLayerIndex, int count) {
			List<Layer> layers = this[ai, frameIndex].Layers.Skip(startLayerIndex).Take(count).ToList();
			this[ai, frameIndex].Layers.RemoveRange(startLayerIndex, count);
			return layers;
		}

		public List<GrfImage> RemoveSprites(int baseIndex, int count) {
			List<GrfImage> images = new List<GrfImage>();

			if (Sprite == null)
				throw new Exception("No sprite loaded (used the obsolete constructor?).");

			for (int i = baseIndex + count - 1; i >= baseIndex; i--) {
				int indexToDelete;
				int spriteType;

				if (i < Sprite.NumberOfIndexed8Images) {
					indexToDelete = i;
					spriteType = 0;
					Sprite.NumberOfIndexed8Images--;
				}
				else {
					indexToDelete = i - Sprite.NumberOfIndexed8Images;
					spriteType = 1;
					Sprite.NumberOfBgra32Images--;
				}

				for (int k = 0; k < Actions.Count; k++) {
					Action action = this[k];

					foreach (Frame frame in action.Frames) {
						for (int j = 0; j < frame.NumberOfLayers; j++) {
							if (frame.Layers[j].SpriteTypeInt == spriteType && frame.Layers[j].SpriteIndex == indexToDelete) {
								frame.Layers.RemoveAt(j);
								j--;
							}
							else if (frame.Layers[j].SpriteTypeInt == spriteType && frame.Layers[j].SpriteIndex > indexToDelete) {
								frame.Layers[j].SpriteIndex--;
							}
						}
					}
				}

				images.Insert(0, Sprite.Images[i]);
				Sprite.Images.RemoveAt(i);
			}

			return images;
		}

		public void Translate(int offsetX, int offsetY) {
			foreach (Action action in this) {
				action.Translate(offsetX, offsetY);
			}
		}

		public void Rotate(int rotate) {
			foreach (Action action in this) {
				action.Rotate(rotate);
			}
		}

		public void Scale(float scale) {
			Scale(scale, scale);
		}

		public void Scale(float scaleX, float scaleY) {
			foreach (Action action in this) {
				action.Scale(scaleX, scaleY);
			}
		}

		public void SetColor(string color) {
			GrfColor grfColor = new GrfColor(color);
			AllLayers(p => p.Color = grfColor);
		}

		public void SetAnimationSpeed(float speed) {
			AllActions(p => p.AnimationSpeed = speed);
		}

		public void SetInterval(int interval) {
			AllActions(p => p.Interval = interval);
		}

		public void Magnify(float value) {
			Magnify(value, false);
		}

		public void Magnify(float value, bool anchors) {
			AllActions(p => p.Magnify(value));

			if (anchors) {
				AllAnchors(anchor => anchor.Magnify(value));
			}
		}

		public GrfImage RemoveSprite(int spriteIndex) {
			return RemoveSprites(spriteIndex, 1)[0];
		}

		public void AllLayers(Action<Layer> func) {
			for (int i = 0; i < NumberOfActions; i++) {
				foreach (Frame frame in this[i].Frames) {
					foreach (Layer layer in frame.Layers) {
						func(layer);
					}
				}
			}
		}

		public void AllLayers(Action<Layer, int, int, int> func) {
			for (int aid = 0; aid < NumberOfActions; aid++) {
				for (int fid = 0; fid < this[aid].Frames.Count; fid++) {
					Frame frame = this[aid, fid];

					for (int lid = 0; lid < frame.Layers.Count; lid++) {
						Layer layer = frame.Layers[lid];
						
						func(layer, aid, fid, lid);
					}
				}
			}
		}

		public void AllAnchors(Action<Anchor> func) {
			AllFrames(frame => {
				foreach (Anchor anchor in frame.Anchors) {
					func(anchor);
				}
			});
		}

		public void AllAnchors(Action<Anchor, int> func) {
			AllFrames(frame => {
				for (int index = 0; index < frame.Anchors.Count; index++) {
					func(frame.Anchors[index], index);
				}
			});
		}

		public void AllActions(Action<Action> func) {
			for (int i = 0; i < NumberOfActions; i++) {
				func(this[i]);
			}
		}

		public void AllActions(Action<Action, int> func) {
			for (int i = 0; i < NumberOfActions; i++) {
				func(this[i], i);
			}
		}

		public void InvalidateVisual() {
			OnVisualInvalidated();
		}

		public void InvalidateSpriteVisual() {
			OnSpriteVisualInvalidated();
		}

		public List<Frame> GetAllFrames() {
			List<Frame> frames = new List<Frame>();

			for (int i = 0; i < NumberOfActions; i++) {
				frames.AddRange(this[i].Frames);
			}

			return frames;
		}

		public List<Layer> GetAllLayers() {
			List<Layer> layers = new List<Layer>();

			for (int i = 0; i < NumberOfActions; i++) {
				foreach (Frame frame in this[i].Frames) {
					layers.AddRange(frame.Layers);
				}
			}

			return layers;
		}

		public void ApplyMirror() {
			foreach (var layer in this) {
				layer.ApplyMirror();
			}
		}

		public void ApplyMirror(bool mirror) {
			foreach (var layer in this) {
				layer.ApplyMirror(mirror);
			}
		}

		public override string ToString() {
			return String.Format("Actions = {0}, Version = {1}", NumberOfActions, Header.HexVersionFormat);
		}

		public void AllLayersAdv(Action<ActIndex, Layer> func) {
			Action action;
			Frame frame;

			for (int a = 0; a < NumberOfActions; a++) {
				action = this[a];

				for (int f = 0; f < action.NumberOfFrames; f++) {
					frame = action[f];

					for (int s = 0; s < frame.NumberOfLayers; s++) {
						func(new ActIndex { ActionIndex = a, FrameIndex = f, LayerIndex = s }, frame[s]);
					}
				}
			}
		}

		public void AllFrames(Action<Frame> func) {
			GetAllFrames().ForEach(func);
		}

		public void AllFrames(Action<Frame, int, int> func) {
			for (int aid = 0; aid < NumberOfActions; aid++) {
				Action action = this[aid];

				for (int fid = 0; fid < action.NumberOfFrames; fid++) {
					func(action[fid], aid, fid);
				}
			}
		}

		public List<ActIndex> FindUsageOf(int absoluteSpriteIndex) {
			List<ActIndex> indexes = new List<ActIndex>();

			AllLayersAdv((index, layer) => {
				if (layer.GetAbsoluteSpriteId(Sprite) == absoluteSpriteIndex)
					indexes.Add(index);
			});

			return indexes;
		}

		public void InvalidatePaletteVisual() {
			OnSpritePaletteInvalidated();
		}

		public void Save(string path) {
			ActConverter.Save(this, path, Sprite);
		}

		public void SaveNoSprite(string path) {
			ActConverter.Save(this, path, null);
		}

		public void Save(Stream stream) {
			ActConverter.Save(this, stream, Sprite, false);
		}

		public void SaveNoSprite(Stream stream) {
			ActConverter.Save(this, stream, null, false);
		}

		public void SaveWithSprite(string actPath, string sprPath = null) {
			if (sprPath == null)
				sprPath = actPath.ReplaceExtension(".spr");

			Sprite.Save(sprPath);
			Save(actPath);
		}

		public void SaveWithSprite(Stream actStream, Stream sprStream) {
			GrfExceptions.IfNullThrow(sprStream, "sprStream");

			Sprite.Save(sprStream);
			Save(actStream);
		}

		public void Save() {
			GrfExceptions.IfNullThrow(LoadedPath, "LoadedPath");
			Save(LoadedPath);
		}

		public List<string> GetAnimationStrings() {
			int actionsCount = NumberOfActions;

			if (actionsCount == 5 * 8) {
				return new List<string> {
					"0 - Idle",
					"1 - Walk",
					"2 - Attack",
					"3 - Receiving damage",
					"4 - Die",
				};
			}

			if (actionsCount == 4 * 8) {
				return new List<string> {
					"0 - Idle",
					"1 - Walk",
					"2 - Receiving damage",
					"3 - Attack",
				};
			}

			if (actionsCount == 13 * 8) {
				return new List<string> {
					"0 - Idle",
					"1 - Walking",
					"2 - Sitting",
					"3 - Picking item",
					"4 - Standby",
					"5 - Attacking1",
					"6 - Receiving damage",
					"7 - Freeze1",
					"8 - Dead",
					"9 - Freeze2",
					"10 - Attacking2 (no weapon)",
					"11 - Attacking3 (weapon)",
					"12 - Casting spell"
				};
			}

			if (actionsCount == 9 * 8) {
				return new List<string> {
					"0 - Idle",
					"1 - Walking",
					"2 - Attacking",
					"3 - Receiving damage",
					"4 - Dead",
					"5 - Special",
					"6 - Perf1",
					"7 - Perf2",
					"8 - Perf3",
				};
			}

			if (actionsCount == 2 * 8) {
				return new List<string> {
					"0 - Idle",
					"1 - Walking",
				};
			}

			if (actionsCount == 8 * 8) {
				return new List<string> {
					"0 - Idle",
					"1 - Walking",
					"2 - Attacking1",
					"3 - Receiving damage",
					"4 - Dead",
					"5 - Attacking2",
					"6 - Attacking3",
					"7 - Action",
				};
			}

			if (actionsCount == 7 * 8) {
				return new List<string> {
					"0 - Idle",
					"1 - Walking",
					"2 - Attacking1",
					"3 - Receiving damage",
					"4 - Dead",
					"5 - Attacking2",
					"6 - Attacking3",
				};
			}

			if (actionsCount == 8) {
				return new List<string> {
					"0 - Idle",
				};
			}

			int animations = (int) Math.Ceiling(actionsCount / 8f);

			List<string> items = new List<string>();

			for (int i = 0; i < animations; i++) {
				items.Add(i.ToString(CultureInfo.InvariantCulture));
			}

			return items;
		}

		public void AnimationExecute(int index, Action<Action> action) {
			for (int i = 8 * index, end = 8 * (index + 1); i < end && i < NumberOfActions; i++) {
				action(this[i]);
			}
		}

		public void AnimationExecute(IEnumerable<int> indexes, Action<Action> action) {
			foreach (int index in indexes) {
				AnimationExecute(index, action);
			}
		}

		public static Act MergeAct(Act[] back, Act primary, Act[] front) {
			Act output = new Act(primary);

			foreach (var act in back) {
				_mergeAct(output, act, false);
			}

			foreach (var act in front) {
				_mergeAct(output, act, true);
			}

			return output;
		}

		public static Vertex CalculateAnchorDiff(Act act, int actionIndex, int frameIndex, int? anchorFrameIndex) {
			Vertex vertex = new Vertex();
			Frame frame = act[actionIndex, frameIndex];

			if (act.AnchoredTo != null && frame.Anchors.Count > 0) {
				Frame frameReference;

				if (anchorFrameIndex != null && act.Name != null && act.AnchoredTo.Name != null) {
					frameReference = act.AnchoredTo.TryGetFrame(actionIndex, frameIndex);

					if (frameReference == null) {
						frameReference = act.AnchoredTo.TryGetFrame(actionIndex, anchorFrameIndex.Value);
					}
				}
				else {
					frameReference = act.AnchoredTo.TryGetFrame(actionIndex, anchorFrameIndex ?? frameIndex);
				}

				if (frameReference != null && frameReference.Anchors.Count > 0) {
					vertex.X = frameReference.Anchors[0].OffsetX - frame.Anchors[0].OffsetX;
					vertex.Y = frameReference.Anchors[0].OffsetY - frame.Anchors[0].OffsetY;

					if (act.AnchoredTo.AnchoredTo != null) {
						frameReference = act.AnchoredTo.AnchoredTo.TryGetFrame(actionIndex, anchorFrameIndex ?? frameIndex);

						if (frameReference != null && frameReference.Anchors.Count > 0) {
							vertex.X = frameReference.Anchors[0].OffsetX - frame.Anchors[0].OffsetX;
							vertex.Y = frameReference.Anchors[0].OffsetY - frame.Anchors[0].OffsetY;
						}
					}
				}
			}

			return vertex;
		}

		private static Act _mergeAct(Act input, Act act, bool front) {
			int newIndexed8Index = input.Sprite.NumberOfIndexed8Images;
			int newBgra32Index = input.Sprite.NumberOfImagesLoaded;
			act = new Act(act);

			foreach (var image in act.Sprite.Images)
				input.Sprite.AddImage(image);

			act.Sprite.ShiftIndexesAbove(act, GrfImageType.Indexed8, newIndexed8Index, -1);
			act.Sprite.ShiftIndexesAbove(act, GrfImageType.Bgra32, newBgra32Index, -1);

			for (int ai = 0; ai < input.NumberOfActions; ai++) {
				if (ai >= act.NumberOfActions) return input;
				var action = input[ai];
				var frameIndex = 0;
				HashSet<int> framesTransformed = new HashSet<int>();

				for (int fi = 0; fi < action.NumberOfFrames; fi++) {
					int? anchorFrameIndex = null;
					frameIndex = fi;

					if (act.Name == "Head" || act.Name == "Body") {
						bool handled = false;

						if (act[ai].NumberOfFrames == 3 &&
						    (0 <= ai && ai < 8) ||
						    (16 <= ai && ai < 24)) {
							int group = input[ai].NumberOfFrames / 3;

							if (group != 0) {
								anchorFrameIndex = frameIndex;

								if (frameIndex < group) {
									frameIndex = 0;
									handled = true;
								}
								else if (frameIndex < 2 * group) {
									frameIndex = 1;
									handled = true;
								}
								else if (frameIndex < 3 * group) {
									frameIndex = 2;
									handled = true;
								}
								else {
									frameIndex = 2;
									handled = true;
								}
							}
						}

						if (!handled) {
							if (frameIndex >= act[ai].NumberOfFrames) {
								if (act[ai].NumberOfFrames > 0)
									frameIndex = frameIndex % act[ai].NumberOfFrames;
								else
									frameIndex = 0;
							}
						}
					}
					else {
						if (frameIndex >= act[ai].NumberOfFrames) {
							if (act[ai].NumberOfFrames > 0)
								frameIndex = frameIndex % act[ai].NumberOfFrames;
							else
								frameIndex = 0;
						}
					}

					Frame frame = act[ai, frameIndex];
					//if (LayerIndex >= frame.NumberOfLayers) return;

					int diffX = 0;
					int diffY = 0;

					if (act.AnchoredTo != null && frame.Anchors.Count > 0) {
						Frame frameReference;

						if (anchorFrameIndex != null && act.Name != null && act.AnchoredTo.Name != null) {
							frameReference = act.AnchoredTo.TryGetFrame(ai, frameIndex);

							if (frameReference == null) {
								frameReference = act.AnchoredTo.TryGetFrame(ai, anchorFrameIndex.Value);
							}
						}
						else {
							frameReference = act.AnchoredTo.TryGetFrame(ai, anchorFrameIndex ?? frameIndex);
						}

						if (frameReference != null && frameReference.Anchors.Count > 0) {
							diffX = frameReference.Anchors[0].OffsetX - frame.Anchors[0].OffsetX;
							diffY = frameReference.Anchors[0].OffsetY - frame.Anchors[0].OffsetY;

							if (act.AnchoredTo.AnchoredTo != null) {
								frameReference = act.AnchoredTo.AnchoredTo.TryGetFrame(ai, anchorFrameIndex ?? frameIndex);

								if (frameReference != null && frameReference.Anchors.Count > 0) {
									diffX = frameReference.Anchors[0].OffsetX - frame.Anchors[0].OffsetX;
									diffY = frameReference.Anchors[0].OffsetY - frame.Anchors[0].OffsetY;
								}
							}
						}
					}

					if (!framesTransformed.Contains(frameIndex)) {
						frame.Translate(diffX, diffY);
						framesTransformed.Add(frameIndex);
					}

					Frame current = input[ai, fi];

					if (front) {
						current.Layers.AddRange(frame);
					}
					else {
						current.Layers.InsertRange(0, frame);
					}
				}
			}

			return input;
		}
	}
}