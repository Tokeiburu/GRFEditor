using System.Collections.Generic;

namespace GRF.FileFormats.LubFormat.Types {
	public class LubStack {
		private readonly List<ILubObject> _stack = new List<ILubObject>();
		public int Pointer { get; set; }

		public List<ILubObject> Internal {
			get { return _stack; }
		}

		public ILubObject this[int index] {
			get {
				//if (index == Pointer) {
				Pointer = index;
				//Pointer--;
				//}

				return _stack[index];
			}
			set {
				_stack[index] = value;

				//if (index > Pointer) {
				Pointer = index;
				//}
			}
		}

		public int Count {
			get { return _stack.Count; }
		}

		public void Init(int max) {
			for (int i = 0; i < max; i++) {
				_stack.Add(null);
			}
		}

		public void Add(ILubObject item) {
			_stack.Add(item);
		}

		public List<string> DumpLines() {
			List<string> lines = new List<string>();

			for (int i = 0; i < _stack.Count; i++) {
				ILubObject obj = _stack[i];

				if (obj is LubValueType) {
					lines.Add("[" + i + "] " + obj);
				}
				else if (obj is LubReferenceType) {
					LubReferenceType keyValue = (LubReferenceType) obj;
					lines.Add("[" + i + "] {Key = " + keyValue.Key + "; Value = " + (keyValue.Value == null ? "null" : keyValue.Value.ToString()));
				}
				else if (obj == null) {
					lines.Add("[" + i + "] null");
				}
				else if (obj is LubDictionary) {
					lines.Add("[" + i + "] { New ditionary }");
				}
				else {
					lines.Add("[" + i + "] " + _stack[i]);
				}
			}

			return lines;
		}
	}
}