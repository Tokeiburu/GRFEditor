using ActEditor.Core.WPF.EditorControls.ActSelectorComponents;
using ErrorManager;
using GRF.FileFormats.ActFormat;
using System;
using System.Diagnostics;
using System.Threading;

namespace GrfToWpfBridge.ActRenderer.ActSelectorComponents {
	public static class ActAnimation {
		public static float MaxAnimationSpeed = 0.1f;

		public static int SafeActionIndex(Act act, int selectedAction) {
			if (selectedAction >= act.NumberOfActions) {
				int lastAnimationIndex = act.NumberOfActions / 8 - 1;
				int baseAction = selectedAction % 8;

				if (lastAnimationIndex < 0) {
					selectedAction = baseAction;
				}
				else {
					selectedAction = lastAnimationIndex * 8 + baseAction;
				}

				// Fallback
				if (selectedAction >= act.NumberOfActions) {
					selectedAction = act.NumberOfActions - 1;
				}
			}

			return selectedAction;
		}

		public static void DoThread(IActIndexSelector selector, IFrameRendererEditor editor, ISoundPlayer soundPlayer = null) {
			bool replay = true;
			int replayLoopCounter = 0;

			while (replay) {
				replay = false;

				var act = editor.Act;

				if (act == null) {
					selector.Stop();
					selector.OnAnimationPlaying(AnimationState.Stopped);
					return;
				}

				int selectedAction = SafeActionIndex(act, selector.SelectedAction);

				if (act[selectedAction].NumberOfFrames <= 1) {
					selector.Stop();
					selector.OnAnimationPlaying(AnimationState.Stopped);
					return;
				}

				if (act[selectedAction].AnimationSpeed < MaxAnimationSpeed) {
					selector.Stop();
					selector.OnAnimationPlaying(AnimationState.Stopped);
					ErrorHandler.HandleException("The animation speed is too fast and might cause issues. The animation will not be displayed.", ErrorLevel.NotSpecified);
					return;
				}

				var watch = new Stopwatch();
				int startFrame = selector.SelectedFrame;
				int frameInterval = FrameRendererConfiguration.FrameInterval;
				int oldInterval = int.MinValue;
				long idx = startFrame;

				try {
					selector.OnAnimationPlaying(AnimationState.StartThread);

					while (selector.IsPlaying) {
						if (act != editor.Act) {
							act = editor.Act;

							if (act == null)
								return;
						}

						try {
							selectedAction = SafeActionIndex(act, selector.SelectedAction);

							var interval = (int)(act[selectedAction].AnimationSpeed * frameInterval);

							if (oldInterval != interval) {
								oldInterval = interval;
								watch.Restart();
								idx = startFrame = selector.SelectedFrame;
							}

							if (act[selectedAction].AnimationSpeed < MaxAnimationSpeed) {
								selector.Stop();
								ErrorHandler.HandleException("The animation speed is too fast and might cause issues. The animation will not be displayed.", ErrorLevel.NotSpecified);
								return;
							}

							selector.SelectedFrame++;

							if (soundPlayer != null)
								soundPlayer.PlaySound(act.TryGetSoundFile(selectedAction, selector.SelectedFrame));

							if (!selector.IsPlaying)
								return;

							long expectedNextFrame = (idx + 1 - startFrame) * interval - watch.ElapsedMilliseconds;
							idx++;

							Thread.Sleep((int)Math.Max(20, Math.Min(interval, expectedNextFrame)));
							replay = false;

							// Reset timer if we're skipping frames
							if (expectedNextFrame < 0)
								oldInterval = -1;
						}
						catch {
							if (replayLoopCounter < 10)
								replay = true;

							replayLoopCounter++;
							break;
						}
					}
				}
				catch (Exception err) {
					selector.Stop();
					selector.OnAnimationPlaying(AnimationState.Stopped);
					ErrorHandler.HandleException(err);
				}
				finally {
					if (!replay) {
						selector.Stop();
						selector.OnAnimationPlaying(AnimationState.Stopped);
					}
				}
			}
		}
	}
}
