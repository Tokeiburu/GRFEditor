using System.Collections.Generic;
using Utilities.Commands;

namespace Database.Commands {
	/// <summary>
	/// Used to apply commands on a table.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the tuple value.</typeparam>
	public class CommandsHolder<TKey, TValue> : AbstractCommand<ITableCommand<TKey, TValue>> where TValue : Tuple {
		private readonly Table<TKey, TValue> _table;

		public CommandsHolder(Table<TKey, TValue> table) {
			_table = table;
		}

		public void Begin() {
			BeginEdit(new GroupCommand<TKey, TValue>());
		}

		public void BeginNoDelay() {
			BeginEdit(new GroupCommand<TKey, TValue>(true, _table));
		}

		public void BeginNoDelay(CCallbacks.GroupTupleCallback callback) {
			BeginEdit(new GroupCommand<TKey, TValue>(true, _table, callback));
		}

		public void AddGroupedCommands(List<ITableCommand<TKey, TValue>> commands) {
			if (commands.Count == 0) return;
			AddGroupedCommands(commands, null);
		}

		public void AddGroupedCommands(List<ITableCommand<TKey, TValue>> commands, CCallbacks.GroupTupleCallback callback) {
			if (commands.Count == 0) return;
			StoreAndExecute(new GroupCommand<TKey, TValue>(commands, callback));
		}

		public void ExecuteAction(TKey key, CCallbacks.GenericCallback<TKey> callback) {
			ExecuteAction(key, callback, "Generic command");
		}

		public void ExecuteAction(TKey key, CCallbacks.GenericCallback<TKey> callback, string display) {
			DatabaseExceptions.IfNullThrow(callback, "callback");
			StoreAndExecute(new GenericCommand<TKey, TValue>(key, display, callback));
		}

		public void AddTuple(TKey key, TValue tuple, bool isCombinable) {
			AddTuple(key, tuple, isCombinable, false, null);
		}

		public virtual void AddTuple(TKey key, TValue tuple, bool isCombinable, bool autoIncrement, CCallbacks.GenericTupleCallback<TKey> callback) {
			StoreAndExecute(new AddTuple<TKey, TValue>(key, tuple, autoIncrement, callback) { IsCombinable = isCombinable });
		}

		public void AddTuple(TKey key, TValue tuple) {
			AddTuple(key, tuple, null);
		}

		public virtual void AddTuple(TKey key, TValue tuple, CCallbacks.GenericTupleCallback<TKey> callback) {
			StoreAndExecute(new AddTuple<TKey, TValue>(key, tuple, false, callback));
		}

		public virtual void SetDico(TValue tupleParent, DbAttribute attributeTable, TValue tuple, DbAttribute attribute, object value, bool reversable = true) {
			Dictionary<TKey, TValue> dico = (Dictionary<TKey, TValue>)tupleParent.GetRawValue(attributeTable.Index);

			if (dico == null) return;

			var dicoEntry = dico[tuple.GetKey<TKey>()];

			if (dicoEntry.GetValue(attribute).ToString() == value.ToString()) return;
			StoreAndExecute(new ChangeTupleDicoProperty2<TKey, TValue>(tupleParent, attributeTable, tuple, attribute, value) { Reversable = reversable });
		}

		public virtual void SetDico(TValue tupleParent, List<TValue> tuples, DbAttribute attribute, object value) {
			StoreAndExecute(new GroupChangeTupleProperty<TKey, TValue>(tupleParent, tuples, attribute, value, null));
		}

		public virtual void DeleteDico(TValue tupleParent, DbAttribute attributeTable, TKey key) {
			StoreAndExecute(new DeleteTupleDico<TKey, TValue>(tupleParent, attributeTable, key));
		}

		public virtual void ChangeKeyDico(TValue tupleParent, DbAttribute attributeTable, TKey oldKey, TKey newKey, CCallbacks.KeyTupleCallback<TKey> callback) {
			if (oldKey.ToString() == newKey.ToString()) return;
			StoreAndExecute(new ChangeTupleKeyDico<TKey, TValue>(tupleParent, attributeTable, oldKey, newKey));
		}

