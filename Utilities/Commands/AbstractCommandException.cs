using System;

namespace Utilities.Commands {
	public class AbstractCommandException : Exception {
		public AbstractCommandArg Argument { get; private set; }
		public AbstractCommandException(AbstractCommandArg arg) : base("") {
			Argument = arg;
		}
	}

	public class CancelAbstractCommand : AbstractCommandException {
		public CancelAbstractCommand(AbstractCommandArg arg)
			: base(arg) {
		}
	}

	public class AbstractCommandArg {
		public bool Cancel { get; set; }
	}
}