using System;
using Utilities;

namespace GRF.GrfSystem {
	public static class CpuPerformance {
		private static IntPtr _query;
		private static IntPtr _counter;

		static CpuPerformance() {
			try {
				// Don't bother closing the _query with PdhCloseQuery; it'll get done when the app closes
				int result;
				
				result = NativeMethods.PdhOpenQuery(null, IntPtr.Zero, out _query);
				if (result != NativeMethods.ERROR_SUCCESS)
					throw new InvalidOperationException();

				result = NativeMethods.PdhAddCounter(_query, @"\Processor(_Total)\% Processor Time", IntPtr.Zero, out _counter);
				if (result != NativeMethods.ERROR_SUCCESS)
					throw new InvalidOperationException();

				// First query for initializing
				NativeMethods.PdhCollectQueryData(_query);
			}
			catch {
			}
		}
		public static float GetCurrentCpuUsage() {
			try {
				int result;

				result = NativeMethods.PdhCollectQueryData(_query);
				if (result != NativeMethods.ERROR_SUCCESS)
					return 0;

				NativeMethods.PDH_FMT_COUNTERVALUE value;
				uint type;
				result = NativeMethods.PdhGetFormattedCounterValue(_counter, NativeMethods.PDH_FMT_DOUBLE, out type, out value);
				if (result != NativeMethods.ERROR_SUCCESS)
					return 0;

				return (float)value.doubleValue;
			}
			catch {
				return 0;
			}
		}
	}
}
