using System;
using GRF.ContainerFormat;
using GRF.Core;
using GRF.IO;
using Utilities.Extension;

namespace GRF.FileFormats.ThorFormat {
	public class ThorHeader : FileHeader {
		/// <summary>
		/// Initializes a new instance of the <see cref="ThorHeader" /> class.
		/// </summary>
		/// <param name="reader">The reader.</param>
		public ThorHeader(IBinaryReader reader) {
			Magic = reader.StringANSI(24);

			if (Magic != "ASSF (C) 2007 Aeomin DEV")
				throw GrfExceptions.__FileFormatException.Create("THOR");

			UseGrfMerging = reader.Byte() != 0;
			NumberOfFiles = reader.Int32();
			Mode = reader.Int16();

			switch(Mode) {
				case 0x30:
					TargetGrf = reader.String(reader.Byte());
					FileTableCompressedLength = reader.Int32();
					FileTableOffset = reader.Int32();
					DataOffset = reader.Position;
					break;
				case 0x21:
					TargetGrf = reader.String(reader.Byte());
					reader.Forward(1);

					FileTableOffset = reader.Position;
					break;
				default:
					throw GrfExceptions.__FileFormatException2.Create("THOR", "Invalid THOR mode: " + Mode + ".");
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ThorHeader" /> class.
		/// </summary>
		public ThorHeader() {
			Magic = "ASSF (C) 2007 Aeomin DEV";
		}

		/// <summary>
		/// Gets the number of files from the container.
		/// This property is not used.
		/// </summary>
		internal int NumberOfFiles { get; private set; }

		/// <summary>
		/// Gets or sets the mode (or version, unclear). It's used to distinguish between an EXE update or a regular patch.
		/// </summary>
		internal short Mode { get; set; }

		/// <summary>
		/// Gets or sets the length of the compressed file table.
		/// </summary>
		internal int FileTableCompressedLength { get; set; }

		/// <summary>
		/// Gets or sets the offset of the file table in the stream.
		/// </summary>
		internal int FileTableOffset { get; set; }

		/// <summary>
		/// Gets or sets the offset of the data stream.
		/// </summary>
		internal int DataOffset { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the patch should be merged in the base GRF.
		/// </summary>
		public bool UseGrfMerging { get; set; }

		/// <summary>
		/// Gets a value indicating whether the default target GRF should be used.
		/// </summary>
		public bool UseDefaultTargetGrf {
			get { return String.IsNullOrEmpty(TargetGrf); }
		}

		/// <summary>
		/// Gets the target GRF.
		/// </summary>
		public string TargetGrf { get; internal set; }

		internal void Write(ByteWriterStream stream, Container thor) {
			stream.Position = 0;
			stream.WriteAnsi(Magic);

			bool isPatcherOrGameExe = false;

			if (thor.Table.Entries.Count == 1) {
				string firstFile = thor.Table.Entries[0].RelativePath;
				string ext = firstFile.GetExtension();

				if (ext == ".exe") {
					isPatcherOrGameExe = true;
					UseGrfMerging = false;
				}
			}

			stream.WriteByte((byte) (UseGrfMerging ? 1 : 0));
			stream.Write(thor.Table.Entries.Count);

			Mode = (short) (isPatcherOrGameExe ? 0x21 : 0x30);

			stream.Write(Mode);

			if (UseGrfMerging) {
				stream.Write((byte) (TargetGrf ?? "").Length);
				stream.WriteAnsi(TargetGrf ?? "");
			}
			else {
				stream.Write((byte) 0);
				stream.WriteAnsi("");
			}

			if (isPatcherOrGameExe) {
				stream.WriteByte(0x00);
			}
			else {
				stream.Write(FileTableCompressedLength);
				stream.Write(FileTableOffset);
			}
		}
	}
}