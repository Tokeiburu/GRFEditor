namespace GRF.FileFormats.GatFormat.Commands {
	public class ChangeType : IGatCommand {
		private readonly int _cellIndex;
		private readonly int[] _cellIndexes;
		private readonly GatType _type;
		private GatType? _oldType;
		private GatType[] _oldTypes;

		public ChangeType(int cellIndex, GatType type) {
			_cellIndex = cellIndex;
			_type = type;
		}

		public ChangeType(int[] cellIndexes, GatType type) {
			_cellIndexes = cellIndexes;
			_type = type;
		}

		#region IGatCommand Members

		public void Execute(Gat gat) {
			if (_cellIndexes != null) {
				if (_oldTypes == null) {
					_oldTypes = new GatType[_cellIndexes.Length];

					for (int i = 0; i < _cellIndexes.Length; i++)
						_oldTypes[i] = gat[_cellIndexes[i]].Type;
				}

				for (int i = 0; i < _cellIndexes.Length; i++)
					gat[_cellIndexes[i]].Type = _type;
			}
			else {
				if (_oldType == null) {
					_oldType = gat[_cellIndex].Type;
				}

				gat[_cellIndex].Type = _type;
			}
		}

		public void Undo(Gat gat) {
			if (_cellIndexes != null) {
				for (int i = 0; i < _cellIndexes.Length; i++)
					gat[_cellIndexes[i]].Type = _oldTypes[i];
			}
			else
				gat[_cellIndex].Type = _oldType.Value;
		}

		public string CommandDescription {
			get {
				if (_cellIndexes != null) {
					return "Gat.Range(" + _cellIndexes.Length + ").Type = " + _type;
				}
				return "Gat[" + _cellIndex + "].Type = " + _type;
			}
		}

		#endregion
	}
}