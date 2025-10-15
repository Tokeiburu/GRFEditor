using System;
using System.Globalization;

namespace Utilities {
	public static class FormatConverters {
		public static int IntConverter(string arg) {
			return Int32.Parse(arg);
		}

		public static string StringConverter(string arg) {
			return arg;
		}

		public static float SingleConverter(string arg) {
			return float.Parse(arg.Replace(",", "."), CultureInfo.InvariantCulture);
		}

		public static float SingleConverterNoThrow(string arg) {
			float value;

			if (float.TryParse(arg.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out value)) {
				return value;
			}

			return 0;
		}

		public static double DoubleConverter(string arg) {
			return double.Parse(arg.Replace(",", "."), CultureInfo.InvariantCulture);
		}

		public static double DoubleConverterNoThrow(string arg) {
			double value;

			if (double.TryParse(arg.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out value)) {
				return value;
			}

			return 0;
		}

		public static int IntOrHexConverter(string text) {
			int value;

			if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && text.Length > 2) {
				value = Convert.ToInt32(text, 16);
			}
			else {
				Int32.TryParse(text, out value);
			}

			return value;
		}

		public static long LongOrHexConverter(string text) {
			long value;

			if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && text.Length > 2) {
				value = Convert.ToInt64(text, 16);
			}
			else {
				Int64.TryParse(text, out value);
			}

			return value;
		}

		public static bool BooleanConverter(string arg) {
			return Boolean.Parse(arg);
		}

		public static T DefConverter<T>(string arg, Func<string, T> specConverter) {
			try {
				return specConverter(arg);
			}
			catch {
				return default;
			}
		}

		public static T DefConverter<T>(string arg, Func<string, T> specConverter, T def) {
			try {
				return specConverter(arg);
			}
			catch {
				return def;
			}
		}
	}
}
