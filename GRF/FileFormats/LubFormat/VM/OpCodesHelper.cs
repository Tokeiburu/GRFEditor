using System.Collections.Generic;
using System.Linq;
using System.Text;
using GRF.FileFormats.LubFormat.Types;
using Utilities.Extension;

namespace GRF.FileFormats.LubFormat.VM {
	public static partial class OpCodes {
		private static readonly Dictionary<string, int> _toIgnore = new Dictionary<string, int>();

		static OpCodes() {
			_toIgnore.Add("(for generator)", 0);
			_toIgnore.Add("(for state)", 0);
			_toIgnore.Add("(for control)", 0);

			_toIgnore.Add("(for limit)", 0);
			_toIgnore.Add("(for index)", 0);
			_toIgnore.Add("(for step)", 0);
		}

		public static void AppendParameters(string functionName, StringBuilder builder, List<int> registers, LubFunction function) {
			builder.Append("(");

			if (registers[1] == 1) {
			}
			else if (registers[1] == 0) {
				// Validate the call
				var ins_call = function.Instructions[function.PC - 1] as Call;

				if (ins_call == null) {
					LubErrorHandler.Handle("Expected a function call as the last parameter.", LubSourceError.CodeDecompiler);
				}

				int stackPointer = ins_call.Registers[0];
				bool objectFunction = functionName.Contains(":");

				for (int i = registers[0] + 1 + (objectFunction ? 1 : 0); i <= stackPointer; i++) {
					object toPrint = GetKey(RegOutput(i, function));
					builder.Append(toPrint);

					if (i < stackPointer && toPrint != null) {
						builder.Append(", ");
					}
				}
			}
			else {
				bool objectFunction = functionName.Contains(":");

				for (int i = 1 + (objectFunction ? 1 : 0); i < registers[1]; i++) {
					builder.Append(GetKey(RegOutput(registers[0] + i, function)));

					if (i < registers[1] - 1) {
						builder.Append(", ");
					}
				}
			}

			builder.Append(")");
		}

		public static ILubObject RegOrK(int value, LubFunction function) {
			if (value >= function.Decompiler.Header.ConstantIndexor)
				return function.Constants[value - function.Decompiler.Header.ConstantIndexor];

			var r = function.Stack[value];

			if (r == null) {
				r = GetLocalName(value, function.PC, function);
			}

			return r;
		}

		public static bool IsConstant(int register, LubFunction function) {
			return register >= function.Decompiler.Header.ConstantIndexor;
		}

		public static string GetAccessor(int register, LubFunction function) {
			ILubObject indexer = RegOrK(register, function);

			// Reference
			if (indexer is LubReferenceType) {
				indexer = ((LubReferenceType)indexer).Key;
				return "[" + indexer + "]";
			}

			if (indexer is LubNumber)
				return "[" + indexer + "]";
			if (indexer is LubString) {
				var lubObj = (LubString)indexer;

				if (lubObj.IsValid()) {
					if (IsConstant(register, function) || lubObj.Source == LubSourceType.Constant) {
						return "." + indexer;
					}

					// Reference
					function.Stack[register] = null;
					return "[" + indexer + "]";
				}

				function.Stack[register] = null;

				if (lubObj.Value.Length == 0)
					return "[\"\"]";
				if (lubObj.Value[0] == '\"')
					return "[" + indexer + "]";

				return "[\"" + indexer + "]\"";
			}

			var key = GetKey(RegOrKOutput(register, function));
			//function.Stack[register] = null;
			return "[" + key + "]";
		}

		public static LubReferenceType GetLocalName(int local_number, int line, LubFunction function) {
			int original_line = local_number;

			for (int i = 0; i < function.Debug_LocalVariables.Count && function.Debug_LocalVariables[i].StartLine <= line; i++) {
				//
				if (line <= function.Debug_LocalVariables[i].EndLine) {
					local_number--;

					if (local_number < 0)
						return function.Debug_LocalVariables[i];
				}
			}

			// Alternative!
			line--;
			local_number = original_line;
			local_number++;

			for (int i = 0; i < function.Debug_LocalVariables.Count; i++) {
				//function.Debug_LocalVariables[i].StartLine <= line
				if (line <= function.Debug_LocalVariables[i].EndLine) {
					local_number--;

					if (local_number == 0)
						return function.Debug_LocalVariables[i];
				}
			}

			return null;
		}

		public static ILubObject RegOutput(int value, LubFunction function) {
			ILubObject acces = function.Stack[value];

			if (acces is LubValueType) {
				LubValueType accessor = (LubValueType)acces;

				if (accessor.Source == LubSourceType.Constant && accessor is LubString) {
					return new LubOutput("\"" + accessor + "\"");
				}
			}

			LubReferenceType reference = acces as LubReferenceType;
			if (reference != null) {
				if (function.PC < reference.StartLine) {
					return new LubNull();
				}
			}

			return acces;
		}

		public static ILubObject RA(AbstractInstruction ins, LubFunction function) {
			return function.Stack[ins.Registers[0]];
		}

