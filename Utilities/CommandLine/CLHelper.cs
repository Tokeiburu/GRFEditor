using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utilities.CommandLine {
	public static class CLHelper {
		private static bool _logState = true;

		public static string Log {
			set { if (_logState) Console.WriteLine("#Log : " + value); }
		}

		public static string Warning {
			set { Console.WriteLine("#Warning : " + value); }
		}

		public static string Error {
			set { Console.WriteLine("#Error : " + value); }
		}

		public static string Exception {
			set { Console.WriteLine("#Exception : " + value); }
		}

		public static float Progress {
			set {
				Console.SetCursorPosition(0, Console.CursorTop);
				Console.Write("Progress : " + String.Format("{0:0.0} %   ", value < 0 ? 0 : value));
			}
		}

		public static string StringProgress {
			set {
				Console.SetCursorPosition(0, Console.CursorTop);
				Console.Write("{0,-" + (Console.WindowWidth - 1) + "}", "Progress : " + value);
			}
		}

		public static string WriteLine {
			set { Console.WriteLine(value); }
		}

		public static string WAppend {
			set {
				Console.Write(_parse(value));
			}
		}

		private static string _parse(string val) {
			if (val.Contains("_CP")) {
				val = val.Replace("_CP", CStart());
			}

			if (val.Contains("_CS")) {
				val = val.Replace("_CS", CStop());
			}

			if (val.Contains("_CD")) {
				val = val.Replace("_CD", CDisplay());
			}

			return val;
		}

		public static string WL {
			set {
				Console.WriteLine(_parse(value));
			}
		}

		private static int _timers;

		public static string CStart() {
			Z.Start(++_timers);
			return "";
		}

		public static string CStart(int opId) {
			Z.Start(opId);
			return "";
		}

		public static string CStop() {
			Z.Stop(_timers);
			return "";
		}

		public static string CStop(int opId) {
			Z.Stop(opId);
			return "";
		}

		public static string CReset(int opId) {
			Z.Stop(opId);
			Z.Delete(opId);
			return "";
		}

		public static string CStopAndDisplay(int opId) {
			CStop(opId);
			return CDisplay(opId);
		}

		public static void CStopAndDisplay_(int opId) {
			CStop(opId);
			WL = "#" + opId + " " + CDisplay(opId) + " ms; ";
		}

		public static void CStopAndDisplay(string method, int opId) {
			if (Z.CounterExists(opId)) {
				CStop(opId);
				WL = method + " " + CDisplay(opId) + " ms; ";
			}
		}

		public static string CResume(int opId) {
			Z.Start(opId);
			return "";
		}

		public static string CDisplay() {
			return Z.GetCounterDisplay(_timers);
		}

		public static string CDisplay(int opId) {
			return Z.GetCounterDisplay(opId);
		}

		public static void ProgressEnded() {
			Console.SetCursorPosition(0, Console.CursorTop);
			Console.Write("#Finished         ");
			Console.WriteLine();
		}

		public static string Indent(string help, int indentLength, bool ignoreFirst = false, bool ignoreCutWords = false) {
			bool originalValue = ignoreFirst;
			string indent = Fill(' ', indentLength);
			help = help.Replace("\r\n", " ").Replace("\n", " ").Replace("  ", " ");

			List<string> lines = new List<string>();
			int maxLineLength = Console.WindowWidth - indentLength;
			int position = 0;
			while (position < help.Length) {
				string line = help.Substring(position, maxLineLength + position > help.Length ? help.Length - position : maxLineLength);
				if (line.Length == maxLineLength && !line.EndsWith(" ")) {
					// We cut a word in half
					if (!ignoreCutWords) {
						int lastIndexOfSpace = line.LastIndexOf(' ');
						if (lastIndexOfSpace >= -1) {
							line = line.Substring(0, lastIndexOfSpace);
							if (line == "")
								return Indent(help, indentLength, originalValue, true);

							position += line.Length;
							line = line.TrimStart(' ');
							line = line + Fill(' ', Console.WindowWidth - indent.Length - line.Length);
							lines.Add((ignoreFirst ? "" : indent) + line);
							ignoreFirst = false;
							continue;
						}
					}
				}

				line = line.TrimStart(' ');
				line = line + Fill(' ', Console.WindowWidth - indent.Length - line.Length);
				lines.Add((ignoreFirst ? "" : indent) + line);
				ignoreFirst = false;
				position += maxLineLength;
			}

			lines[lines.Count - 1] = lines.Last().TrimEnd(' ');
			return lines.Aggregate((current, line) => current + line);
		}

		public static string Fill(char c, int times) {
			if (times <= 0)
				return "";

			StringBuilder builder = new StringBuilder(times);
			for (int i = 0; i < times; i++)
				builder.Append(c);
			return builder.ToString();
		}

		public static void SetLogState(bool state) {
			_logState = state;
		}

		//public static void 
	}
}
