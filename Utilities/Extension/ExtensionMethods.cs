using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Utilities.Services;

namespace Utilities.Extension {
	[Flags]
	public enum EscapeMode {
		Normal = 1 << 0,
		LineFeed = 1 << 1,
		KeepAsciiCode = 1 << 2,
		RemoveEscapedAsciiCode = 1 << 3,
		RemoveQuote = 1 << 4,
		RawAscii = 1 << 5,
		All = Normal | LineFeed,
	}

	/// <summary>
	/// ROUtilityTool library class
	/// Common extension methods
	/// </summary>
	public static class ExtensionMethods {
		/// <summary>
		/// Replaces the first.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="oldChar">The old char.</param>
		/// <param name="newChar">The new char.</param>
		/// <returns></returns>
		public static string ReplaceFirst(this String text, string oldChar, string newChar) {
			if (text.StartsWith(oldChar)) {
				return newChar + text.Substring(oldChar.Length);
			}
			return text;
		}

		/// <summary>
		/// Replaces the first.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="oldChar">The old char.</param>
		/// <param name="newChar">The new char.</param>
		/// <param name="compare">The comparison method.</param>
		/// <returns></returns>
		public static string ReplaceFirst(this String text, string oldChar, string newChar, StringComparison compare) {
			if (text.StartsWith(oldChar, compare)) {
				return newChar + text.Substring(oldChar.Length);
			}
			return text;
		}

		public static string RemoveComment(this String text) {
			int start = text.IndexOf("//", System.StringComparison.Ordinal);

			if (start >= 0)
				return text.Substring(0, start);

			return text;
		}

		public static byte[] Substr(this byte[] data, int offset, int length) {
			byte[] ret = new byte[length];
			Buffer.BlockCopy(data, offset, ret, 0, length);
			return ret;
		}

		public static int FindOffset(this byte[] array, int startOffset, byte[] toFind, byte wildCard) {
			int index = startOffset;
			int oldPosition;
			bool done;

			// Trim away wildcards from the pattern
			int pStart = 0;
			int pEnd = toFind.Length;

			for (int i = 0; i < toFind.Length; i++) {
				if (toFind[i] == wildCard)
					pStart++;
				else
					break;
			}

			for (int i = toFind.Length - 1; i >= 0; i--) {
				if (toFind[i] == wildCard)
					pEnd--;
				else
					break;
			}

			toFind = toFind.Substr(pStart, pEnd - pStart);

			if (toFind.Length == 0)
				throw new Exception("Pattern to find is empty.");

			while (index < array.Length) {
				oldPosition = index;
				index = Array.IndexOf(array, toFind[0], oldPosition + 1);

				if (index == -1) {
					index = oldPosition + 1;
				}
				else {
					done = true;
					for (int i = 1; i < toFind.Length; i++) {
						while (toFind[i] == wildCard && i < toFind.Length)
							i++;

						if (array[i + index] != toFind[i]) {
							done = false;
							break;
						}
					}
					if (done)
						return index;
				}
			}

			return -1;
		}

		public static string Unescape(this String text, EscapeMode mode) {
			if (mode == EscapeMode.All)
				return Regex.Unescape(text);

			StringBuilder b = new StringBuilder();
			char c;
			bool removeEscapedAsciiCode = (mode & EscapeMode.RemoveEscapedAsciiCode) == EscapeMode.RemoveEscapedAsciiCode;
			bool keepAscii = (mode & EscapeMode.KeepAsciiCode) == EscapeMode.KeepAsciiCode;
			bool removeQuote = (mode & EscapeMode.RemoveQuote) == EscapeMode.RemoveQuote;

			for (int i = 0; i < text.Length; i++) {
				c = text[i];

				if (c <= 124) {
					switch (c) {
						case '\"':
							if (!removeQuote || (i != 0 && i != text.Length - 1)) b.Append(c);
							break;
						case '\\':
							b.Append(c);

							// Look for the next escaping sequence
							if (removeEscapedAsciiCode && i + 2 < text.Length) {
								c = text[++i];

								if (c == '\\') {
									c = text[++i];

									if (char.IsDigit(c)) {
										b.Append(c);
									}
									else {
										b.Append('\\');
										b.Append(c);
									}
								}
								else {
									b.Append(c);
								}
							}

							if (keepAscii && i + 1 < text.Length) {
								c = text[++i];

								if (char.IsDigit(c)) {
									b.Append('\\');
									b.Append(c);
								}
								else {
									b.Append(c);
								}
							}
							break;
						default:
							b.Append(c);
							break;
					}
				}
				else {
					b.Append(c);
				}
			}

			return Regex.Unescape(b.ToString());
		}

