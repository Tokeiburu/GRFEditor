using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.Core;
using GRF.IO;
using GRF.System;

namespace GRF.Threading {
	public class GrfThreadPool<TEntry> where TEntry : ContainerEntry {
		private List<GrfWriterThread<TEntry>> _threads = new List<GrfWriterThread<TEntry>>();
		private int _numberOfFiles;
		public List<TEntry> Entries { get; protected set; }

		public List<GrfWriterThread<TEntry>> Threads {
			get { return _threads; }
		}

		public GrfThreadPool() {
			Entries = new List<TEntry>();
		}

		public void Initialize<T>(ContainerAbstract<TEntry> grf, List<TEntry> sortedEntries, int numberOfThreads = -1) where T : GrfWriterThread<TEntry>, new() {
			if (numberOfThreads < 0)
				numberOfThreads = sortedEntries.Count < Settings.MaximumNumberOfThreads ? sortedEntries.Count : Settings.MaximumNumberOfThreads;

			for (int i = 0; i < numberOfThreads; i++) {
				int startIndex = (int)(sortedEntries.Count / (float)numberOfThreads * i);
				int endIndex = (int)(sortedEntries.Count / (float)numberOfThreads * (i + 1));
				T t = new T();
				t.Init(grf, sortedEntries, startIndex, endIndex);
				_threads.Add(t);
			}

			_numberOfFiles += sortedEntries.Count;
			Entries.AddRange(sortedEntries);
		}

		public void Set<T>(List<T> threads, ContainerAbstract<TEntry> grf, List<TEntry> sortedEntries, int numberOfThreads = -1) where T : GrfWriterThread<TEntry>, new() {
			Entries = sortedEntries;
			_numberOfFiles = Entries.Count;
			_threads = (List<GrfWriterThread<TEntry>>) (object) threads;
		}

		public void Start(Action<float> progressUpdate, Func<bool> isCancelling) {
			Start(progressUpdate, isCancelling, true, true);
		}

		public void Start(Action<float> progressUpdate, Func<bool> isCancelling, bool enableCpuPerformance, bool startThreads) {
			const int DelayThreads = 2;

			if (startThreads) {
				for (int index = 0; index < _threads.Count; index++) {
					GrfWriterThread<TEntry> t = _threads[index];
					if (Settings.CpuMonitoringEnabled && index > DelayThreads)
						t.IsPaused = true;

					t.Start();
				}
			}

			if (enableCpuPerformance) {
				if (Settings.CpuMonitoringEnabled)
					CpuPerformance.GetCurrentCpuUsage();
			}

			GrfWriterThread<TEntry> thread;
			float cpuPerf;
			int ignore = 0;

			while (_threads.Any(p => !p.Terminated)) {
				progressUpdate(_threads.Sum(p => p.NumberOfFilesProcessed) / (float)_numberOfFiles * 100.0f);

				// We have to detect if there are too many _threads being ran at the same time, 
				// this affects the computer's performance way too much.
				Thread.Sleep(_numberOfFiles < 25 ? 75 : 200);

				if (enableCpuPerformance && Settings.CpuMonitoringEnabled) {
					ignore--;
					ignore = ignore < 0 ? 0 : ignore;

					cpuPerf = CpuPerformance.GetCurrentCpuUsage();

					_threads.Where(p => !p.IsPaused).ToList().ForEach(p => p.IsPaused = false);

					if (ignore == 0) {
						if (cpuPerf < Settings.CpuUsageCritical) {
							thread = _threads.FirstOrDefault(p => p.IsPaused && !p.Terminated);

							if (thread != null) {
								thread.IsPaused = false;
							}
						}
						else {
							// Too many _threads ;S!
							if (!isCancelling() && _threads.Count(p => !p.IsPaused && !p.Terminated) > 1) {
								thread = _threads.FirstOrDefault(p => !p.IsPaused && !p.Terminated);

								if (thread != null) {
									ignore = 6;
									thread.IsPaused = true;
								}
							}
						}
					}

					// Ensures that at least one thread is activated
					if (!_threads.Any(p => !p.IsPaused && !p.Terminated)) {
						thread = _threads.FirstOrDefault(p => !p.Terminated);
						if (thread != null) {
							thread.IsPaused = false;
						}
					}
				}
				else {
					_threads.Where(p => !p.Terminated).ToList().ForEach(p => p.IsPaused = false);
				}

				if (isCancelling()) {
					_threads.ForEach(p => p.IsPaused = false);
				}
			}

			if (isCancelling()) throw new OperationCanceledException();

			if (_threads.Any(p => p.Error)) {
				ErrorHandler.HandleException("Generic failure : a task in the thread pool has failed to finish properly. The current operation will be cancelled.", _threads.First(p => p.Error).Exception);
				throw new OperationCanceledException();
			}

			if (progressUpdate != null)
				progressUpdate(_threads.Sum(p => p.NumberOfFilesProcessed) / (float)_numberOfFiles * 100.0f);
		}

