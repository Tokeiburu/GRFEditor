using System.Collections.Generic;

namespace Database {
	public static class TableHelper {
		public delegate void TupleTraceEventHandler(object sender, bool state);

		public static event TupleTraceEventHandler TraceStatusChanged;

		public static void OnTraceStatusChanged(bool state) {
			TupleTraceEventHandler handler = TraceStatusChanged;
			if (handler != null) handler(null, state);
		}

		public static bool EnableTupleTrace {
			get { return _enableTupleTrace; }
			set {
				_enableTupleTrace = value;

				OnTraceStatusChanged(_enableTupleTrace);
			}
		}

		public static List<BaseTable> Tables = new List<BaseTable>();
		private static bool _enableTupleTrace;
	}

	public abstract class BaseTable {
		protected BaseTable() {
			EnableEvents = true;
		}

		public object AttachedProperty { get; set; }
		public bool EnableEvents { get; set; }

		public virtual void ClearTupleStates() {
		}

		internal abstract bool Contains(Tuple tuple);
		internal abstract void CommandSet(Tuple tuple, DbAttribute attribute, object value);
	}
}