		public static string Escape(this String text, EscapeMode mode) {
			StringBuilder b = new StringBuilder();
			char c;
			bool lineFeed = (mode & EscapeMode.LineFeed) == EscapeMode.LineFeed;
			bool keepAscii = (mode & EscapeMode.KeepAsciiCode) == EscapeMode.KeepAsciiCode;
			bool removeQuote = (mode & EscapeMode.RemoveQuote) == EscapeMode.RemoveQuote;
			bool rawAscii = (mode & EscapeMode.RawAscii) == EscapeMode.RawAscii;

			if (rawAscii) {
				for (int i = 0; i < text.Length; i++) {
					b.Append("\\");
					b.Append((int) text[i]);
				}

				return b.ToString();
			}

			for (int i = 0; i < text.Length; i++) {
				c = text[i];

				if (c <= 124) {
					switch (c) {
						case '\r':
						case '\n':
							if (lineFeed) {
								b.Append("\\");
								b.Append(c);
							}
							else {
								b.Append(c);
							}
							break;
						case '\t':
							b.Append(@"\t");
							break;
						case '\f':
							b.Append(@"\f");
							break;
						case '\"':
							if (!removeQuote || (i != 0 && i != text.Length - 1)) b.Append("\\\"");
							break;
						case '\\':
							if (keepAscii && i + 1 < text.Length) {
								c = text[++i];

								if (char.IsDigit(c)) {
									b.Append('\\');
									b.Append(c);
								}
								else {
									b.Append(@"\\");
									i--;
								}
							}
							else {
								b.Append(@"\\");
							}
							break;
						default:
							b.Append(c);
							break;
					}
				}
				else {
					b.Append(c);
				}
			}

			return b.ToString();
		}

		public static string ToEncoding(this String text, Encoding destination) {
			return EncodingService.FromAnyTo(text, destination);
		}

		public static string ToDisplayEncoding(this String text) {
			return EncodingService.FromAnyToDisplayEncoding(text);
		}

		public static string ToDisplayEncoding(this String text, bool expand) {
			return EncodingService.FromAnyToDisplayEncoding(text, expand);
		}

		public static int ToInt(this string text) {
			return FormatConverters.IntOrHexConverter(text);
		}

		public static long ToLong(this string text) {
			return FormatConverters.LongOrHexConverter(text);
		}

		public static bool IsHexOrDigit(this char c) {
			return
				(c >= '0' && c <= '9') ||
				(c >= 'a' && c <= 'f') ||
				(c >= 'A' && c <= 'F');
		}

		public static bool IsDigit(this char c) {
			return
				(c >= '0' && c <= '9');
		}

		public static string ExpandString(this string text) {
			StringBuilder b = new StringBuilder();

			for (int i = 0; i < text.Length; i++) {
				if (text[i] == '\\' && i + 2 < text.Length && text[i + 1] == '\\' && text[i + 2].IsDigit()) {
					i++;
					int val = text[++i].HexToInt();

					for (int j = i + 1; j < text.Length; j++) {
						if (text[j].IsDigit()) {
							val = val * 10 + text[j].HexToInt();
							i++;
						}
						else
							break;
					}

					b.Append((char)(byte)val);
				}
				else if (text[i] == '\\' && i + 1 < text.Length && text[i + 1].IsDigit()) {
					int val = text[++i].HexToInt();

					for (int j = i + 1; j < text.Length; j++) {
						if (text[j].IsDigit()) {
							val = val * 10 + text[j].HexToInt();
							i++;
						}
						else
							break;
					}

					b.Append((char) (byte) val);
				}
				else {
					b.Append(text[i]);
				}
			}

			return b.ToString();
		}

