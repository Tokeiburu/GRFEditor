using System;
using System.Collections.Generic;
using System.Text;
using GRF.ContainerFormat;
using GRF.FileFormats.LubFormat.VM;
using GRF.IO;

namespace GRF.FileFormats.LubFormat {
	public class LubHeader : FileHeader {
		public enum LubEndianess {
			BigEndian = 0,
			LittleEndian = 1
		}

		public LubHeader(IBinaryReader reader) {
			Magic = reader.StringANSI(4);

			if (Magic != Encoding.ASCII.GetString(new byte[] { 0x1b, 0x4c, 0x75, 0x61 }, 0, 4))
				throw GrfExceptions.__FileFormatException.Create(Encoding.ASCII.GetString(new byte[] { 0x1b, 0x4c, 0x75, 0x61 }, 0, 4));

			var v = reader.Byte();
			MajorVersion = (byte)((v & 0xF0) >> 4);
			MinorVersion = (byte)(v & 0x0F);

			if (IsCompatibleWith(5, 1)) {
				Format = reader.Byte();
			}

			Endianess = (LubEndianess)reader.Byte();
			SizeOfInt = reader.Byte();
			SizeOfInt64 = reader.Byte();
			SizeOfInstruction = reader.Byte();

			if (IsCompatibleWith(5, 1)) {
				SizeOfOp = 6;
				SizeOfA = 8;
				SizeOfB = 9;
				SizeOfC = 9;
			}
			else if (IsCompatibleWith(5, 0)) {
				SizeOfOp = reader.Byte();
				SizeOfA = reader.Byte();
				SizeOfB = reader.Byte();
				SizeOfC = reader.Byte();
			}

			SizeOfNumbers = reader.Byte();

			if (IsCompatibleWith(5, 1)) {
				IntegralFlag = reader.Byte();
			}
			else if (IsCompatibleWith(5, 0)) {
				SampleNumber1 = reader.Double();
			}

			if (IsCompatibleWith(5, 1)) {
				ConstantIndexor = 256;
				OperandCodeReader = new OperandCodeReader51(this);
				InstructionSet = OpcodeMapper.InstructionSets["0x501"];
			}
			else if (IsCompatibleWith(5, 0)) {
				ConstantIndexor = 250;
				OperandCodeReader = new OperandCodeReader50(this);
				InstructionSet = OpcodeMapper.InstructionSets["0x500"];
			}
		}

		public OperandCodeReader OperandCodeReader { get; private set; }
		public List<Func<OpCodes.AbstractInstruction>> InstructionSet { get; private set; }

		public LubEndianess Endianess { get; set; }
		public int Format { get; set; }
		public int SizeOfInt { get; set; }
		public int SizeOfInt64 { get; set; }
		public int SizeOfInstruction { get; set; }
		public int SizeOfOp { get; set; }
		public int SizeOfA { get; set; }
		public int SizeOfB { get; set; }
		public int SizeOfC { get; set; }
		public int SizeOfNumbers { get; set; }
		public int IntegralFlag { get; set; }
		public double SampleNumber1 { get; set; }
		public double SampleNumber2 { get; set; }
		public int ConstantIndexor { get; set; }
	}

	public abstract class OperandCodeReader {
		protected OperandCodeReader(LubHeader header) {
			_header = header;
			SignRemoval = ((1 << (header.SizeOfB + header.SizeOfC - 1)) - 1);
		}

		public int SizeOfOp { get; internal set; }
		public int SizeOfA { get; internal set; }
		public int SizeOfB { get; internal set; }
		public int SizeOfC { get; internal set; }

		public int LocationRegisterA { get; set; }
		public int LocationRegisterB { get; set; }
		public int LocationRegisterC { get; set; }

		public int ShiftRegisterA { get; set; }
		public int ShiftRegisterB { get; set; }
		public int ShiftRegisterC { get; set; }

		public int SignRemoval { get; set; }

		public int LocationRegister { get; set; }

		protected LubHeader _header { get; set; }

