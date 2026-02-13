using System.Windows.Controls;
using System.Windows.Media;

namespace TokeiLibrary.WPF {
	public class XImage : Image {
		public XImage() {
			this.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
		}
	}
}
