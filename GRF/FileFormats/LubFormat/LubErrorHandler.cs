using System.Collections.Generic;
using GRF.GrfSystem;
using Utilities.CommandLine;

namespace GRF.FileFormats.LubFormat {
	public enum LubSourceError {
		CodeReconstructor,
		CodeDecompiler,
		LubReader,
		LubGeneric
	}

	public class LubErrorHandler {
		public const string Header = "GRF Editor Decompiler : ";
		public const string HeaderShort = "GRF Editor Decompiler ";
		private static readonly List<IErrorListener> _listeners = new List<IErrorListener>();

		public static void AddListener(IErrorListener listener) {
			_listeners.Add(listener);
		}

		public static void Handle(string exception, LubSourceError source) {
			exception = HeaderShort + "(" + source + ") : " + exception;

			if (_listeners.Count == 0)
				CLHelper.Error = exception;
			else
				_listeners.ForEach(p => p.Handle(exception));
		}
	}
}