		public uint Dump(Stream grfStream, long offset = GrfHeader.StructSize, long grfAddTotalSize = 0) {
			const int BufferCopyLength = 2097152;
			byte[] buffer = new byte[BufferCopyLength];
			long totalLength = offset;

			// Validate total length
			foreach (GrfWriterThread<TEntry> t in Threads) {
				totalLength += new FileInfo(t.FileName).Length;
			}

			if (totalLength + grfAddTotalSize > uint.MaxValue) {
				throw GrfExceptions.__GrfSizeLimitReached.Create();
			}

			foreach (GrfWriterThread<TEntry> t in Threads) {
				using (FileStream file = new FileStream(t.FileName, FileMode.Open)) {
					int len;
					while ((len = file.Read(buffer, 0, BufferCopyLength)) > 0) {
						grfStream.Write(buffer, 0, len);
					}
				}
				File.Delete(t.FileName);
			}

			foreach (TEntry grfentry in Entries) {
				grfentry.TemporaryOffset = (uint)offset;
				offset += (uint) grfentry.TemporarySizeCompressedAlignment;
			}

			return (uint)offset;
		}
	}

	public class GenericThreadPool<TObj> {
		private readonly List<AGenericThreadPool<TObj>> _threads = new List<AGenericThreadPool<TObj>>();
		private int _numberOfFiles;
		public List<TObj> Items { get; protected set; }

		public List<AGenericThreadPool<TObj>> Threads {
			get { return _threads; }
		}

		public GenericThreadPool() {
			Items = new List<TObj>();
		}

		public void Initialize(IProgress progress, IEnumerable<TObj> sortedEntries, Action<TObj> action, int numberOfThreads = -1) {
			var entries = sortedEntries.ToList();

			if (numberOfThreads < 0)
				numberOfThreads = entries.Count < Settings.MaximumNumberOfThreads ? entries.Count : Settings.MaximumNumberOfThreads;

			for (int i = 0; i < numberOfThreads; i++) {
				int startIndex = (int)(entries.Count / (float)numberOfThreads * i);
				int endIndex = (int)(entries.Count / (float)numberOfThreads * (i + 1));
				GenericPoolThread<TObj> t = new GenericPoolThread<TObj>(action);
				t.Init(progress, entries, startIndex, endIndex);
				_threads.Add(t);
			}

			_numberOfFiles += entries.Count;
			Items.AddRange(entries);
		}

		public void Initialize(IProgress progress, IEnumerable<TObj> sortedEntries, Action<TObj, GenericPoolThread<TObj>> action, int numberOfThreads = -1) {
			var entries = sortedEntries.ToList();

			if (numberOfThreads < 0)
				numberOfThreads = entries.Count < Settings.MaximumNumberOfThreads ? entries.Count : Settings.MaximumNumberOfThreads;

			for (int i = 0; i < numberOfThreads; i++) {
				int startIndex = (int)(entries.Count / (float)numberOfThreads * i);
				int endIndex = (int)(entries.Count / (float)numberOfThreads * (i + 1));
				GenericPoolThread<TObj> t = new GenericPoolThread<TObj>(action);
				t.Init(progress, entries, startIndex, endIndex);
				_threads.Add(t);
			}

			_numberOfFiles += entries.Count;
			Items.AddRange(entries);
		}