		public static ILubObject RB(AbstractInstruction ins, LubFunction function) {
			return function.Stack[ins.Registers[1]];
		}

		public static ILubObject RC(AbstractInstruction ins, LubFunction function) {
			return function.Stack[ins.Registers[2]];
		}

		public static ILubObject RKB(AbstractInstruction ins, LubFunction function) {
			return RegOrK(1, function);
		}

		public static ILubObject RegOrKOutput(int value, LubFunction function) {
			ILubObject acces = RegOrK(value, function);

			//if (value >= function.Decompiler.Header.ConstantIndexor) {
			//	acces = function.Constants[value - function.Decompiler.Header.ConstantIndexor];
			//}
			//else {
			//	acces = function.Stack[value];
			//}

			LubValueType accessor = acces as LubValueType;

			if (accessor != null) {
				if (accessor.Source == LubSourceType.Constant && accessor is LubString) {
					return new LubOutput("\"" + accessor + "\"");
				}
			}

			return acces;
		}

		public static ILubObject GetKey(ILubObject value) {
			if (value is LubReferenceType) {
				return ((LubReferenceType)value).Key;
			}

			LubDictionary lubDictionary = value as LubDictionary;
			if (lubDictionary != null) {
				if (lubDictionary.Count == 0)
					return new LubString("{}");
			}
			return value;
		}

		public static ILubObject GetVal(ILubObject value) {
			return value is LubReferenceType ? ((LubReferenceType)value).Value : value;
		}

		public static T GetKey<T>(ILubObject value) where T : class, ILubObject {
			return value is LubReferenceType ? ((LubReferenceType)value).Key as T : (T)value;
		}

		public static VarPosition ShouldAssign(LubFunction function, int pc) {
			// Check both local and global
			var stackData = function.StackResolver.Fetch(pc, function);

			for (int i = 0; i < stackData.Count; i++) {
				var data = stackData[i];
				var local = data.Debug_LocalVariable;

				if (!data.IsParameter &&
				    !data.IsLoopControl &&
				    !function.IsVariableInstantiated(data.Debug_Index)) {
					return data;
				}

				// Only assign if dumping block variables
				if (_shouldAssign(local, function, data)) {
					return data;
				}
			}

			return null;
		}

		public class VarPosition {
			public int Debug_Index { get; set; }
			public int StackIndex { get; set; }
			public int LocalOffset { get; set; }
			public bool IsLoopControl { get; set; }
			public bool IsLoopIterator { get; set; }
			public bool IsParameter { get; set; }
			public bool IsLoopAssignFirst { get; set; }
			public int LoopLength { get; set; }
			public bool LoopAssigned { get; set; }

			public LubReferenceType Debug_LocalVariable;

			public bool IsLocalAssign(int pc, LubFunction function) {
				return !IsParameter &&
				       !IsLoopControl &&
				       !function.IsVariableInstantiated(Debug_Index);
			}

			public override string ToString() {
				return Debug_LocalVariable.ToString();
			}
		}

		public class StackResolver {
			private readonly Dictionary<int, List<VarPosition>> _s = new Dictionary<int, List<VarPosition>>();

			public List<VarPosition> Fetch(int pc, LubFunction function) {
				if (!_s.ContainsKey(pc)) {
					List<VarPosition> l = new List<VarPosition>();
					_s[pc] = l;
					int localOffset = 0;

					for (int i = 0; i < function.Debug_LocalVariables.Count && function.Debug_LocalVariables[i].StartLine <= pc; i++) {
						LubReferenceType local = function.Debug_LocalVariables[i];

						if (!function.Debug_LocalVariables[i].IsValid(pc)) {
							localOffset++;
							continue;
						}

						var lInfo = LoopInfo.GetLoopInfo(function, local.Key.Value);

						if (lInfo != null) {
							for (int j = 0; j < -lInfo.Start; j++) {
								l.RemoveAt(l.Count - 1);
							}

							i += lInfo.Start;
							int itStart = -1;

							for (int j = 0; j < lInfo.Length; j++, i++, itStart--) {
								if (j == lInfo.IteratorsStart)
									itStart = lInfo.IteratorsLength;

								l.Add(new VarPosition {
									Debug_Index = i,
									Debug_LocalVariable = function.Debug_LocalVariables[i],
									StackIndex = i - localOffset,
									LocalOffset = localOffset,
									IsLoopControl = true,
									IsLoopIterator = itStart > 0,
									IsLoopAssignFirst = j == 0,
									LoopLength = lInfo.Length,
									IsParameter = i < function.NumberOfParametersWithArg
								});
							}

							i--;
							continue;
						}

						l.Add(new VarPosition { Debug_Index = i, Debug_LocalVariable = local, StackIndex = i - localOffset, LocalOffset = localOffset, IsLoopControl = false, IsLoopIterator = false, IsParameter = i < function.NumberOfParametersWithArg });
					}
				}

				return _s[pc];
			}
		}

		public sealed class LoopInfo {
			public int Length { get; set; }
			public int IteratorsLength { get; set; }
			public int IteratorsStart { get; set; }
			public int Start { get; set; }

