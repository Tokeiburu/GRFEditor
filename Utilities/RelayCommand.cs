using System;
using System.Windows.Input;

namespace Utilities {
	public class RelayCommand : ICommand {
		private readonly Action _execute;
		private readonly Func<bool> _canExecute;

		public RelayCommand(Action execute, Func<bool> canExecute = null) {
			_execute = execute ?? throw new ArgumentNullException(nameof(execute));
			_canExecute = canExecute;
		}

		public bool CanExecute(object parameter) => _canExecute == null || _canExecute();
		public void Execute(object parameter) => _execute();

		public event EventHandler CanExecuteChanged;
		public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
	}

	public class RelayCommand<T> : ICommand {
		private readonly Action<T> _execute;
		private readonly Func<bool> _canExecute;

		public RelayCommand(Action<T> execute, Func<bool> canExecute = null) {
			_execute = execute ?? throw new ArgumentNullException(nameof(execute));
			_canExecute = canExecute;
		}

		public bool CanExecute(object parameter) => _canExecute == null || _canExecute();
		public void Execute(object parameter) => _execute((T)parameter);

		public event EventHandler CanExecuteChanged;
		public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
	}
}
