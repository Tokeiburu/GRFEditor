using ErrorManager;

namespace GRF.GrfSystem {
	public interface IErrorListener {
		void Handle(string exception);
		void Handle(string exception, ErrorLevel errorLevel);
	}
}
