using System;
using Utilities;

namespace GrfToWpfBridge {
	public class BufferedProperty<T> {
		private readonly ConfigAsker _ca;
		private readonly Func<string, T> _converter;
		private readonly T _def;
		private readonly string _prop;
		private bool _isSet;
		private T _value;

		public BufferedProperty(ConfigAsker ca, string prop, T def, Func<string, T> converter) {
			_ca = ca;
			_prop = prop;
			_def = def;
			_converter = converter;
		}

		public T Get() {
			if (_isSet)
				return _value;

			_isSet = true;
			_value = _converter(_ca[_prop, _def.ToString()]);
			return _value;
		}

		public void Set(T value) {
			_value = value;
			_isSet = true;
			_ca[_prop] = value.ToString();
		}
	}
}