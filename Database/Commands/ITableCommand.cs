namespace Database.Commands {
	public interface ITableCommand<TKey, TValue> 
		where TValue : Tuple {
		void Execute(Table<TKey, TValue> table);
		void Undo(Table<TKey, TValue> table);
		string CommandDescription { get; }
		TKey Key { get; }
	}
}
