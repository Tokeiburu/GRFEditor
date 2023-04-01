using System.Windows;

namespace TokeiLibrary.WPF {
	public static class DaDExtensions {
		public static T Get<T>(this DragEventArgs e, string format) where T : class {
			if (format == DataFormats.FileDrop) {
				if (typeof(T) == typeof(string))
					return (T) (object) (e.Data.GetData(format, true) as string[])[0];
			}

			return e.Data.GetData(format, true) as T;
		}

		public static bool Is(this DragEventArgs e, string format) {
			var res = e.Data.GetDataPresent(format, true);

			if (res && format == DataFormats.FileDrop) {
				var r = e.Data.GetData(format, true) as string[];

				if (r != null && r.Length > 0)
					return true;

				return false;
			}

			return res;
		}
	}
}
