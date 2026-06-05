using System;
using System.Drawing;

namespace Utilities {
	public static class ConfigRegistry {
		public static class Cache<T> {
			public static Func<T, string> Serializer { get; set; }
			public static Func<string, T> Deserializer { get; set; }
		}

		public static void Register<T>(Func<T, string> serializer, Func<string, T> deserializer) {
			Cache<T>.Serializer = serializer;
			Cache<T>.Deserializer = deserializer;
		}

		public static string Serialize<T>(T value) => Cache<T>.Serializer(value);
		public static T Deserialize<T>(string source) => Cache<T>.Deserializer(source);
	}

	public static class ConfigAskerSerializer {
		static ConfigAskerSerializer() {
			// Adds default converters
			Add<int>(v => v.ToString(), v => Int32.Parse(v));
			Add<string>(v => v, v => v);
			Add<bool>(v => v.ToString(), v => Boolean.Parse(v));
			Add<float>(v => v.ToString(), v => FormatConverters.SingleConverter(v));
			Add<double>(v => v.ToString(), v => FormatConverters.DoubleConverter(v));
			Add<Color>(v => String.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", v.A, v.R, v.G, v.B), v => {
				v = v.Replace("0x", "").Replace("#", "");

				if (v.Length == 6) {
					return Color.FromArgb(
						255,
						Convert.ToByte(v.Substring(0, 2), 16),
						Convert.ToByte(v.Substring(2, 2), 16),
						Convert.ToByte(v.Substring(4, 2), 16));
				}
				else if (v.Length == 8) {
					return Color.FromArgb(
						Convert.ToByte(v.Substring(0, 2), 16),
						Convert.ToByte(v.Substring(2, 2), 16),
						Convert.ToByte(v.Substring(4, 2), 16),
						Convert.ToByte(v.Substring(6, 2), 16));
				}
				else {
					throw new ArgumentException("Invalid format for the color. Expected a hex number: '0xAAFF00BB' or '#AAFF00BB'.", "color");
				}
			});
		}

		public static void Add<T>(Func<T, string> set, Func<string, T> get) {
			ConfigRegistry.Register<T>(set, get);
		}
	}
}
