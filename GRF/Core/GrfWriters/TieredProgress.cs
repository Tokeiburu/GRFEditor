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
		private Dictionary<int, double> _weights = new Dictionary<int, double>();
		private double _totalWeight;
		private double _totalProcessed;
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

		public void SplitTier(int subDivision) {
			if (subDivision <= 1)
				return;

			var currentWeight = _weights[_currentTier];
			var newWeight = currentWeight / subDivision;

			for (int i = _weights.Count - 1; i > _currentTier; i--)
				_weights[i + subDivision] = _weights[i];

			for (int i = _currentTier; i < _currentTier + subDivision; i++)
				_weights[i] = newWeight;

			_totalWeight = 0;
			for (int i = 0; i < _weights.Count; i++)
				_totalWeight += _weights[i];
		}

		public void CompleteTier() {
			_totalProcessed += _weights[_currentTier];
			_currentTier++;

			if (_totalWeight == 0)
				_progressObject.Progress = -1;
			else
				_progressObject.Progress = (float)Math.Min(99.99f, 100.0f * _totalProcessed / Math.Max(1, _totalWeight));
		}

		public void SetTierProgress(int currentCount) {
			SetTierProgress(currentCount / (float)_weights[_currentTier]);
		}

		public void SetTierProgress(float progress) {
			if (_totalWeight == 0)
				_progressObject.Progress = -1;
			else
				_progressObject.Progress = (float)Math.Min(99.99f, (progress * _weights[_currentTier] + _totalProcessed) / Math.Max(1, _totalWeight) * 100.0f);
		}

		public void SetSpecialState(int value) {
			_progressObject.Progress = value;
		}
	}
}
