using GRF.Threading;
using System;
using System.Collections.Generic;

namespace GRF.Core.GrfWriters {
	public class TieredProgress {
		public const int SpecialIndexingContent = -5;
		public const int SpecialPending = -1;
		public const int SpecialCopyingFile = -7;

		private int _currentTier = 0;
		public float CurrentProgress = 0;
		public float OverrideState = 0;
		private Dictionary<int, long> _weights = new Dictionary<int, long>();
		private long _totalWeight;
		private long _totalProcessed;
		private IProgress _progressObject;

		public TieredProgress(IProgress progressObject) {
			_progressObject = progressObject;
		}

		public void AddWeightedTier(long weight) {
			_weights[_weights.Count] = weight;
			_totalWeight += weight;
		}

		public void AddTiers(int count = 1) {
			for (int i = 0; i < count; i++) {
				AddWeightedTier(1);
			}
		}

		public void CompleteTier() {
			_totalProcessed += _weights[_currentTier];
			_currentTier++;
		}

		public void SetTierProgress(int currentCount) {
			SetTierProgress((float)currentCount / _weights[_currentTier]);
		}

		public void SetTierProgress(float progress) {
			_progressObject.Progress = Math.Min(99.99f, (progress * _weights[_currentTier] + _totalProcessed) / _totalWeight * 100.0f);
		}

		public void SetSpecialState(int value) {
			_progressObject.Progress = value;
		}
	}
}
