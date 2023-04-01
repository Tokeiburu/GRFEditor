using System.Windows.Media;

namespace TokeiLibrary.WPF.Styles {
	public static class FrozenBrush {
		public static Brush Freeze(Brush brush) {
			brush.Freeze();
			return brush;
		}
	}
}
