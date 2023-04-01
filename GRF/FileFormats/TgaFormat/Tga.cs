using System;
using GRF.ContainerFormat;
using GRF.Image;

namespace GRF.FileFormats.TgaFormat {
	/// <summary>
	/// TGA file
	/// Encoded with RGBA format
	/// </summary>
	public class Tga : IImageable {
		/// <summary>
		/// Initializes a new instance of the <see cref="Tga" /> class.
		/// </summary>
		/// <param name="anyData">The data.</param>
		public Tga(MultiType anyData) {
			var dataDecompressed = anyData.Data;

			Header = new TgaHeader(dataDecompressed);

			if (Header.ImageType == 2) {
				int stride = Header.Width * Header.Bits / 8;
				Pixels = new byte[Header.Height * stride];

				for (int i = 0; i < Header.Height; i++) {
					Buffer.BlockCopy(dataDecompressed, (Header.Height - i - 1) * stride + TgaHeader.StructSize, Pixels, i * stride, stride);
				}

				if (Header.Bits == 32) {
					Image = new GrfImage(Pixels, Header.Width, Header.Height, GrfImageType.Bgra32);
				}
				else if (Header.Bits == 24) {
					Image = new GrfImage(Pixels, Header.Width, Header.Height, GrfImageType.Bgr24);
				}
				else {
					throw GrfExceptions.__FileFormatException2.Create("TGA", string.Format(GrfStrings.TgaBitsExpected, Header.Bits));
				}
			}
			else if (Header.ImageType == 3) {	// Grayscale
				int stride = Header.Width * Header.Bits / 8;
				var temp = new byte[Header.Height * stride];

				for (int i = 0; i < Header.Height; i++) {
					Buffer.BlockCopy(dataDecompressed, (Header.Height - i - 1) * stride + TgaHeader.StructSize, temp, i * stride, stride);
				}

				if (Header.Bits == 8) {
					Pixels = new byte[Header.Height * stride * 3];

					for (int i = 0; i < temp.Length; i++) {
						Pixels[3 * i + 0] = temp[i];
						Pixels[3 * i + 1] = temp[i];
						Pixels[3 * i + 2] = temp[i];
					}

					Image = new GrfImage(Pixels, Header.Width, Header.Height, GrfImageType.Bgr24);
				}
				else {
					throw GrfExceptions.__FileFormatException2.Create("TGA", string.Format(GrfStrings.TgaBitsExpected, Header.Bits));
				}
			}
			else if (Header.ImageType >= 8) {
				int increment = Header.Bits / 8;
				Pixels = new byte[Header.Width * Header.Height * increment];
				byte[] data = new byte[dataDecompressed.Length - TgaHeader.StructSize];
				Buffer.BlockCopy(dataDecompressed, TgaHeader.StructSize, data, 0, data.Length);

				int position = 0;

				for (int k = 0; position < Pixels.Length; k++) {
					byte repetitionCount = data[k];
					bool runLenghtPacket = (repetitionCount & 128) == 128;

					if (runLenghtPacket) {
						repetitionCount -= 127;
						byte[] firstPacket = new byte[increment];

						switch(increment) {
							case 1:
								firstPacket[0] = data[++k];
								break;
							case 2:
								firstPacket[0] = data[++k];
								firstPacket[1] = data[++k];
								break;
							case 3:
								firstPacket[0] = data[++k];
								firstPacket[1] = data[++k];
								firstPacket[2] = data[++k];
								break;
							case 4:
								firstPacket[0] = data[++k];
								firstPacket[1] = data[++k];
								firstPacket[2] = data[++k];
								firstPacket[3] = data[++k];
								break;
							default:
								throw GrfExceptions.__UnsupportedPixelFormat.Create(Header.Bits);
						}

						for (int i = 0; i < repetitionCount; i++) {
							switch(increment) {
								case 1:
									Pixels[i * increment + position] = firstPacket[0];
									break;
								case 2:
									Pixels[i * increment + position] = firstPacket[0];
									Pixels[i * increment + position + 1] = firstPacket[1];
									break;
								case 3:
									Pixels[i * increment + position] = firstPacket[0];
									Pixels[i * increment + position + 1] = firstPacket[1];
									Pixels[i * increment + position + 2] = firstPacket[2];
									break;
								case 4:
									Pixels[i * increment + position] = firstPacket[0];
									Pixels[i * increment + position + 1] = firstPacket[1];
									Pixels[i * increment + position + 2] = firstPacket[2];
									Pixels[i * increment + position + 3] = firstPacket[3];
									break;
								default:
									throw GrfExceptions.__UnsupportedPixelFormat.Create(Header.Bits);
							}
						}

						position += increment * repetitionCount;
					}
					else {
						repetitionCount += 1;
						for (int i = 0; i < repetitionCount; i++) {
							switch(increment) {
								case 1:
									Pixels[i * increment + position] = data[++k];
									break;
								case 2:
									Pixels[i * increment + position] = data[++k];
									Pixels[i * increment + position + 1] = data[++k];
									break;
								case 3:
									Pixels[i * increment + position] = data[++k];
									Pixels[i * increment + position + 1] = data[++k];
									Pixels[i * increment + position + 2] = data[++k];
									break;
								case 4:
									Pixels[i * increment + position] = data[++k];
									Pixels[i * increment + position + 1] = data[++k];
									Pixels[i * increment + position + 2] = data[++k];
									Pixels[i * increment + position + 3] = data[++k];
									break;
								default:
									throw GrfExceptions.__UnsupportedPixelFormat.Create(Header.Bits);
							}
						}

						position += repetitionCount * increment;
					}
				}

				if (Header.Bits == 32) {
					Image = new GrfImage(Pixels, Header.Width, Header.Height, GrfImageType.Bgra32);
					Image.Flip(FlipDirection.Vertical);
				}
				else if (Header.Bits == 24) {
					Image = new GrfImage(Pixels, Header.Width, Header.Height, GrfImageType.Bgr24);
					Image.Flip(FlipDirection.Vertical);
				}
				else {
					throw GrfExceptions.__FileFormatException2.Create("TGA", string.Format(GrfStrings.TgaBitsExpected, Header.Bits));
				}
			}
			else {
				throw GrfExceptions.__FileFormatException2.Create("TGA", string.Format(GrfStrings.TgaImageTypeExpected, Header.ImageType));
			}
		}

		/// <summary>
		/// Gets the pixels.
		/// </summary>
		public byte[] Pixels { get; private set; }

		/// <summary>
		/// Gets the header.
		/// </summary>
		public TgaHeader Header { get; private set; }

		/// <summary>
		/// Gets the type.
		/// </summary>
		public GrfImageType Type {
			get { return Image.GrfImageType; }
		}

		#region IImageable Members

		/// <summary>
		/// Gets or sets the image.
		/// </summary>
		public GrfImage Image { get; set; }

		#endregion
	}
}