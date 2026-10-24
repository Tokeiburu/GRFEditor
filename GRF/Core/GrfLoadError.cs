using ErrorManager;

namespace GRF.Core {
	public class GrfLoadError {
		public ErrorLevel Severity = ErrorLevel.Critical;
		public string Message;

		internal GrfLoadError(string message, ErrorLevel severity = ErrorLevel.Critical) {
			Message = message;
			Severity = severity;
		}

		public override string ToString() {
			return Message;
		}
	}
}
