using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Utilities {
	public static class Compiled {
		public static class New<T> where T : new() {
			public static readonly Func<T> Instance = Expression.Lambda<Func<T>> (Expression.New(typeof (T))).Compile();
		}

		public static class New2<T> {
			public static readonly Func<T> Instance = _creator();

			static Func<T> _creator() {
				Type t = typeof(T);
				if (t == typeof(string))
					return Expression.Lambda<Func<T>>(Expression.Constant(string.Empty)).Compile();

				if (t.HasDefaultConstructor())
					return Expression.Lambda<Func<T>>(Expression.New(t)).Compile();

				return () => (T)FormatterServices.GetUninitializedObject(t);
			}
		}

		public static bool HasDefaultConstructor(this Type t) {
			return t.IsValueType || t.GetConstructor(Type.EmptyTypes) != null;
		}
	}
}