		public virtual void AddTupleDico(TValue tupleParent, DbAttribute attributeTable, TKey key, TValue tuple, CCallbacks.AddTupleDicoCallback<TKey, TValue> callback = null) {
			StoreAndExecute(new AddTupleDico<TKey, TValue>(tupleParent, attributeTable, key, tuple, callback));
		}

		public virtual void Set(TValue tuple, DbAttribute attribute, object value) {
			if (tuple.GetValue(attribute) == value) return;
			StoreAndExecute(new ChangeTupleProperty<TKey, TValue>(tuple, attribute, value));
		}

		public virtual void Set(TValue tuple, DbAttribute attribute, object value, bool reversable) {
			if (tuple.GetValue(attribute) == value) return;
			StoreAndExecute(new ChangeTupleProperty<TKey, TValue>(tuple, attribute, value) { Reversable = reversable });
		}

		public virtual void Set(TValue tuple, int attributeId, object value) {
			if (tuple.GetValue(attributeId) == value) return;
			StoreAndExecute(new ChangeTupleProperty<TKey, TValue>(tuple, attributeId, value));
		}

		public virtual void Set(TKey key, TValue tuple, DbAttribute attribute, object value) {
			if (tuple.GetValue(attribute) == value) return;
			StoreAndExecute(new ChangeTupleProperty<TKey, TValue>(key, tuple, attribute, value));
		}

		public virtual void Set(List<TValue> tuples, DbAttribute attribute, object value) {
			StoreAndExecute(new GroupChangeTupleProperty<TKey, TValue>(tuples, attribute, value, null));
		}

		public virtual void ChangeKey(TKey oldKey, TKey newKey) {
			ChangeKey(oldKey, newKey, null);
		}

		public virtual void ChangeKey(TValue oldValue, TValue newValue) {
			ChangeKey(oldValue.GetKey<TKey>(), newValue.GetKey<TKey>(), null);
		}

		public virtual void ChangeKey(TKey oldKey, TKey newKey, CCallbacks.KeyTupleCallback<TKey> callback) {
			if (oldKey.ToString() == newKey.ToString()) return;
			StoreAndExecute(new ChangeTupleKey<TKey, TValue>(oldKey, newKey, callback));
		}

		public virtual void ChangeKey(TValue oldValue, TValue newValue, CCallbacks.KeyTupleCallback<TKey> callback) {
			ChangeKey(oldValue.GetKey<TKey>(), newValue.GetKey<TKey>(), callback);
		}

		public virtual void Delete(TKey key) {
			StoreAndExecute(new DeleteTuple<TKey, TValue>(key));
		}

		public virtual void CopyTupleTo(TKey oldkey, TKey newKey) {
			CopyTupleTo(oldkey, newKey, null);
		}

		public virtual void CopyTupleTo(TKey oldkey, TKey newKey, CCallbacks.KeyTupleCallback<TKey> callback) {
			StoreAndExecute(new CopyTupleTo<TKey, TValue>(oldkey, newKey, callback));
		}

		public virtual void CopyTupleTo(Table<TKey, TValue> tableSource, TKey oldkey, TKey newKey) {
			CopyTupleTo(tableSource, oldkey, newKey, null);
		}

		public virtual void CopyTupleTo(Table<TKey, TValue> tableSource, TKey oldkey, TKey newKey, CCallbacks.CopyToTupleCallback<TKey, TValue> callback) {
			StoreAndExecute(new CopyTupleToAdv<TKey, TValue>(tableSource, oldkey, _table, newKey, callback));
		}

		protected override void _execute(ITableCommand<TKey, TValue> command) {
			command.Execute(_table);
		}

		protected override void _undo(ITableCommand<TKey, TValue> command) {
			command.Undo(_table);
		}

		protected override void _redo(ITableCommand<TKey, TValue> command) {
			command.Execute(_table);
		}

		public void End() {
			EndEdit();
		}
	}
}
