using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;

namespace GRF.ContainerFormat {
	public class ContainerValidation {
		private readonly List<Exception> _exceptions = new List<Exception>();

		public bool IsValid {
			get { return _exceptions.Count == 0; }
		}

		public string Message {
			get { return Methods.Aggregate(_exceptions.Select(p => p.Message).ToList(), Environment.NewLine); }
		}

		public void Add(string message) {
			_exceptions.Add(new Exception(message));
		}

		public void Add(Exception err) {
			_exceptions.Add(err);
		}

		public void Clear() {
			_exceptions.Clear();
		}

		public string GetLastMessage() {
			if (_exceptions.Count == 0) return null;
			return _exceptions.Last().Message;
		}

		public Exception GetLastException() {
			if (_exceptions.Count == 0) return null;
			return _exceptions.Last();
		}

		public override string ToString() {
			if (_exceptions.Count == 0) return GrfStrings.NoExceptions;
			return _exceptions.Last().Message;
		}

		public static implicit operator string(ContainerValidation item) {
			return item.ToString();
		}

		public void Add(ContainerValidation validation) {
			_exceptions.AddRange(validation._exceptions);
		}
	}
}