		public static int HexToInt(this char c) {
			return
				(c >= '0' && c <= '9') ? c - '0' :
				(c >= 'a' && c <= 'f') ? c - 'a' + 10 :
				(c >= 'A' && c <= 'F') ? c - 'A' + 10 : 0;
		}

		public static byte[] ReadAllBytes(this Stream stream) {
			stream.Seek(0, SeekOrigin.Begin);
			byte[] data = new byte[stream.Length];
			stream.Read(data, 0, data.Length);
			return data;
		}

		public static void CopyTo(this Stream fromStream, Stream toStream) {
			if (fromStream == null)
				throw new ArgumentNullException("fromStream");
			if (toStream == null)
				throw new ArgumentNullException("toStream");

			var bytes = new byte[8092];
			int dataRead;
			while ((dataRead = fromStream.Read(bytes, 0, bytes.Length)) > 0)
				toStream.Write(bytes, 0, dataRead);
		}

		//public static HashSet<T> ToHashSet<T>(this IEnumerable<T> list) {
		//	HashSet<T> set = new HashSet<T>();
		//
		//	foreach (var item in list) {
		//		set.Add(item);
		//	}
		//
		//	return set;
		//}

		//public static HashSet<T2> ToHashSet<T, T2>(this Dictionary<T, T2>.ValueCollection list) {
		//	HashSet<T2> set = new HashSet<T2>();
		//
		//	foreach (var item in list) {
		//		set.Add(item);
		//	}
		//
		//	return set;
		//}

		public static string ReplaceAll(this string text, string oldChar, string newChar) {
			if (text == null) return null;

			int len;

			do {
				len = text.Length;
				text = text.Replace(oldChar, newChar);
			} while (text.Length != len);

			return text;
		}

		public static string RemoveBreakLines(this string text) {
			StringBuilder builder = new StringBuilder();

			text = text.Replace("\r\n", "\n");

			for (int i = 0; i < text.Length; i++) {
				if (text[i] == '\n') {
					if (i > 0 && i < text.Length - 1) {
						if (text[i - 1] != ' ' && text[i + 1] != ' ') {
							builder.Append(' ');
						}
					}
				}
				else {
					builder.Append(text[i]);
				}
			}

			return builder.ToString();
		}

		public static string RemoveBreakLines2(this string text) {
			text = text.Replace("\r\n", "\n");
			text = text.Replace("\n", "");

			return text;
		}

		public static int IndexOf(this byte[] array, string toFind) {
			return IndexOf(array, EncodingService.DisplayEncoding.GetBytes(toFind));
		}

		public static int LastIndexOf(this byte[] array, string toFind) {
			return LastIndexOf(array, EncodingService.DisplayEncoding.GetBytes(toFind));
		}

		public static int LastIndexOf(this byte[] array, byte[] toFind) {
			var len = toFind.Length;
			var limit = array.Length - len;

			for (int i = limit; i > -1; i--) {
				if (toFind[0] == array[i]) {
					int k = 0;

					for (; k < len; k++) {
						if (toFind[k] != array[i + k]) break;
					}

					if (k == len) return i;
				}
			}

			return -1;
		}

		public static int IndexOf(this byte[] array, byte[] toFind) {
			var len = toFind.Length;
			var limit = array.Length - len;

			for (int i = 0; i <= limit; i++) {
				if (toFind[0] == array[i]) {
					int k = 0;

					for (; k < len; k++) {
						if (toFind[k] != array[i + k]) break;
					}

					if (k == len) return i;
				}
			}

			return -1;
		}

		public static int IndexOfFirstOrDefault(this List<string> list, string toFind) {
			for (int i = 0; i < list.Count; i++) {
				if (list[i].IndexOf(toFind, 0, StringComparison.Ordinal) == 0)
					return i;
			}

			return -1;
		}

		public static string ReplaceOnce(this String text, string oldChar, string newChar) {
			int pos = text.IndexOf(oldChar, StringComparison.Ordinal);
			if (pos < 0) {
				return text;
			}
			return text.Substring(0, pos) + newChar + text.Substring(pos + oldChar.Length);
		}

		public static string ReplaceExtension(this String text, string newExtension) {
			int index = text.LastIndexOf('.');

			if (index < 0)
				throw new Exception("The path has no extension.");

			return text.Remove(index) + newExtension;
		}

