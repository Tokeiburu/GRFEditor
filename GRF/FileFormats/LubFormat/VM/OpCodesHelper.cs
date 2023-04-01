using System;
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
			//_toIgnore.Add("(for limit)", 0);
			//_toIgnore.Add("(for index)", 0);
			//_toIgnore.Add("(for step)", 0);
			_toIgnore.Add("(for control)", 0);
		}

		public static void AppendParameters(StringBuilder builder, List<int> registers, int stackPointer, LubFunction function) {
			builder.Append("(");

			if (registers[1] == 1) {
			}
			else if (registers[1] == 0) {
				// Validate the call
				ILubObject func = GetKey(RegOutput(stackPointer, function));

				if (func == null) {
					LubErrorHandler.Handle("Expected a function call as the last parameter.", LubSourceError.CodeDecompiler);
				}
				else {
					string temp = GetKey(RegOutput(stackPointer, function)).ToString();
					if (!temp.Contains("(") || !temp.Contains(")")) {
						LubErrorHandler.Handle("Expected a function call as the last parameter.", LubSourceError.CodeDecompiler);
					}
				}

				for (int i = registers[0] + 1; i <= stackPointer; i++) {
					object toPrint = GetKey(RegOutput(i, function));
					builder.Append(toPrint);

					if (i < stackPointer && toPrint != null) {
						builder.Append(", ");
					}
				}
			}
			else {
				for (int i = 1; i < registers[1]; i++) {
					builder.Append(GetKey(RegOutput(registers[0] + i, function)));

					if (i < registers[1] - 1) {
						builder.Append(", ");
					}
				}
			}

			builder.Append(")");
		}

		public static ILubObject RegOrK(int value, LubFunction function) {
			return value >= function.Decompiler.Header.ConstantIndexor ? function.Constants[value - function.Decompiler.Header.ConstantIndexor] : function.Stack[value];
		}

		public static ILubObject RegOutput(int value, LubFunction function) {
			ILubObject acces = function.Stack[value];

			if (acces is LubValueType) {
				LubValueType accessor = (LubValueType) acces;

				if (accessor.Source == LubSourceType.Constant && accessor is LubString) {
					return new LubOutput("\"" + accessor + "\"");
				}
			}

			LubReferenceType reference = acces as LubReferenceType;
			if (reference != null) {
				if (function.InstructionIndex < reference.StartLine) {
					return new LubNull();
				}
			}

			return acces;
		}

		public static ILubObject RegOrKOutput(int value, LubFunction function) {
			ILubObject acces;

			if (value >= function.Decompiler.Header.ConstantIndexor) {
				acces = function.Constants[value - function.Decompiler.Header.ConstantIndexor];
			}
			else {
				acces = function.Stack[value];
			}

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
				return ((LubReferenceType) value).Key;
			}

			LubDictionary lubDictionary = value as LubDictionary;
			if (lubDictionary != null) {
				if (lubDictionary.Count == 0)
					return new LubString("{}");
			}
			return value;
		}

		public static ILubObject GetVal(ILubObject value) {
			return value is LubReferenceType ? ((LubReferenceType) value).Value : value;
		}

		public static T GetKey<T>(ILubObject value) where T : class, ILubObject {
			return value is LubReferenceType ? ((LubReferenceType) value).Key as T : (T) value;
		}

		public static void ForceAssigningLoopVariables(StringBuilder builder, LubFunction function, int baseLocals, int length = 4, bool print = false) {
			for (int i = baseLocals; i < baseLocals + length && i < function.LocalVariables.Count; i++) {
				LubReferenceType local = function.LocalVariables[i];

				try {
					if (_toIgnore.ContainsKey(local.Key.ToString()))
						continue;

					if (!(local.Value == null && function.Stack[i] == null) && (local.Value == null || _nonNull(local.Value) != _nonNull(GetVal(function.Stack[i])))) {
						if (local.Value != null && GetVal(function.Stack[i]) == null) {
							continue;
						}

						if (ReferenceEquals(local, function.Stack[i]))
							continue;

						if (function.InstructionIndex > local.EndLine) {
							continue;
						}

						if (function.InstructionIndex >= local.StartLine) {
							local.Value = function.Stack[i];
							function.Stack[i] = local;

							if (print) {
								builder.AppendIndent(function.FunctionLevel);
								builder.Append(local.Key + " = " + GetKey(local.Value));
								builder.AppendLine();
							}
						}
					}
				}
				catch (Exception) {
					LubErrorHandler.Handle("Generic failure while attempting to assign loop variables.", LubSourceError.CodeDecompiler);
				}
			}
		}

		private static string _nonNull(object obj) {
			if (obj == null) {
				return "__null%";
			}

			return obj.ToString();
		}

		public static void ForceAssigning(StringBuilder builder, LubFunction function) {
			int pointerStackBefore = function.Stack.Pointer;

			//for (int i = function.NumberOfParameters; i < function.LocalVariables.Count; i++) {
			for (int i = 0; i < function.LocalVariables.Count; i++) {
				LubReferenceType local = function.LocalVariables[i];

				if (local.Key.ToString() == "(for generator)") {
					if (function.LocalVariables[i + 2].Key.ToString() == "(for control)") {
						ForceAssigningLoopVariables(builder, function, i, 5, true);
						i += 4;
					}
					else {
						ForceAssigningLoopVariables(builder, function, i, 4, true);
						i += 3;
					}

					continue;
				}

				if (local.Key.ToString() == "(for index)") {
					ForceAssigningLoopVariables(builder, function, i, 4);
					i += 3;
				}

				//if ((local.Value == null || (local.Value.ToString() != GetVal(function.Stack[i]).ToString()))) {
				//    if (GetVal(function.Stack[i]) == null) {
				//        Z.F();
				//    }
				//}

				//if (GetVal(function.Stack[i]) != null && (local.Value == null || (local.Value.ToString() != GetVal(function.Stack[i]).ToString()))) {

				// local.Value == null || (local.Value.ToString() != result.ToString())
				if (_shouldForceAssign(local, function, i)) {
					if (!function.IsVariableInstantiated(local.Key)) {
						continue;
					}

					if (function.InstructionIndex > local.EndLine)
						continue;

					if (GetVal(function.Stack[i]) == null && i < function.NumberOfParameters)
						continue;

					builder.AppendIndent(function.FunctionLevel);
					builder.Append(local.Key + " = " + GetVal(RegOutput(i, function)));
					builder.AppendLine();
					local.Value = function.Stack[i];
					function.Stack[i] = local;

					if (i < function.NumberOfParameters) {
						// We assigned a local variable
						// We must update the usage of the copied reference
						function.Stack.Internal.Where(p => p is LubReferenceType && Equals(((LubReferenceType) p).Key, local.Key)).
							Where(p => p != local).ToList().ForEach(p => ((LubReferenceType) p).Value = local);
					}
				}
			}

			function.Stack.Pointer = pointerStackBefore;
		}

		private static bool _shouldForceAssign(LubReferenceType local, LubFunction function, int i) {
			if (local.Value == null) {
				if (GetVal(function.Stack[i]) == null)
					return false;

				return true;
			}
			if (GetVal(function.Stack[i]) == null) {
				return true;
			}
			return (local.Value.ToString() != GetVal(function.Stack[i]).ToString());
		}

		public static void ForceInstantiation(StringBuilder builder, LubFunction function) {
			for (int i = function.NumberOfParameters; i < function.LocalVariables.Count; i++) {
				LubReferenceType local = function.LocalVariables[i];

				if (local.Key.ToString() == "(for generator)") {
					if (function.LocalVariables[i + 2].Key.ToString() == "(for control)") {
						i += 4;
					}
					else {
						i += 3;
					}
					continue;
				}

				if (local.Key.ToString() == "(for index)") {
					i += 3;
					continue;
				}

				if (function.InstructionIndex >= local.StartLine &&
				    !function.IsVariableInstantiated(local.Key) &&
				    GetVal(function.Stack[i]) != null) {
					//ILubObject obj = GetVal(RegOutput(i, function));

					//if (obj is LubDictionary) {
					//    new LubReferenceType(new LubString("local " + local.Key), obj).Print(builder, function.FunctionLevel);
					//    builder.AppendLine();
					//}
					//else {
					builder.AppendIndent(function.FunctionLevel);
					builder.AppendLine("local " + local.Key + " = " + GetVal(RegOutput(i, function)));
					//}

					function.Instantiated[local.Key] = true;
					local.Value = function.Stack[i];
					function.Stack[i] = local;
				}
			}
		}

		public static void ForceInstantiation2(StringBuilder builder, LubFunction function) {
			for (int i = function.NumberOfParameters; i < function.LocalVariables.Count; i++) {
				LubReferenceType local = function.LocalVariables[i];

				if (local.Key.ToString() == "(for generator)") {
					if (function.LocalVariables[i + 2].Key.ToString() == "(for control)") {
						i += 4;
					}
					else {
						i += 3;
					}
					continue;
				}

				if (local.Key.ToString() == "(for index)") {
					i += 3;
					continue;
				}

				if (!function.IsVariableInstantiated(local.Key) &&
					GetVal(function.Stack[i]) != null) {
					local.Value = function.Stack[i];
					function.Stack[i] = local;
				}
			}
		}

		public static string ForceInstantiation(int value, LubFunction function) {
			if (value < function.Decompiler.Header.ConstantIndexor && value < function.LocalVariables.Count) {
				LubReferenceType local = function.LocalVariables[value];

				if (_toIgnore.ContainsKey(local.Key.ToString()))
					return "";

				if (!function.IsVariableInstantiated(local.Key)) {
					StringBuilder builder = new StringBuilder();
					builder.AppendIndent(function.FunctionLevel);
					builder.Append("local " + local.Key + " = " + GetVal(RegOutput(value, function)));
					builder.AppendLine();
					function.Instantiated[local.Key] = true;
					local.Value = function.Stack[value];
					function.Stack[value] = local;
					return builder.ToString();
				}
			}

			return "";
		}
	}
}