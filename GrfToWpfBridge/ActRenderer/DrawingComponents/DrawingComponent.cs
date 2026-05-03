using GrfToWpfBridge.ActRenderer;
using System.Windows.Controls;

namespace GrfToWpfBridge.DrawingComponents {
	/// <summary>
	/// The drawing component class is used to display items
	/// in the FrameRenderer.
	/// </summary>
	public abstract class DrawingComponent : Control {
		/// <summary>
		/// Renders the element in the IPreview object.
		/// </summary>
		/// <param name="renderer">The renderer.</param>
		public abstract void Render(FrameRenderer renderer);

		/// <summary>
		/// Renders only the essential parts without reloading the elements.
		/// </summary>
		/// <param name="renderer">The renderer.</param>
		public abstract void QuickRender(FrameRenderer renderer);

		/// <summary>
		/// Removes the element from the IPreview object.
		/// </summary>
		/// <param name="renderer">The renderer.</param>
		public abstract void Remove(FrameRenderer renderer);

		/// <summary>
		/// Unloads this instance.
		/// </summary>
		public virtual void Unload(FrameRenderer renderer) {
			Remove(renderer);
		}
	}
}