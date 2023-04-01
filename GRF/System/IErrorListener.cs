using ErrorManager;

namespace GRF.System {
	public interface IErrorListener {
		void Handle(string exception);
		void Handle(string exception, ErrorLevel errorLevel);
	}
}
