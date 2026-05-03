using System.Windows.Controls;
using ActEditor.Core.WPF.EditorControls.ActSelectorComponents;
using GRF.FileFormats.ActFormat;

namespace GrfToWpfBridge.ActRenderer {
	/// <summary>
	/// Interface for a frame editor, includes all related components.
	/// </summary>
	public interface IFrameRendererEditor {
		Act Act { get; }
		int SelectedAction { get; }
		int SelectedFrame { get; }
		IActIndexSelector IndexSelector { get; }
		event FrameRendererEventDelegates.ActEditorEventDelegate ActLoaded;
		FrameRenderer FrameRenderer { get; }
		Grid GridPrimary { get; }
		Canvas Canvas { get; }
	}

	public interface IActIndexSelector {
		bool IsPlaying { get; }
		event FrameRendererEventDelegates.IndexChangedDelegate ActionChanged;
		event FrameRendererEventDelegates.IndexChangedDelegate FrameChanged;
		event FrameRendererEventDelegates.IndexChangedDelegate SpecialFrameChanged;
		event FrameRendererEventDelegates.AnimationStateEventHandler AnimationPlaying;
		void OnFrameChanged(int frameIndex);
		void OnActionChanged(int actionIndex);
		void OnAnimationPlaying(AnimationState state);
		int SelectedAction { get; set; }
		int SelectedFrame { get; set; }
		void Play();
		void Stop();
		void Init(IFrameRendererEditor editor, int actionIndex, int selectedAction);
	}

	public static class FrameRendererEventDelegates {
		public delegate void ActEditorEventDelegate(object sender);
		public delegate void IndexChangedDelegate(int index);
		public delegate void AnimationStateEventHandler(AnimationState index);
	}
}