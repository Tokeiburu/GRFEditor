using System.IO;

namespace GRF.FileFormats {
	public interface IWriteableFile {
		/// <summary>
		/// Gets or sets the loaded file path of this object.
		/// </summary>
		string LoadedPath { get; set; }

		/// <summary>
		/// Saves this object from the LoadedPath.
		/// </summary>
		void Save();

		/// <summary>
		/// Saves this object to the specified file path.
		/// </summary>
		/// <param name="file">The file path.</param>
		void Save(string file);

		/// <summary>
		/// Saves this object to the specified stream.
		/// </summary>
		/// <param name="stream">The stream.</param>
		void Save(Stream stream);
	}

	public interface IWriteableObject {
		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		void Write(BinaryWriter writer);
	}
}