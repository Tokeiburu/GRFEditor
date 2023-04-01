using System;
using System.Linq;
using Utilities.Extension;

namespace Database {
	public static class DatabaseExceptions {
		// ReSharper disable InconsistentNaming
		public static readonly FormattedExceptionMessage __NullPathException = "Path cannot be null or empty.\r\n{0}";
		public static readonly FormattedExceptionMessage __TraceNotEnabled = "Tuple tracing is required for this operation.";
		public static readonly FormattedExceptionMessage __KeyConstraint = "Cannot set the key to '{0}' because the table's key type is <{1}>.";
		public static readonly FormattedExceptionMessage __ArgumentNullValue = "The value cannot be null for the argument '{0}'.";
		public static readonly FormattedExceptionMessage __AttributeNotFound = "Attribute couldn't be found '{0}', known attributes are : \r\n{1}.";

		internal static DatabaseException GetException(FormattedExceptionMessage exception, params object[] items) {
			return new DatabaseException(exception, String.Format(exception.Message, items));
		}

		public static void ThrowIfTraceNotEnabled() {
			if (!TableHelper.EnableTupleTrace)
				throw GetException(__TraceNotEnabled);
		}

		public static void ThrowKeyConstraint<T>(object value) {
			throw GetException(__KeyConstraint, value, typeof (T));
		}

		internal static void IfNullThrow(object value, string name) {
			if (value == null)
				throw GetException(__ArgumentNullValue, name);
		}

		internal static void ThrowAttributeNotFound(object attributeKey, AttributeList attributes) {
			throw GetException(__AttributeNotFound, attributeKey, string.Join(", ", attributes.Attributes.Select(p => p.GetQueryName()).ToArray()));
		}
	}

	public class FormattedExceptionMessage {
		private static int _errorId;

		private readonly int _id;
		private readonly string _message;

		public FormattedExceptionMessage(string message) {
			_message = message;
			_errorId++;
			_id = _errorId;
		}

		public string Message {
			get { return _message; }
		}

		protected bool Equals(FormattedExceptionMessage other) {
			return _id == other._id;
		}

		public override int GetHashCode() {
			return _id;
		}

		public string Display(params object[] args) {
			return String.Format(Message, args);
		}

		public static implicit operator FormattedExceptionMessage(string message) {
			return new FormattedExceptionMessage(message);
		}

		public static implicit operator string(FormattedExceptionMessage message) {
			return message.Message;
		}

		public override bool Equals(object obj) {
			var formattedExceptionMessage = obj as FormattedExceptionMessage;
			if (formattedExceptionMessage != null)
				return Equals(formattedExceptionMessage);
			return false;
		}

		public DatabaseException Throw(params object[] items) {
			return DatabaseExceptions.GetException(this, items);
		}
	}

	public class DatabaseException : Exception {
		private readonly FormattedExceptionMessage _format;

		public DatabaseException(FormattedExceptionMessage format, string message) : base(message) {
			_format = format;
		}

		public FormattedExceptionMessage Format {
			get { return _format; }
		}

		protected bool Equals(FormattedExceptionMessage other) {
			return Equals(_format, other);
		}

		public override int GetHashCode() {
			return (_format != null ? _format.GetHashCode() : 0);
		}

		public override bool Equals(object obj) {
			if (obj is DatabaseException) {
				return Equals((obj as DatabaseException).Format);
			}

			if (obj is FormattedExceptionMessage) {
				return Equals(obj as FormattedExceptionMessage);
			}

			return false;
		}

		public static bool operator ==(DatabaseException exp1, DatabaseException exp2) {
			if (ReferenceEquals(exp1, null) && ReferenceEquals(exp2, null)) return true;
			if (ReferenceEquals(exp1, null) || ReferenceEquals(exp2, null)) return false;

			return exp1.Equals(exp2);
		}

		public static bool operator !=(DatabaseException exp1, DatabaseException exp2) {
			return !(exp1 == exp2);
		}

		public static bool operator ==(DatabaseException exp1, FormattedExceptionMessage exp2) {
			if (ReferenceEquals(exp1, null) && ReferenceEquals(exp2, null)) return true;
			if (ReferenceEquals(exp1, null) || ReferenceEquals(exp2, null)) return false;

			return exp1.Equals(exp2);
		}

		public static bool operator !=(DatabaseException exp1, FormattedExceptionMessage exp2) {
			return !(exp1 == exp2);
		}
	}
}
