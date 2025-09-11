using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GRF.FileFormats.LubFormat.Types;
using GRF.FileFormats.LubFormat.VM;
using GRF.IO;
using Utilities;

namespace GRF.FileFormats.LubFormat {
	[Flags]
	public enum VarArgType {
		None,
		VARARG_HASARG = 1,
		VARARG_ISVARARG = 2,
		VARARG_NEEDSARG = 4
	}

	public class LubFunction : ILubObject {
		public struct BlockDelimiter {
			public string Label { get; set; }
			public int BlockStart { get; set; }
		}

		internal Dictionary<int, bool> Instantiated = new Dictionary<int, bool>();
		private readonly Stack<Dictionary<int, bool>> _instantiatedStack = new Stack<Dictionary<int, bool>>();
		internal readonly Lub _decompiler;
		private List<LubValueType> _constants = new List<LubValueType>();
		private List<LubFunction> _functions = new List<LubFunction>();
		private List<LubReferenceType> _debug_localVariables = new List<LubReferenceType>();
		private LubStack _stack = new LubStack();
		private List<LubReferenceType> _upValues = new List<LubReferenceType>();
		public OpCodes.StackResolver StackResolver = new OpCodes.StackResolver();

		public LubFunction(int functionLevel, IBinaryReader reader, Lub decompiler) {
			Label = decompiler.TotalFunctionCount++;

			FunctionLevel = functionLevel;
			_decompiler = decompiler;

			if (decompiler.Header.Is(5, 1)) {
				int sizeOfSourceName = reader.Int32();

				if (sizeOfSourceName > 0) {
					SourceName = reader.StringANSI(sizeOfSourceName);
				}

				LineDefined = reader.Int32();
				LastLineDefined = reader.Int32();
				Nups = reader.Byte();
				NumberOfParameters = reader.Byte();
				IsVarArg = (VarArgType)reader.Byte();
				MaxStackSize = reader.Byte();

				int sizeCode = reader.Int32();
				List<OpCodes.AbstractInstruction> instructions = new List<OpCodes.AbstractInstruction>();

				for (int i = 0; i < sizeCode; i++) {
					instructions.Add(OpcodeMapper.GetInstruction(reader.Bytes(4), _decompiler.Header.InstructionSet, decompiler));
				}

				int constantsSize = reader.Int32();

				for (int i = 0; i < constantsSize; i++) {
					Constants.Add(ValueTypeProvider.GetConstant(reader.Byte(), reader, decompiler.Header));
				}

				int sizeP = reader.Int32();

				for (int i = 0; i < sizeP; i++) {
					_functions.Add(new LubFunction(functionLevel + 1, reader, decompiler));
				}

				SizeLineInfo = reader.Int32();
				reader.Forward(SizeLineInfo * 4);

				// Local variables
				int sizeOfLocVariables = reader.Int32();

				for (int i = 0; i < sizeOfLocVariables; i++) {
					LubValueType name = ValueTypeProvider.GetConstant(4, reader, decompiler.Header);

					Debug_LocalVariables.Add(new LubReferenceType((LubString)name, null, reader.Int32(), reader.Int32()));
				}

				if (Debug_LocalVariables.Count > 0 && Debug_LocalVariables[0].Key.Value == "self") {
					SelfParameter = true;
				}

				int sizeOfUpValues = reader.Int32();

				for (int i = 0; i < sizeOfUpValues; i++) {
					UpValues.Add(new LubReferenceType((LubString)ValueTypeProvider.GetConstant(4, reader, decompiler.Header), null));
				}

				Instructions = instructions;
			}
			else if (decompiler.Header.Is(5, 0)) {
				int sizeOfSourceName = reader.Int32();

				if (sizeOfSourceName > 0) {
					SourceName = reader.StringANSI(sizeOfSourceName);
				}

				LineDefined = reader.Int32();
				Nups = reader.Byte();
				NumberOfParameters = reader.Byte();
				IsVarArg = reader.ByteBool() ? VarArgType.VARARG_HASARG | VarArgType.VARARG_ISVARARG | VarArgType.VARARG_NEEDSARG : 0;
				MaxStackSize = reader.Byte();
				SizeLineInfo = reader.Int32();

				reader.Forward(4 * SizeLineInfo);

				// Local variables
				int sizeOfLocVariables = reader.Int32();

				for (int i = 0; i < sizeOfLocVariables; i++) {
					LubValueType name = ValueTypeProvider.GetConstant(4, reader, decompiler.Header);

					Debug_LocalVariables.Add(new LubReferenceType((LubString)name, null, reader.Int32(), reader.Int32()));
				}

				if (Debug_LocalVariables.Count > 0 && Debug_LocalVariables[0].Key.Value == "self") {
					SelfParameter = true;
				}

				int sizeOfUpValues = reader.Int32();

				for (int i = 0; i < sizeOfUpValues; i++) {
					UpValues.Add(new LubReferenceType((LubString)ValueTypeProvider.GetConstant(4, reader, decompiler.Header), null));
				}

				int constantsSize = reader.Int32();

				for (int i = 0; i < constantsSize; i++) {
					Constants.Add(ValueTypeProvider.GetConstant(reader.Byte(), reader, decompiler.Header));
				}

				int sizeP = reader.Int32();

				for (int i = 0; i < sizeP; i++) {
					_functions.Add(new LubFunction(functionLevel + 1, reader, decompiler));
				}

				int sizeCode = reader.Int32();

				List<OpCodes.AbstractInstruction> instructions = new List<OpCodes.AbstractInstruction>();

				for (int i = 0; i < sizeCode; i++) {
					instructions.Add(OpcodeMapper.GetInstruction(reader.Bytes(4), _decompiler.Header.InstructionSet, decompiler));
				}

				Instructions = instructions;
			}
		}

