using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Utilities {
	public static class ReflectionOptimizer {
		// Caches delegates by a unique string key: "FullTypeName.FieldName"
		public static Func<object, TFieldValue> CreateGetter<TFieldValue>(Type modelType, string fieldName) {
			FieldInfo fi = modelType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (fi == null) throw new ArgumentException($"Field '{fieldName}' not found on {modelType.Name}");

			var instanceParam = System.Linq.Expressions.Expression.Parameter(typeof(object), "model");
			var castInstance = System.Linq.Expressions.Expression.Convert(instanceParam, modelType);
			var fieldAccess = System.Linq.Expressions.Expression.Field(castInstance, fi);

			// Handle conversion if the underlying field is not a string (optional safety)
			var castResult = System.Linq.Expressions.Expression.Convert(fieldAccess, typeof(TFieldValue));

			return System.Linq.Expressions.Expression.Lambda<Func<object, TFieldValue>>(castResult, instanceParam).Compile();
		}

		public static Action<object, TFieldValue> CreateSetter<TFieldValue>(Type modelType, string fieldName) {
			FieldInfo fi = modelType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (fi == null) throw new ArgumentException($"Field '{fieldName}' not found on {modelType.Name}");

			var instanceParam = System.Linq.Expressions.Expression.Parameter(typeof(object), "model");
			var valueParam = System.Linq.Expressions.Expression.Parameter(typeof(TFieldValue), "value");

			var castInstance = System.Linq.Expressions.Expression.Convert(instanceParam, modelType);
			var fieldAccess = System.Linq.Expressions.Expression.Field(castInstance, fi);

			// Assign the value to the field: model.Field = value
			var assignment = System.Linq.Expressions.Expression.Assign(fieldAccess, valueParam);

			return System.Linq.Expressions.Expression.Lambda<Action<object, TFieldValue>>(assignment, instanceParam, valueParam).Compile();
		}
	}
}