		public void Start() {
			Start(null, () => false);
		}

		public void Start(Action<float> progressUpdate, Func<bool> isCancelling, int delay = -1) {
			const int DelayThreads = 2;
			for (int index = 0; index < _threads.Count; index++) {
				AGenericThreadPool<TObj> t = _threads[index];
				if (Settings.CpuMonitoringEnabled && index > DelayThreads)
					t.IsPaused = true;

				t.Start();
			}

			if (Settings.CpuMonitoringEnabled)
				CpuPerformance.GetCurrentCpuUsage();

			AGenericThreadPool<TObj> thread;
			float cpuPerf;
			int ignore = 0;

			while (_threads.Any(p => !p.Terminated)) {
				if (progressUpdate != null)
					progressUpdate(_threads.Sum(p => p.NumberOfFilesProcessed) / (float)_numberOfFiles * 100.0f);

				// We have to detect if there are too many _threads being ran at the same time, 
				// this affects the computer's performance way too much.
				if (delay < 0) {
					Thread.Sleep(_numberOfFiles < 25 ? 75 : 200);
				}
				else {
					Thread.Sleep(delay);
				}

				if (Settings.CpuMonitoringEnabled) {
					ignore--;
					ignore = ignore < 0 ? 0 : ignore;

					cpuPerf = CpuPerformance.GetCurrentCpuUsage();

					_threads.Where(p => !p.IsPaused).ToList().ForEach(p => p.IsPaused = false);

					if (ignore == 0) {
						if (cpuPerf < Settings.CpuUsageCritical) {
							thread = _threads.FirstOrDefault(p => p.IsPaused && !p.Terminated);

							if (thread != null) {
								thread.IsPaused = false;
							}
						}
						else {
							// Too many _threads ;S!
							if (!isCancelling() && _threads.Count(p => !p.IsPaused && !p.Terminated) > 1) {
								thread = _threads.FirstOrDefault(p => !p.IsPaused && !p.Terminated);

								if (thread != null) {
									ignore = 6;
									thread.IsPaused = true;
								}
							}
						}
					}

					// Ensures that at least one thread is activated
					if (!_threads.Any(p => !p.IsPaused && !p.Terminated)) {
						thread = _threads.FirstOrDefault(p => !p.Terminated);
						if (thread != null) {
							thread.IsPaused = false;
						}
					}
				}
				else {
					_threads.Where(p => !p.Terminated).ToList().ForEach(p => p.IsPaused = false);
				}

				if (isCancelling()) {
					_threads.ForEach(p => p.IsPaused = false);
				}
			}

			if (isCancelling()) throw new OperationCanceledException();

			if (_threads.Any(p => p.Error)) {
				ErrorHandler.HandleException("Generic failure : a task in the thread pool has failed to finish properly. The current operation will be cancelled.", _threads.First(p => p.Error).Exception);
				throw new OperationCanceledException();
			}

			if (progressUpdate != null)
				progressUpdate(_threads.Sum(p => p.NumberOfFilesProcessed) / (float)_numberOfFiles * 100.0f);
		}
	}

	public static class GenericThreadPool {
		public static int? MaxThreadAmount { get; set; }

		public static void For<T>(IList<T> entries, Action<T> action) {
			GenericThreadPool<T> threadPool = new GenericThreadPool<T>();
			threadPool.Initialize(null, entries, action);
			threadPool.Start();
		}

		public static void For<T>(IProgress progress, IList<T> entries, Action<T> action) {
			GenericThreadPool<T> threadPool = new GenericThreadPool<T>();
			threadPool.Initialize(progress, entries, action);
			threadPool.Start();
		}

		public static void For<T>(IProgress progress, IList<T> entries, Action<T> action, Action<float> progressUpdate, Func<bool> isCancelling) {
			GenericThreadPool<T> threadPool = new GenericThreadPool<T>();
			threadPool.Initialize(progress, entries, action, MaxThreadAmount ?? -1);
			threadPool.Start(progressUpdate, isCancelling);
		}
	}
}
