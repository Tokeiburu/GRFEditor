using System.Collections.Generic;
using System.Linq;

namespace GRF.FileFormats.LubFormat.Types {
	public class LubStack {
		private List<ILubObject> _stack = new List<ILubObject>();
		private List<bool> _stackAssigned = new List<bool>();
		private readonly Stack<List<ILubObject>> _scope = new Stack<List<ILubObject>>();
		private readonly Stack<List<bool>> _scopeAssigned = new Stack<List<bool>>();
		private List<ILubObject> _previousStack;
		public int Pointer { get; set; }
		public int LastGetPointer { get; set; }
		public int LastSetPointer { get; set; }

		public List<ILubObject> Internal {
			get { return _stack; }
		}

		public int GetStashedSize {
			get { return _scope.Count; }
		}

		public ILubObject this[int index] {
			get {
				LastGetPointer = Pointer = index;
				return _stack[index];
			}
			set {
				_stack[index] = value;
				_stackAssigned[index] = value != null;
				LastSetPointer = Pointer = index;
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

		public void SetAllIsAssigned(bool value) {
			for (int i = 0; i < _stackAssigned.Count; i++)
				_stackAssigned[i] = value;
		}

		public void SetIsAssigned(int index, bool value) {
			_stackAssigned[index] = value;
		}

		public bool GetIsAssigned(int index) {
			return _stackAssigned[index];
		}

		public void Add(ILubObject item) {
			_stack.Add(item);
			_stackAssigned.Add(false);
		}

		public void Clear() {
			_stack.Clear();
			_stackAssigned.Clear();
		}

		public List<string> DumpLines() {
			List<string> lines = new List<string>();

			for (int i = 0; i < _stack.Count; i++) {
				ILubObject obj = _stack[i];

				if (obj is LubValueType) {
					lines.Add("[" + i + "] " + obj);
				}
				else if (obj is LubReferenceType) {
					LubReferenceType keyValue = (LubReferenceType)obj;
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

		public void Push(bool copyPrevious = true) {
			var l = _stack.ToList();

			for (int i = 0; i < l.Count; i++) {
				var v = l[i] as LubReferenceType;

				if (v != null) {
					v.Push();
				}
			}

			_scope.Push(l);

			var stackAssigned = _stackAssigned.ToList();
			_scopeAssigned.Push(stackAssigned);

			for (int i = 0; i < _stack.Count; i++) {
				_stack[i] = copyPrevious ? l[i] : null;
				_stackAssigned[i] = copyPrevious ? stackAssigned[i] : false;
			}

			_previousStack = l;
		}

		public ILubObject GetPopValue(int index) {
			return _previousStack[index];
		}

		public void Pop() {
			_stack = _scope.Pop();

			for (int i = 0; i < _stack.Count; i++) {
				var v = _stack[i] as LubReferenceType;

				if (v != null) {
					v.Pop();
				}
			}

			_stackAssigned = _scopeAssigned.Pop();
		}
	}
}