		public static string GetExtension(this String text) {
			if (text == null) return null;

			int index = text.LastIndexOf('.');

			if (index < 0)
				return null;

			if (text.IndexOf('\\', index) > index) {
				return null;
			}

			return text.Substring(index).ToLowerInvariant();
		}

		public static string RemoveDoubleSlashes(this String text) {
			if (text == null) return null;

			int len;

			do {
				len = text.Length;
				text = text.Replace(@"\\", @"\");
			} while (text.Length != len);

			return text;
		}

		public static HashSet<T> ToHashSet<T>(this List<T> items) {
			HashSet<T> set = new HashSet<T>();

			foreach (var item in items) {
				set.Add(item);
			}

			return set;
		}

		public static bool ContainsDoubleSlash(string source) {
			for (int i = 0; i < source.Length - 1; i++) {
				if (source[i] == '\\') {
					i++;

					if (source[i] == '\\') {
						return true;
					}
				}
			}

			return false;
		}

		public static bool ContainsDoubleSlash4(byte[] source) {
			for (int i = 0; i < source.Length - 1; i++) {
				if (source[i] == 92) {
					i++;

					if (source[i] == 92) {
						return true;
					}
				}
			}

			return false;
		}

		public static int FastIndexOf(this string source, string pattern) {
			if (pattern == null) throw new ArgumentNullException();
			if (pattern.Length == 0) return 0;
			if (pattern.Length == 1) return source.IndexOf(pattern[0]);
			bool found;
			int limit = source.Length - pattern.Length + 1;
			if (limit < 1) return -1;
			// Store the first 2 characters of "pattern"
			char c0 = pattern[0];
			char c1 = pattern[1];
			// Find the first occurrence of the first character
			int first = source.IndexOf(c0, 0, limit);
			while (first != -1) {
				// Check if the following character is the same like
				// the 2nd character of "pattern"
				if (source[first + 1] != c1) {
					first = source.IndexOf(c0, ++first, limit - first);
					continue;
				}
				// Check the rest of "pattern" (starting with the 3rd character)
				found = true;
				for (int j = 2; j < pattern.Length; j++)
					if (source[first + j] != pattern[j]) {
						found = false;
						break;
					}
				// If the whole word was found, return its index, otherwise try again
				if (found) return first;
				first = source.IndexOf(c0, ++first, limit - first);
			}
			return -1;
		}

		public static void Move<T>(this List<T> list, int indexFrom, int range, int indexTo) {
			List<T> itemsToMove = list.Skip(indexFrom).Take(range).ToList();
			list.InsertRange(indexTo, itemsToMove);

			if (indexTo < indexFrom) {
				list.RemoveRange(indexFrom + range, range);
			}
			else {
				list.RemoveRange(indexFrom, range);
			}
		}

		public static void Switch<T>(this List<T> list, int indexFrom, int rangeFrom, int indexTo, int rangeTo) {
			int eindexFrom = indexFrom < indexTo ? indexFrom : indexTo;
			int erangeStart = eindexFrom;
			int erangeFrom = indexFrom < indexTo ? rangeFrom : rangeTo;
			int eindexBetween = eindexFrom + erangeFrom;
			int eindexTo = indexFrom < indexTo ? indexTo : indexFrom;
			int erangeTo = indexFrom < indexTo ? rangeTo : rangeFrom;
			int erangeBetween = eindexTo - eindexBetween;
			int eindexEnd = eindexTo + erangeTo;
			int erangeEnd = list.Count - eindexEnd;

			List<T> tmp = new List<T>(list);
			IEnumerable<T> segmentStart = tmp.Take(erangeStart);
			IEnumerable<T> segmentFrom = tmp.TakeRange(eindexFrom, erangeFrom);
			IEnumerable<T> segmentBetween = tmp.TakeRange(eindexBetween, erangeBetween);
			IEnumerable<T> segmentTo = tmp.TakeRange(eindexTo, erangeTo);
			IEnumerable<T> segmentEnd = tmp.TakeRange(eindexEnd, erangeEnd);

			list.Clear();
			list.AddRange(segmentStart.Concat(segmentTo.Concat(segmentBetween.Concat(segmentFrom.Concat(segmentEnd)))));
		}

		public static IEnumerable<T> TakeRange<T>(this IEnumerable<T> list, int indexFrom, int range) {
			return list.Skip(indexFrom).Take(range);
		}

		public static void ReverseMove<T>(this List<T> list, int indexFrom, int range, int indexTo) {
			List<T> itemsToMove;

			if (indexTo < indexFrom) {
				itemsToMove = list.Skip(indexTo).Take(range).ToList();
				list.RemoveRange(indexTo, range);
			}
			else {
				itemsToMove = list.Skip(indexTo - range).Take(range).ToList();
				list.RemoveRange(indexTo - range, range);
			}

			list.InsertRange(indexFrom, itemsToMove);
		}

		public static bool IsExtension(this String text, params string[] exts) {
			string ext = text.GetExtension();
			return exts.Any(p => p == ext);
		}

		public static bool IsStart(this String text, string toFind, int offset, out int lastIndex) {
			if (offset + toFind.Length > text.Length) {
				lastIndex = text.Length;
				return false;
			}

			int size = toFind.Length;

			for (int i = offset, j = 0; i < size; i++, j++) {
				if (text[i] != toFind[j]) {
					lastIndex = i;
					return false;
				}
			}

			lastIndex = offset + toFind.Length;
			return true;
		}

		public static void RemoveRange<T>(this List<T> list, List<T> toRemove) {
			for (int i = 0; i < toRemove.Count; i++) {
				list.Remove(toRemove[i]);
			}
		}

		private static readonly Dictionary<int, string> _indentations = new Dictionary<int, string>();

		public static void AppendIndent(this StringBuilder builder, int level) {
			if (_indentations.ContainsKey(level)) {
				builder.Append(_indentations[level]);
				return;
			}

			string val = "";
			for (int i = 0; i < level; i++) {
				val += '\t';
			}
			_indentations[level] = val;

			builder.Append(val);
		}

		public static int Indent(this StringBuilder builder) {
			int indent = 0;

			for (int i = builder.Length - 1; i >= 0; i--) {
				if (builder[i] == '\t') {
					indent++;
					i--;

					for (; i >= 0; i--) {
						if (builder[i] == '\t')
							indent++;
						else
							break;
					}

					break;
				}
			}

			return indent;
		}

		public static void AppendLineUnix(this StringBuilder builder, string line) {
			builder.Append(line);
			builder.Append('\n');
		}

		public static void AppendLineUnix(this StringBuilder builder) {
			builder.Append('\n');
		}

		/// <summary>
		/// Determines whether the variable has the specified flag.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <param name="value">The value.</param>
		/// <returns>
		///   <c>true</c> if the specified variable has flag; otherwise, <c>false</c>.
		/// </returns>
		/// <exception cref="System.ArgumentNullException">value</exception>
		/// <exception cref="System.ArgumentException"></exception>
		public static bool HasFlags(this Enum variable, Enum value) {
			int num = Convert.ToInt32(value);
			return (Convert.ToInt32(variable) & num) == num;
		}

		public static byte[] Bytes(this String source, int desiredLength, Encoding enc) {
			byte[] toReturn = new byte[desiredLength];
			byte[] sourceBytes = enc.GetBytes(source);
			Buffer.BlockCopy(sourceBytes, 0, toReturn, 0, sourceBytes.Length);
			return toReturn;
		}

		public static void WriteANSI(this BinaryWriter writer, string value, int length, bool forceNullTerminated = false) {
			byte[] data = EncodingService.DisplayEncoding.GetBytes(value);

			// Accomodate for null-terminated byte
			int dataLength;

			if (forceNullTerminated)
				dataLength = Math.Min(data.Length, length - 1);
			else
				dataLength = Math.Min(data.Length, length);

			writer.Write(data, 0, dataLength);

			for (int i = 0; i < (length - dataLength); i++) {
				writer.Write((byte)'\0');
			}
		}

		public static string GetString(this Encoding encoding, byte[] data, int offset, int length, char cut) {
			string temp = encoding.GetString(data, offset, length);
			int index = temp.IndexOf(cut);
			if (index < 0)
				return temp;
			return temp.Substring(0, index);
		}
	}
}
