using System.Collections.Generic;

namespace Database.Commands {
	public static class CCallbacks {
		public delegate void GenericTupleCallback<TKey>(TKey oldkey, TKey newKey, bool executed);
		public delegate void CopyToTupleCallback<TKey, TValue>(Table<TKey, TValue> tableSource, TKey oldkey, Table<TKey, TValue> tableDest, TKey newKey, bool executed) where TValue : Tuple;
		public delegate void GroupChangeTupleCallback<TValue>(List<TValue> values, DbAttribute attribute, List<object> oldValue, List<object> newValue, bool executed);
		public delegate void KeyTupleCallback<TKey>(TKey oldkey, TKey newKey, bool executed);
		public delegate void GenericCallback<TKey>(TKey key, bool executed);
		public delegate void AddTupleDicoCallback<TKey, TValue>(TValue tupleParent, TKey dkey, TValue dvalue, bool executed);
		public delegate void GroupTupleCallback(bool executed);
	}
}
