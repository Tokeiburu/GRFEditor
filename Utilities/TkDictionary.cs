﻿using System.Collections.Generic;

namespace Utilities {
	/// <summary>
	/// Automatically adds default values when requesting an element.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	public class TkDictionary<TKey, TValue> : Dictionary<TKey, TValue> {
		public TkDictionary() {
		}

		public TkDictionary(IEqualityComparer<TKey> comparer)
			: base(comparer) {
		}

		public new TValue this[TKey key] {
			get {
				if (TryGetValue(key, out TValue t))
					return t;
				t = default;
				Add(key, t);
				return t;
			}
			set {
				base[key] = value;
			}
		}
	}
}