		protected int _getUnsignedValue(int shiftRight, int length, int instruction) {
			return (instruction >> shiftRight) & ((1 << length) - 1);
		}

		public abstract List<int> GetResiters(int instruction, EncodedMode mode);
	}

	public class OperandCodeReader50 : OperandCodeReader {
		public OperandCodeReader50(LubHeader header) : base(header) {
			LocationRegisterA = 0;
			LocationRegisterB = header.SizeOfA;
			LocationRegisterC = header.SizeOfA + header.SizeOfB;

			ShiftRegisterA = 32 - header.SizeOfA - LocationRegisterA;
			ShiftRegisterB = 32 - header.SizeOfB - LocationRegisterB;
			ShiftRegisterC = 32 - header.SizeOfC - LocationRegisterC;
		}

		public override List<int> GetResiters(int instruction, EncodedMode mode) {
			List<int> registers = new List<int>();

			switch(mode) {
				case EncodedMode.ABC:
					registers.Add(_getUnsignedValue(ShiftRegisterA, _header.SizeOfA, instruction));
					registers.Add(_getUnsignedValue(ShiftRegisterB, _header.SizeOfB, instruction));
					registers.Add(_getUnsignedValue(ShiftRegisterC, _header.SizeOfC, instruction));
					break;
				case EncodedMode.ABx:
					registers.Add(_getUnsignedValue(ShiftRegisterA, _header.SizeOfA, instruction));
					registers.Add(_getUnsignedValue(ShiftRegisterC, _header.SizeOfB + _header.SizeOfC, instruction));
					break;
				case EncodedMode.AsBx:
					registers.Add(_getUnsignedValue(ShiftRegisterA, _header.SizeOfA, instruction));
					registers.Add(_getUnsignedValue(ShiftRegisterC, _header.SizeOfB + _header.SizeOfC, instruction) - SignRemoval);
					break;
				case EncodedMode.SBx:
					registers.Add(_getUnsignedValue(ShiftRegisterC, _header.SizeOfB + _header.SizeOfC, instruction) - SignRemoval);
					break;
			}

			return registers;
		}
	}

	public class OperandCodeReader51 : OperandCodeReader {
		public OperandCodeReader51(LubHeader header)
			: base(header) {
			LocationRegisterA = header.SizeOfB + header.SizeOfC;
			LocationRegisterB = 0;
			LocationRegisterC = header.SizeOfB;

			ShiftRegisterA = 32 - header.SizeOfA - LocationRegisterA;
			ShiftRegisterB = 32 - header.SizeOfB - LocationRegisterB;
			ShiftRegisterC = 32 - header.SizeOfC - LocationRegisterC;
		}

		public override List<int> GetResiters(int instruction, EncodedMode mode) {
			List<int> registers = new List<int>();

			switch(mode) {
				case EncodedMode.ABC:
					registers.Add(_getUnsignedValue(ShiftRegisterA, _header.SizeOfA, instruction));
					registers.Add(_getUnsignedValue(ShiftRegisterB, _header.SizeOfB, instruction));
					registers.Add(_getUnsignedValue(ShiftRegisterC, _header.SizeOfC, instruction));
					break;
				case EncodedMode.ABx:
					registers.Add(_getUnsignedValue(ShiftRegisterA, _header.SizeOfA, instruction));
					registers.Add(_getUnsignedValue(ShiftRegisterC, _header.SizeOfB + _header.SizeOfC, instruction));
					break;
				case EncodedMode.AsBx:
					registers.Add(_getUnsignedValue(ShiftRegisterA, _header.SizeOfA, instruction));
					registers.Add(_getUnsignedValue(ShiftRegisterC, _header.SizeOfB + _header.SizeOfC, instruction) - SignRemoval);
					break;
				case EncodedMode.SBx:
					registers.Add(_getUnsignedValue(ShiftRegisterC, _header.SizeOfB + _header.SizeOfC, instruction) - SignRemoval);
					break;
			}

			return registers;
		}
	}
}