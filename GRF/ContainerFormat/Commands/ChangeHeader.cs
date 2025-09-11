using GRF.Core;

namespace GRF.ContainerFormat.Commands {
	internal class ChangeHeader<TEntry> : IContainerCommand<TEntry> where TEntry : ContainerEntry {
		private readonly CCallbacks.ChangeHeaderCallback _callback;

		private readonly string _header;
		private string _oldHeader;

		public ChangeHeader(string header, CCallbacks.ChangeHeaderCallback callback) {
			_header = header;
			_callback = callback;
		}

		#region IContainerCommand<TEntry> Members

		public void Execute(ContainerAbstract<TEntry> container) {
			_oldHeader = container.Header.Magic;

			container.Header.Magic = _header;

			if (_callback != null)
				_callback(container.Header.Magic, true);
		}

		public void Undo(ContainerAbstract<TEntry> container) {
			container.Header.Magic = _oldHeader;

			if (_callback != null)
				_callback(container.Header.Magic, false);
		}

		public string CommandDescription {
			get {
				return string.Format(GrfStrings.ChangeHeader, _oldHeader, _header);
			}
		}

		#endregion
	}
}