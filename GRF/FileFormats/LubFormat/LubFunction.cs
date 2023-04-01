using System;
using System.Collections.Generic;
using System.Text;
using GRF.FileFormats.LubFormat.Types;
using GRF.FileFormats.LubFormat.VM;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace GRF.FileFormats.LubFormat {
	public class LubFunction : ILubObject {
		internal readonly Dictionary<LubString, bool> Instantiated = new Dictionary<LubString, bool>();
		private readonly Lub _decompiler;
		private List<LubValueType> _constants = new List<LubValueType>();
		private List<LubFunction> _functions = new List<LubFunction>();
		private List<LubReferenceType> _localVariables = new List<LubReferenceType>();
		private List<LubLine> _lubLines = new List<LubLine>();
		private LubStack _stack = new LubStack();
		private List<LubReferenceType> _upValues = new List<LubReferenceType>();

		public LubFunction(int functionLevel, byte[] data, ref int offset, Lub decompiler) {
			Label = decompiler.TotalFunctionCount++;

			FunctionLevel = functionLevel;
			_decompiler = decompiler;

			if (decompiler.Header.Is(5, 1)) {
				int sizeOfSourceName = BitConverter.ToInt32(data, offset);
				offset += 4;

				if (sizeOfSourceName > 0) {
					SourceName = EncodingService.Ansi.GetString(data, offset, sizeOfSourceName, '\0');
					offset += sizeOfSourceName;
				}

				LineDefined = BitConverter.ToInt32(data, offset);
				offset += 4;

				LastLineDefined = BitConverter.ToInt32(data, offset);
				offset += 4;

				Nups = data[offset];
				offset++;

				NumberOfParameters = data[offset];
				offset++;

				IsVarArg = data[offset];
				offset++;

				MaxStackSize = data[offset];
				offset++;

				int sizeCode = BitConverter.ToInt32(data, offset);
				offset += 4;

				List<OpCodes.AbstractInstruction> instructions = new List<OpCodes.AbstractInstruction>();

				for (int i = 0; i < sizeCode; i++) {
					try {
						byte[] dataRead = new byte[4];
						Buffer.BlockCopy(data, offset, dataRead, 0, 4);
						offset += 4;

						instructions.Add(OpcodeMapper.GetInstruction(dataRead, decompiler));
					}
					catch {
						LubErrorHandler.Handle("Failed to retrieve the opcode instruction set. Version probably not supported.", LubSourceError.LubReader);
					}
				}

				int constantsSize = BitConverter.ToInt32(data, offset);
				offset += 4;

				for (int i = 0; i < constantsSize; i++) {
					Constants.Add(ValueTypeProvider.GetConstant(data[offset++], data, ref offset, decompiler.Header));
				}

				int sizeP = BitConverter.ToInt32(data, offset);
				offset += 4;

				for (int i = 0; i < sizeP; i++) {
					_functions.Add(new LubFunction(functionLevel + 1, data, ref offset, decompiler));
				}

				SizeLineInfo = BitConverter.ToInt32(data, offset);
				offset += 4;

				for (int i = 0; i < SizeLineInfo; i++) {
					LubLines.Add(new LubLine(BitConverter.ToInt32(data, offset)));
					offset += 4;
				}

				// Local variables
				int sizeOfLocVariables = BitConverter.ToInt32(data, offset);
				offset += 4;

				for (int i = 0; i < sizeOfLocVariables; i++) {
					LubValueType name = ValueTypeProvider.GetConstant(4, data, ref offset, decompiler.Header);

					LocalVariables.Add(new LubReferenceType((LubString) name, null, BitConverter.ToInt32(data, offset), BitConverter.ToInt32(data, offset + 4)));
					offset += 8;
				}

				int sizeOfUpValues = BitConverter.ToInt32(data, offset);
				offset += 4;

				for (int i = 0; i < sizeOfUpValues; i++) {
					UpValues.Add(new LubReferenceType((LubString) ValueTypeProvider.GetConstant(4, data, ref offset, decompiler.Header), null));
				}

				Instructions = instructions;
			}
			else if (decompiler.Header.Is(5, 0)) {
				int sizeOfSourceName = BitConverter.ToInt32(data, offset);
				offset += 4;

				if (sizeOfSourceName > 0) {
					SourceName = EncodingService.Ansi.GetString(data, offset, sizeOfSourceName, '\0');
					offset += sizeOfSourceName;
				}

				LineDefined = BitConverter.ToInt32(data, offset);
				offset += 4;

				Nups = data[offset];
				offset++;

				NumberOfParameters = data[offset];
				offset++;

				IsVarArg = data[offset];
				offset++;

				MaxStackSize = data[offset];
				offset++;

				SizeLineInfo = BitConverter.ToInt32(data, offset);
				offset += 4;

				for (int i = 0; i < SizeLineInfo; i++) {
					LubLines.Add(new LubLine(BitConverter.ToInt32(data, offset)));
					offset += 4;
				}

				// Local variables
				int sizeOfLocVariables = BitConverter.ToInt32(data, offset);
				offset += 4;

				for (int i = 0; i < sizeOfLocVariables; i++) {
					LubValueType name = ValueTypeProvider.GetConstant(4, data, ref offset, decompiler.Header);

					LocalVariables.Add(new LubReferenceType((LubString) name, null, BitConverter.ToInt32(data, offset), BitConverter.ToInt32(data, offset + 4)));
					offset += 8;
				}

				int sizeOfUpValues = BitConverter.ToInt32(data, offset);
				offset += 4;

				for (int i = 0; i < sizeOfUpValues; i++) {
					UpValues.Add(new LubReferenceType((LubString) ValueTypeProvider.GetConstant(4, data, ref offset, decompiler.Header), null));
				}

				int constantsSize = BitConverter.ToInt32(data, offset);
				offset += 4;

				for (int i = 0; i < constantsSize; i++) {
					Constants.Add(ValueTypeProvider.GetConstant(data[offset++], data, ref offset, decompiler.Header));
				}

				int sizeP = BitConverter.ToInt32(data, offset);
				offset += 4;

				for (int i = 0; i < sizeP; i++) {
					_functions.Add(new LubFunction(functionLevel + 1, data, ref offset, decompiler));
				}

				int sizeCode = BitConverter.ToInt32(data, offset);
				offset += 4;

				List<OpCodes.AbstractInstruction> instructions = new List<OpCodes.AbstractInstruction>();

				for (int i = 0; i < sizeCode; i++) {
					try {
						byte[] dataRead = new byte[4];
						Buffer.BlockCopy(data, offset, dataRead, 0, 4);
						offset += 4;

						instructions.Add(OpcodeMapper.GetInstruction(dataRead, decompiler));
					}
					catch {
						LubErrorHandler.Handle("Failed to retrieve the opcode instruction set. Version probably not supported.", LubSourceError.LubReader);
					}
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
		public byte IsVarArg { get; private set; }
		public byte MaxStackSize { get; set; }
		public byte NumberOfParameters { get; set; }

		public List<LubFunction> Functions {
			get { return _functions; }
			set { _functions = value; }
		}

		public List<OpCodes.AbstractInstruction> Instructions { get; set; }
		public int FunctionLevel { get; set; }
		public int SizeLineInfo { get; set; }
		public string SourceName { get; set; }

		public List<LubReferenceType> LocalVariables {
			get { return _localVariables; }
			set { _localVariables = value; }
		}

		public List<LubReferenceType> UpValues {
			get { return _upValues; }
			set { _upValues = value; }
		}

		public List<LubLine> LubLines {
			get { return _lubLines; }
			set { _lubLines = value; }
		}

		public List<LubValueType> Constants {
			get { return _constants; }
			set { _constants = value; }
		}

		public int LineDefined { get; set; }
		public int LastLineDefined { get; set; }
		public int InstructionIndex { get; set; }

		public Dictionary<int, BlockDelimiter> BlockDelimiters { get; set; }

		#region ILubObject Members

		public void Print(StringBuilder builder, int level) {
			builder.Append(OpCodesToLua.DecompileFunction(_decompiler, this));
		}

		public int GetLength() {
			return 1000;
		}

		#endregion

		public bool IsVariableInstantiated(LubString value) {
			if (Instantiated.ContainsKey(value)) {
				return Instantiated[value];
			}

			LubErrorHandler.Handle("Asked for a local variable instantiation state, but the variable isn't local.", LubSourceError.CodeDecompiler);
			return false;
		}

		public void InitFunctionStack() {
			for (int i = 0, count = Math.Max(MaxStackSize, LocalVariables.Count); i < count; i++) {
				Stack.Add(null);
			}

			for (int i = 0; i < LocalVariables.Count; i++) {
				Stack[i] = LocalVariables[i];
				Instantiated[LocalVariables[i].Key] = false;

				if (i < NumberOfParameters)
					Instantiated[LocalVariables[i].Key] = true;
			}
		}

		public override string ToString() {
			return Methods.TypeToString(this);
		}
	}
}