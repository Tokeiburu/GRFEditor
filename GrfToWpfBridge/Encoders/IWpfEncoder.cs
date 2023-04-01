using System.IO;
using GRF.Image;

namespace GrfToWpfBridge.Encoders {
	public interface IWpfEncoder {
		GrfImage Frame { get; set; }
		void Save(Stream stream);
	}
}