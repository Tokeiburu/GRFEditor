using GRF.Core;

namespace GRF.ContainerFormat.Commands {
	internal class ChangeVersion<TEntry> : IContainerCommand<TEntry> where TEntry : ContainerEntry {
		private readonly CCallbacks.ChangeVersionCallback _callback;

		private readonly byte _major;
		private readonly byte _minor;
		private byte _oldMajor;
		private byte _oldMinor;

		public ChangeVersion(byte major, byte minor, CCallbacks.ChangeVersionCallback callback) {
			_major = major;
			_minor = minor;
			_callback = callback;
		}

		#region IContainerCommand<TEntry> Members

		public void Execute(ContainerAbstract<TEntry> container) {
			_oldMajor = container.Header.MajorVersion;
			_oldMinor = container.Header.MinorVersion;

			((GrfHeader) container.Header).SetGrfVersion(_major, _minor);

			if (_callback != null)
				_callback(container.Header.MajorVersion, container.Header.MinorVersion, true);
		}

		public void Undo(ContainerAbstract<TEntry> container) {
			((GrfHeader) container.Header).SetGrfVersion(_oldMajor, _oldMinor);

			if (_callback != null)
				_callback(container.Header.MajorVersion, container.Header.MinorVersion, false);
		}

		public string CommandDescription {
			get {
				return string.Format(GrfStrings.ChangeVersion,
				                     string.Format("0x{0:X}", (_oldMajor << 8) + _oldMinor),
				                     string.Format("0x{0:X}", (_major << 8) + _minor));
			}
		}

		#endregion
	}
}