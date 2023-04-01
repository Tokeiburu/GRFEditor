using System.Collections.Generic;
using System.Linq;
using GRF.ContainerFormat;

namespace GRF.Image {
	/// <summary>
	/// This class is used to convert GrfImage objects to another type.
	/// With a GrfImage, use the method image.Cast&lt;YourType&gt;();
	/// </summary>
	public static class ImageConverterManager {
		/// <summary>
		/// Gets a value indicating whether at least one image converter has been set.
		/// </summary>
		public static bool IsSet {
			get { return _converters.Count > 0; }
		}

		private static readonly List<AbstractImageConverter> _converters = new List<AbstractImageConverter>();

		/// <summary>
		/// Registers an image converter.
		/// </summary>
		/// <param name="converter">The converter.</param>
		public static void AddConverter(AbstractImageConverter converter) {
			_converters.Add(converter);
		}

		internal static T Convert<T>(GrfImage image) where T : class {
			string type = typeof (T).ToString();

			foreach (AbstractImageConverter converter in _converters) {
				if (converter.ReturnTypes.Any(p => p.ToString() == type)) {
					return converter.Convert(image) as T;
				}
			}

			throw GrfExceptions.__NoImageConverter.Create();
		}

		internal static GrfImage Self<T>(GrfImage image) where T : class {
			string type = typeof(T).ToString();

			foreach (AbstractImageConverter converter in _converters) {
				if (converter.ReturnTypes.Any(p => p.ToString() == type)) {
					return converter.ConvertToSelf(image);
				}
			}

			throw GrfExceptions.__NoImageConverter.Create();
		}

		internal static GrfImage SelfAny(GrfImage image) {
			if (_converters.Count > 0) {
				return _converters[0].ConvertToSelf(image);
			}

			throw GrfExceptions.__NoImageConverter.Create();
		}
	}
}
