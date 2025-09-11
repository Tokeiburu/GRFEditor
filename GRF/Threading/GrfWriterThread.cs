using System;
using System.Collections.Generic;
using System.IO;
using GRF.ContainerFormat;
using GRF.Core;

namespace GRF.Threading {
	public abstract class GrfWriterThread<TEntry> : PausableThread where TEntry : ContainerEntry {
		protected int EndIndex;
		protected int StartIndex;

		public int NumberOfFilesProcessed = 0;		// Used by the dispatcher to estimate the progress
		public bool Terminated = false;				// Used by the dispatcher to evaluate when the work is done
		public bool Error { get; set; }
		public Exception Exception { get; set; }

		public string FileName { get; set; }

		protected List<TEntry> _entries;
		protected ContainerAbstract<TEntry> _grfData;

		public FileStream OutputFileStream;

		public void Init(GrfHolder container, List<TEntry> entries, int startIndex, int endIndex) {
			Init((ContainerAbstract<TEntry>) (object) container.Container, entries, startIndex, endIndex);
		}

		public void Init(ContainerAbstract<TEntry> container, List<TEntry> entries, int startIndex, int endIndex) {
			_entries = entries;
			StartIndex = startIndex;
			EndIndex = endIndex;
			_grfData = container;
		}

		public abstract void Start();
	}

	public abstract class AGenericThreadPool<T> : PausableThread {
		protected int EndIndex;
		protected int StartIndex;

		public int NumberOfFilesProcessed = 0;		// Used by the dispatcher to estimate the progress
		public bool Terminated = false;				// Used by the dispatcher to evaluate when the work is done
		public bool Error { get; set; }
		public Exception Exception { get; set; }
		public string FileName { get; set; }
		public int ThreadId { get; private set; }

		protected List<T> _entries;
		protected IProgress _progress;

		public void Init(IProgress progress, List<T> entries, int startIndex, int endIndex) {
			ThreadId = startIndex;
			_entries = entries;
			_progress = progress;
			StartIndex = startIndex;
			EndIndex = endIndex;
		}

		public abstract void Start();
	}

	public class GenericPoolThread<T> : AGenericThreadPool<T> {
		private readonly Action<T> _action;
		private readonly Action<T, GenericPoolThread<T>> _action2;

		public GenericPoolThread(Action<T> action) {
			_action = action;
		}

		public GenericPoolThread(Action<T, GenericPoolThread<T>> action) {
			_action2 = action;
		}

		public override void Start() {
			GrfThread.Start(_start, "GRF - GenericPoolThread thread starter");
		}

		private void _start() {
			try {
				for (int i = StartIndex; i < EndIndex; i++) {
					if (IsPaused) {
						Pause();
					}

					if (_progress != null && _progress.IsCancelling)
						return;

					if (_action == null)
						_action2(_entries[i], this);
					else
						_action(_entries[i]);

					NumberOfFilesProcessed++;
				}
			}
			catch (Exception err) {
				Exception = err;
				Error = true;
			}
			finally {
				Terminated = true;
			}
		}
	}
}