			public static LoopInfo NumericFor_501 = new LoopInfo { Length = 4, IteratorsStart = 2, IteratorsLength = 2, Start = 0 };
			public static LoopInfo GenericFor_501 = new LoopInfo { Length = 5, IteratorsStart = 3, IteratorsLength = 2, Start = 0 };

			public static LoopInfo NumericFor_500 = new LoopInfo { Length = 3, IteratorsStart = 0, IteratorsLength = 1, Start = -1 };
			public static LoopInfo GenericFor_500 = new LoopInfo { Length = 4, IteratorsStart = 2, IteratorsLength = 2, Start = 0 };

			public static LoopInfo GetLoopInfo(LubFunction function, string value) {
				if (function._decompiler.Header.Version >= 5.1) {
					if (value == "(for generator)")
						return GenericFor_501;
					if (value == "(for index)")
						return NumericFor_501;
				}
				else {
					if (value == "(for generator)")
						return GenericFor_500;
					if (value == "(for index)")
						return NumericFor_500;
					if (value == "(for limit)")
						return NumericFor_500;
				}

				return null;
			}
		}

		public static void VarAssign(StringBuilder builder, LubFunction function) {
			var pc = function.PC;
			var stackData = function.StackResolver.Fetch(pc, function);

			for (int i = 0; i < stackData.Count; i++) {
				var data = stackData[i];
				var local = data.Debug_LocalVariable;

				if (!data.LoopAssigned) {
					if (data.IsLoopAssignFirst) {
						AssignLoopVariables(builder, function, data);
					}

					data.LoopAssigned = true;
				}

				int stackIndex = data.StackIndex;

				if (_shouldAssign(local, function, data)) {
					builder.AppendIndent(function.BaseIndent);
					var res = RegOutput(stackIndex, function);

					if (GetVal(res) == null)
						res = GetKey(res);

					builder.Append(local.Key + " = " + res);
					builder.AppendLine();
					//local.Value = function.Stack[stackIndex];
					function.Stack[stackIndex] = local;
					function.Stack.SetIsAssigned(stackIndex, false);

					if (stackIndex < function.NumberOfParametersWithArg) {
						// We assigned a local variable
						// We must update the usage of the copied reference
						function.Stack.Internal.Where(p => p is LubReferenceType && Equals(((LubReferenceType)p).Key, local.Key)).
							Where(p => p != local).ToList().ForEach(p => ((LubReferenceType)p).Value = local);
					}
				}
			}
		}

		public static void AssignLoopVariables(StringBuilder builder, LubFunction function, VarPosition data) {
			var baseLocals = data.Debug_Index;
			var localOffset = data.LocalOffset;
			var length = data.LoopLength;

			for (int i = baseLocals; i < baseLocals + length && i < function.Debug_LocalVariables.Count; i++) {
				LubReferenceType local = function.Debug_LocalVariables[i];

				if (_toIgnore.ContainsKey(local.Key.Value))
					continue;

				if (function.PC != local.StartLine)
					continue;

				if (function.Stack[i - localOffset] != local)
					local.LoopValue = function.Stack[i - localOffset];

				function.Stack[i - localOffset] = local;
				function.Instantiated[i] = true;
			}
		}

		private static bool _shouldAssign(LubReferenceType local, LubFunction function, VarPosition data) {
			int stackIndex = data.StackIndex;

			if (!function.Stack.GetIsAssigned(stackIndex))
				return false;

			if (GetVal(function.Stack[stackIndex]) == null && local == function.Stack[stackIndex])
				return false;

			if (!function.IsVariableInstantiated(data.Debug_Index))
				return false;

			return true;
		}

		public static void LocalVarInstantiation(StringBuilder builder, LubFunction function) {
			var pc = function.PC;
			var stackData = function.StackResolver.Fetch(pc, function);

			for (int i = function.NumberOfParametersWithArg; i < stackData.Count; i++) {
				var data = stackData[i];
				var local = data.Debug_LocalVariable;

				if (data.IsLoopControl)
					continue;

				if (function.PC >= local.StartLine
				    && !function.IsVariableInstantiated(data.Debug_Index)
					) {
					var v = GetVal(function.Stack[data.StackIndex]);

					if (v is LubString)
						v = new LubOutput("\"" + v + "\"");

					builder.AppendIndent(function.BaseIndent);

					if (v == null) {
						// Only 5.0 does this
						if (function.Stack[data.StackIndex] != local)
							v = GetKey(function.Stack[data.StackIndex]);
					}

					if (v == null)
						builder.AppendLine("local " + local.Key + " = nil");
					else if (v is LubDictionary) {
						builder.Append("local " + local.Key + " = ");
						v.Print(builder, function.BaseIndent);
						builder.AppendLine();
					}
					else
						builder.AppendLine("local " + local.Key + " = " + v);

					function.Instantiated[data.Debug_Index] = true;
					//local.Value = v == null ? null : function.Stack[data.StackIndex];
					function.Stack[data.StackIndex] = local;
					function.Stack.SetIsAssigned(data.StackIndex, false);
				}
			}
		}
	}
}