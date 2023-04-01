using System;
using System.Collections.Generic;

namespace GRF.FileFormats.LubFormat.VM {
	public static class OpcodeMapper {
		private static readonly Dictionary<string, List<Type>> _instructionSet = new Dictionary<string, List<Type>>();

		public static OpCodes.AbstractInstruction GetInstruction(byte[] codeByteData, Lub decompiler) {
			int codeInt = BitConverter.ToInt32(codeByteData, 0);

			int opcode = codeInt & 0x3f;

			if (!_instructionSet.ContainsKey(decompiler.Header.HexVersionFormat)) {
				_instructionSet[decompiler.Header.HexVersionFormat] = GetInstructionsMap(decompiler.Header);
			}

			if (opcode >= _instructionSet[decompiler.Header.HexVersionFormat].Count) {
				LubErrorHandler.Handle("Failed to retrieve the opcode : " + opcode + ".", LubSourceError.LubReader);
			}

			OpCodes.AbstractInstruction instrution = Activate(_instructionSet[decompiler.Header.HexVersionFormat][opcode]);
			instrution.Load(codeInt, decompiler);
			return instrution;
		}

		public static List<Type> GetInstructionsMap(LubHeader header) {
			List<Type> instructions = new List<Type>();

			if (header.Is(5, 0)) {
				instructions.Add(typeof (OpCodes.Move));
				instructions.Add(typeof (OpCodes.LoadK));
				instructions.Add(typeof (OpCodes.LoadBool));
				instructions.Add(typeof (OpCodes.LoadNil));
				instructions.Add(typeof (OpCodes.GetUpVal));
				instructions.Add(typeof (OpCodes.GetGlobal));
				instructions.Add(typeof (OpCodes.GetTable));
				instructions.Add(typeof (OpCodes.SetGlobal));
				instructions.Add(typeof (OpCodes.SetUpVal));
				instructions.Add(typeof (OpCodes.SetTable));
				instructions.Add(typeof (OpCodes.NewTable));
				instructions.Add(typeof (OpCodes.Self));
				instructions.Add(typeof (OpCodes.Add));
				instructions.Add(typeof (OpCodes.Sub));
				instructions.Add(typeof (OpCodes.Mul));
				instructions.Add(typeof (OpCodes.Div));
				instructions.Add(typeof (OpCodes.Pow));
				instructions.Add(typeof (OpCodes.UnaryMinus));
				instructions.Add(typeof (OpCodes.Not));
				instructions.Add(typeof (OpCodes.Concat));
				instructions.Add(typeof (OpCodes.Jmp));
				instructions.Add(typeof (OpCodes.Eq));
				instructions.Add(typeof (OpCodes.Lt));
				instructions.Add(typeof (OpCodes.Le));
				instructions.Add(typeof (OpCodes.TestSet));
				instructions.Add(typeof (OpCodes.Call));
				instructions.Add(typeof (OpCodes.TailCall));
				instructions.Add(typeof (OpCodes.Return));
				instructions.Add(typeof (OpCodes.ForLoop50));
				instructions.Add(typeof (OpCodes.TableForLoop50));
				instructions.Add(typeof (OpCodes.TableForPrep));
				instructions.Add(typeof (OpCodes.SetList50));
				instructions.Add(typeof (OpCodes.SetListTo));
				instructions.Add(typeof (OpCodes.Close));
				instructions.Add(typeof (OpCodes.Closure));

				return instructions;
			}

			if (header.Is(5, 1)) {
				instructions.Add(typeof (OpCodes.Move));
				instructions.Add(typeof (OpCodes.LoadK));
				instructions.Add(typeof (OpCodes.LoadBool));
				instructions.Add(typeof (OpCodes.LoadNil));
				instructions.Add(typeof (OpCodes.GetUpVal));
				instructions.Add(typeof (OpCodes.GetGlobal));
				instructions.Add(typeof (OpCodes.GetTable));
				instructions.Add(typeof (OpCodes.SetGlobal));
				instructions.Add(typeof (OpCodes.SetUpVal));
				instructions.Add(typeof (OpCodes.SetTable));
				instructions.Add(typeof (OpCodes.NewTable));
				instructions.Add(typeof (OpCodes.Self));
				instructions.Add(typeof (OpCodes.Add));
				instructions.Add(typeof (OpCodes.Sub));
				instructions.Add(typeof (OpCodes.Mul));
				instructions.Add(typeof (OpCodes.Div));
				instructions.Add(typeof (OpCodes.Mod));
				instructions.Add(typeof (OpCodes.Pow));
				instructions.Add(typeof (OpCodes.UnaryMinus));
				instructions.Add(typeof (OpCodes.Not));
				instructions.Add(typeof (OpCodes.Len)); //20
				instructions.Add(typeof (OpCodes.Concat));
				instructions.Add(typeof (OpCodes.Jmp));
				instructions.Add(typeof (OpCodes.Eq));
				instructions.Add(typeof (OpCodes.Lt));
				instructions.Add(typeof (OpCodes.Le));
				instructions.Add(typeof (OpCodes.Test));
				instructions.Add(typeof (OpCodes.TestSet));
				instructions.Add(typeof (OpCodes.Call));
				instructions.Add(typeof (OpCodes.TailCall));
				instructions.Add(typeof (OpCodes.Return));
				instructions.Add(typeof (OpCodes.ForLoop51));
				instructions.Add(typeof (OpCodes.ForPrep));
				instructions.Add(typeof (OpCodes.TableForLoop51));
				instructions.Add(typeof (OpCodes.SetList51));
				instructions.Add(typeof (OpCodes.Close));
				instructions.Add(typeof (OpCodes.Closure));
				instructions.Add(typeof (OpCodes.VarArg));

				return instructions;
			}

			throw new Exception("Unsupported lua version.");
		}

		public static OpCodes.AbstractInstruction Activate(Type instruction) {
			return (OpCodes.AbstractInstruction) Activator.CreateInstance(instruction);
		}
	}
}