		public int Label { get; set; }

		internal Lub Decompiler {
			get { return _decompiler; }
		}

		public LubStack Stack {
			get { return _stack; }
			set { _stack = value; }
		}

		public byte Nups { get; private set; }
		public VarArgType IsVarArg { get; private set; }
		public byte MaxStackSize { get; set; }
		public byte NumberOfParameters { get; set; }

		public int NumberOfParametersWithArg {
			get {
				return NumberOfParameters +
				       ((IsVarArg & (VarArgType.VARARG_HASARG | VarArgType.VARARG_ISVARARG | VarArgType.VARARG_NEEDSARG)) == (VarArgType.VARARG_HASARG | VarArgType.VARARG_ISVARARG | VarArgType.VARARG_NEEDSARG) ? 1 : 0);
			}
		}

		public bool SelfParameter { get; set; }

		public List<LubFunction> Functions {
			get { return _functions; }
			set { _functions = value; }
		}

		public List<OpCodes.AbstractInstruction> Instructions { get; set; }
		public int FunctionLevel { get; set; }

		public int BaseIndent {
			get { return FunctionLevel == 0 ? 0 : 1; }
		}

		public int SizeLineInfo { get; set; }
		public string SourceName { get; set; }

		public List<LubReferenceType> Debug_LocalVariables {
			get { return _debug_localVariables; }
			set { _debug_localVariables = value; }
		}

		public List<LubReferenceType> UpValues {
			get { return _upValues; }
			set { _upValues = value; }
		}

		public List<LubValueType> Constants {
			get { return _constants; }
			set { _constants = value; }
		}

		public int LineDefined { get; set; }
		public int LastLineDefined { get; set; }
		public int PC { get; set; }

		public Dictionary<int, BlockDelimiter> BlockDelimiters { get; set; }

		#region ILubObject Members
		public void Print(StringBuilder builder, int level) {
			builder.Append(OpCodesToLua.DecompileFunction(_decompiler, this, level));
		}

		public int GetLength() {
			return 1000;
		}
		#endregion

		public bool IsVariableInstantiated(int index) {
			if (Instantiated.ContainsKey(index)) {
				return Instantiated[index];
			}

			LubErrorHandler.Handle("Asked for a local variable instantiation state, but the variable isn't local.", LubSourceError.CodeDecompiler);
			return false;
		}

		public void InitFunctionStack() {
			Stack.Clear();

			for (int i = 0, count = Math.Max(MaxStackSize, Debug_LocalVariables.Count); i < count; i++) {
				Stack.Add(null);
			}

			for (int i = 0; i < Debug_LocalVariables.Count; i++) {
				Stack[i] = Debug_LocalVariables[i];
				Instantiated[i] = false;

				if (i < NumberOfParameters)
					Instantiated[i] = true;
			}

			Stack.SetAllIsAssigned(false);
		}

		public override string ToString() {
			return Methods.TypeToString(this);
		}

		public void PushInstances(bool copyPrevious = true) {
			Dictionary<int, bool> ins = Instantiated.ToDictionary(entry => entry.Key, entry => entry.Value);

			_instantiatedStack.Push(ins);

			if (!copyPrevious) {
				Instantiated.Clear();
			}
		}

		public void PopInstances() {
			Instantiated = _instantiatedStack.Pop();
		}
	}
}