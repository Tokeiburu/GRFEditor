using System;
using System.Diagnostics;
using System.Text;
using Utilities;

namespace GRF.System {
	public static class CpuPerformance {
		private static readonly PerformanceCounter _cpuCounter;

		static CpuPerformance() {
			try {
				StringBuilder buffer = new StringBuilder(1024);
				uint bufferSize = (uint) buffer.Capacity;

				NativeMethods.PdhLookupPerfNameByIndex(Environment.MachineName, 6, buffer, ref bufferSize);
				string category = buffer.ToString().Substring(0, (int)(bufferSize - 1));

				buffer = new StringBuilder(1024);
				bufferSize = (uint)buffer.Capacity;
				NativeMethods.PdhLookupPerfNameByIndex(Environment.MachineName, 238, buffer, ref bufferSize);
				string processor = buffer.ToString().Substring(0, (int)(bufferSize - 1));

				_cpuCounter = new PerformanceCounter(processor, category, "_Total");
			}
			catch { }
		}
		public static float GetCurrentCpuUsage() {
			try {
				return _cpuCounter.NextValue();
			}
			catch {
				return 0;
			}
		}
	}
}
