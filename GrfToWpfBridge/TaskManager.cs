using System;
using GRF.Threading;

namespace GrfToWpfBridge {
	public static class TaskManager {
		public static void DisplayTask(string title, string description, IProgress progress) {
		}

		public static void DisplayTask(string title, string description, Func<int> func, int count, Action task) {
			TaskDialog dialog = new TaskDialog(title, "document.ico", description);
			dialog.Start(task, () => func() / (float) count * 100f);
			dialog.ShowDialog();
		}

		public static void DisplayTaskC(string title, string description, Func<int> func, int count, Action<Func<bool>> task) {
			TaskDialog dialog = new TaskDialog(title, "document.ico", description);
			dialog.Start(task, () => func() / (float) count * 100f);
			dialog.ShowDialog();
		}

		public static void DisplayTask(string title, string description, Func<float> progress, Action task) {
			TaskDialog dialog = new TaskDialog(title, "document.ico", description);
			dialog.Start(task, progress);
			dialog.ShowDialog();
		}

		public static void DisplayTaskC(string title, string description, Func<float> progress, Action<Func<bool>> task) {
			TaskDialog dialog = new TaskDialog(title, "document.ico", description);
			dialog.Start(task, progress);
			dialog.ShowDialog();
		}
	}
}