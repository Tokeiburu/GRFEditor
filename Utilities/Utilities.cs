using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using ErrorManager;

namespace Utilities {
	/// <summary>
	/// Used to debug lazily or to quickly set a breakpoint
	/// </summary>
	public static class Z {
		private static readonly Random _rnd = new Random(DateTime.Now.Millisecond);
		private static readonly Dictionary<int, Stopwatch> _watches = new Dictionary<int, Stopwatch>();

		/// <summary>
		/// These are used to stop the debugger.
		/// </summary>
		/// <param name="sender">Any variable or simply none. Useful when the compiler ignores unused variables.</param>
		public static void F(object sender = null) {
		}

		/// <summary>
		/// Starts a stopwatch with the specified ID.
		/// </summary>
		/// <param name="opId">The stopwatch ID.</param>
		public static void Start(int opId) {
			if (_watches.ContainsKey(opId)) {
				_watches[opId].Start();
			}
			else {
				_watches.Add(opId, new Stopwatch());
				_watches[opId].Start();
			}
		}

		/// <summary>
		/// Stops a stopwatch with the specified ID.
		/// </summary>
		/// <param name="opId">The stopwatch ID.</param>
		/// <param name="display">Display the stopwatch duration or not.</param>
		public static void Stop(int opId, bool display = false) {
			if (_watches.ContainsKey(opId)) {
				_watches[opId].Stop();

				if (display) {
					Console.WriteLine("{2}; Elapsed milliseconds : {0}; Elapsed ticks : {1}.", _watches[opId].ElapsedMilliseconds, _watches[opId].ElapsedTicks, "Timer ID = " + opId);
				}
			}
		}

		public static void StopAndDisplayAll() {
			foreach (var watch in _watches.OrderBy(p => p.Key)) {
				watch.Value.Stop();

				Console.WriteLine("{2}; Elapsed milliseconds : {0}; Elapsed ticks : {1}.", watch.Value.ElapsedMilliseconds, watch.Value.ElapsedTicks, "Timer ID = " + watch.Key);
			}
		}

		public static void Delete(int opId) {
			if (_watches.ContainsKey(opId)) {
				_watches.Remove(opId);
			}
		}

		public static string GetCounterDisplay(int opId) {
			if (_watches.ContainsKey(opId)) {
				return _watches[opId].ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture);
			}

			return "0";
		}

		public static int GetRandomInteger(int from, int to) {
			return (_rnd.Next() % (to - @from)) + @from;
		}

		public static bool CounterExists(int opId) {
			return _watches.ContainsKey(opId);
		}
	}

	public class Description : Attribute {
		public string Text { get; private set; }

		public Description(string text) {
			Text = text;
		}

		/// <summary>
		/// Gets the description, returns null if no description has been found.
		/// </summary>
		/// <param name="en">The enum.</param>
		/// <returns></returns>
		public static string GetDescriptionOrNull(Enum en) {
			Type type = en.GetType();

			MemberInfo[] memInfo = type.GetMember(en.ToString());

			if (memInfo.Length > 0) {
				object[] attrs = memInfo[0].GetCustomAttributes(typeof(Description), false);
				if (attrs.Length > 0)
					return ((Description)attrs[0]).Text;
			}

			return null;
		}

		/// <summary>
		/// Gets the description, returns the member name if no description has been found.
		/// </summary>
		/// <param name="en">The enum.</param>
		/// <returns></returns>
		public static string GetDescription(Enum en) {
			Type type = en.GetType();

			MemberInfo[] memInfo = type.GetMember(en.ToString());

			if (memInfo.Length > 0) {
				object[] attrs = memInfo[0].GetCustomAttributes(typeof(Description), false);
				if (attrs.Length > 0)
					return ((Description)attrs[0]).Text;
			}

			return en.ToString();
		}

		/// <summary>
		/// Gets the description attribute for any type.
		/// </summary>
		/// <param name="en">The en.</param>
		/// <returns></returns>
		public static string GetAnyDescription(Type en) {
			foreach (object attrib in en.GetCustomAttributes(true)) {
				if (attrib is Description)
					return ((Description) attrib).Text;
			}

			return en.ToString();
		}
	}

	public static class Debug {
		public static void Ignore(Action action) {
			try { action.Invoke(); } catch { }
		}

		public static void Time(Action action) {
			int timerId = Z.GetRandomInteger(0, Int32.MaxValue);
			Z.Start(timerId);
			action();
			Z.Stop(timerId);
		}

		public static void PrintStack() {
			try {
				StackTrace stack = new StackTrace();
				Console.WriteLine(stack);
				throw new Exception();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public static void PrintStack(Exception err) {
			StackTrace stack = new StackTrace();
			Console.WriteLine(stack);
			ErrorHandler.HandleException(err);
		}
	}

	public static class GZip {
		public static byte[] Decompress(byte[] data) {
			using (GZipStream stream = new GZipStream(new MemoryStream(data), CompressionMode.Decompress)) {
				const int size = 4096;
				byte[] buffer = new byte[size];
				using (MemoryStream memory = new MemoryStream()) {
					int count = 0;
					do {
						count = stream.Read(buffer, 0, size);
						if (count > 0) {
							memory.Write(buffer, 0, count);
						}
					}
					while (count > 0);
					return memory.ToArray();
				}
			}
		}

		public static byte[] Compress(byte[] data) {
			using (MemoryStream memory = new MemoryStream()) {
				using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true)) {
					gzip.Write(data, 0, data.Length);
				}
				return memory.ToArray();
			}
		}
	}

	public static class Methods {
		public const string AdvSeperator = "__%Separator%";

		public static HashSet<int> GetRange(string querry, int max) {
			List<string> rangeQuerries = querry.Split(';', ',').Select(p => p.Trim()).ToList();
			HashSet<int> predicates = new HashSet<int>();

			foreach (string rangeQuerry in rangeQuerries) {
				try {
					if (rangeQuerry.StartsWith("-")) {
						string querryPredicate = rangeQuerry;
						int high = Int32.Parse(querryPredicate.Substring(1));

						for (int i = 0; i <= high; i++) {
							predicates.Add(i);
						}
					}
					else if (rangeQuerry.Contains("-")) {
						string querryPredicate = rangeQuerry;
						int low = Int32.Parse(querryPredicate.Split('-')[0]);
						int high = Int32.Parse(querryPredicate.Split('-')[1]);

						for (int i = low; i <= high; i++) {
							predicates.Add(i);
						}
					}
					else if (rangeQuerry.EndsWith("+")) {
						string querryPredicate = rangeQuerry;
						int low = Int32.Parse(querryPredicate.Substring(0, rangeQuerry.Length - 1));

						for (int i = low; i <= max; i++) {
							predicates.Add(i);
						}
					}
					else {
						string querryPredicate = rangeQuerry;
						int middle = Int32.Parse(querryPredicate);

						predicates.Add(middle);
					}
				}
				catch { }
			}

			return predicates;
		}

		public static int LevenshteinDistance(string s, string t) {
			int n = s.Length;
			int m = t.Length;
			int[,] d = new int[n + 1,m + 1];
			if (n == 0) {
				return m;
			}
			if (m == 0) {
				return n;
			}
			for (int i = 0; i <= n; d[i, 0] = i++)
				;
			for (int j = 0; j <= m; d[0, j] = j++)
				;
			for (int i = 1; i <= n; i++) {
				for (int j = 1; j <= m; j++) {
					int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
					d[i, j] = Math.Min(
						Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
						d[i - 1, j - 1] + cost);
				}
			}
			return d[n, m];
		}

		public static string ClosestString(string source, IList<string> values) {
			int low = Int32.MaxValue;
			int index = -1;

			for (int j = 0; j < values.Count; j++) {
				int dist = LevenshteinDistance(source, values[j]);

				if (dist < low) {
					low = dist;
					index = j;
				}
			}

			if (index < 0)
				return null;

			return values[index];
		}

		private static bool? _canUseIndexed8;

		private static string _applicationPath;
		private static string _applicationFullPath;

		public static bool CanUseIndexed8 {
			get {
				if (_canUseIndexed8 == null) {
					_canUseIndexed8 = Environment.OSVersion.Version.Major >= 6;
				}

				return _canUseIndexed8 == true;
			}
		}

		public static string ApplicationPath {
			get { return _applicationPath ?? (_applicationPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)); }
		}

		public static string ApplicationFullPath {
			get { return _applicationFullPath ?? (_applicationFullPath = Process.GetCurrentProcess().MainModule.FileName.Replace("vshost.", "")); }
		}

		private const string _emptyStringIdentifier = "__%EmptyString%";
		private const string _nullStringIdentifier = "__%NullString%";

		public static bool HasWriteAccessToFolder(string path) {
			try {
				path = path ?? Directory.GetCurrentDirectory();

				System.Security.AccessControl.DirectorySecurity ds = Directory.GetAccessControl(path);
				return true;
			}
			catch (UnauthorizedAccessException) {
				return false;
			}
		}

		public static IntPtr LockFile(string file) {
			NativeMethods.OFSTRUCT st;
			return NativeMethods.OpenFile(file, out st, NativeMethods.OpenFileStyle.OF_READ);
		}

		public static bool IsFileLocked(string file) {
			try {
				using (new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
				}

				return false;
			}
			catch {
				return true;
			}
		}

		public static string ListToString(List<string> items, char cut = ',') {
			return ListToString(items, cut.ToString(CultureInfo.InvariantCulture));
		}

		public static string ListToString(List<string> items, string cut) {
			StringBuilder builder = new StringBuilder();

			for (int index = 0; index < items.Count; index++) {
				string item = items[index];

				if (item == null) {
					builder.Append(_nullStringIdentifier);
				}
				else if (item == "") {
					builder.Append(_emptyStringIdentifier);
				}
				else {
					builder.Append(item);
				}

				if (index != items.Count - 1) {
					builder.Append(cut);
				}
			}

			return builder.ToString();
		}
		private static readonly Random _random = new Random((int)DateTime.Now.Ticks);//thanks to McAden
		public static string RandomString(int size) {
			byte[] bytes = new byte[size];

			for (int i = 0, count = size / 8; i < count; i++) {
				Buffer.BlockCopy(BitConverter.GetBytes(_random.NextDouble()), 0, bytes, 8 * i, 8);
			}

			StringBuilder builder = new StringBuilder();

			for (int i = 0; i < size; i++) {
				builder.Append((char) (bytes[i] % 26 + 'A'));
			}

			return builder.ToString();
		}
		public static List<string> StringToList(string item, char cut = ',') {
			if (item.Length == 0) {
				return new List<string>();
			}
			else {
				List<string> items = new List<string>();

				foreach (string value in item.Split(cut)) {
					if (value == _emptyStringIdentifier) {
						items.Add("");
					}
					else if (value == _nullStringIdentifier) {
						items.Add(null);
					}
					else {
						items.Add(value);
					}
				}

				return items;
			}
		}

		public static string GetReadableRuntimeVersion() {
			return GetReadableRuntimeVersion(Path.GetFileName(RuntimeEnvironment.GetSystemVersion()));
		}
		public static string GetReadableRuntimeVersion(string version, bool lastTry = false) {
			try {
				if (version == null)
					return "";

				string cleanedVersion = version.Where(t => t == '.' || (t >= '0' && t <= '9')).Aggregate("", (current, t) => current + t);

				switch (cleanedVersion) {
					case "1.0.2204.21":
						return "1.0 Beta 1";
					case "1.0.2914.0":
						return "1.0 Beta 2";
					case "1.0.3705.0":
						return "1.0 RTM";
					case "1.0.3705.209":
						return "1.0 SP1";
					case "1.0.3705.288":
						return "1.0 SP2";
					case "1.0.3705.6018":
						return "1.0 SP3";
					case "1.1.4322.510":
						return "1.1 Final Beta";
					case "1.1.4322.573":
						return "1.1 RTM";
					case "1.1.4322.2032":
						return "1.1 SP1";
					case "1.1.4322.2300":
						return "1.1 SP1 (Server 2003)";
					case "1.1.4322.2310":
						return "1.1 (KB893251)";
					case "1.1.4322.2407":
						return "1.1 (KB927495)";
					case "1.1.4322.2443":
						return "1.1 (KB953297)";
					case "2.0.40607.16":
						return "2.0 Beta 1";
					case "2.0.50215.44":
						return "2.0 Beta 2";
					case "2.0.50727.42":
						return "2.0 RTM (x86)";
					case "2.0.50727.312":
						return "2.0 RTM (Vista)";
					case "2.0.50727.832":
						return "2.0 (KB928365)";
					case "2.0.50727.1433":
						return "2.0 SP1 (x86)";
					case "2.0.50727.1434":
						return "2.0 SP1 (Server 2008 andVista SP1)";
					case "2.0.50727.3053":
						return "2.0 SP2 (installed with 3.5 SP1)";
					case "2.0.50727.3082":
						return "2.0 SP2 (x86) (installed with3.5 Family Update)";
					case "2.0.50727.3603":
						return "2.0 SP2 (KB974417)";
					case "2.0.50727.3607":
						return "2.0 SP2 (KB976569)";
					case "2.0.50727.3615":
						return "2.0 SP2 (KB983583)";
					case "2.0.50727.4016":
						return "2.0 SP2 (Windows Vista SP2 / Server 2008 SP2)";
					case "2.0.50727.4918":
						return "2.0 SP2 (installed with Windows 7 RC)";
					case "2.0.50727.4927":
						return "2.0 SP2 (installed with Windows 7 RTM)";
					case "2.0.50727.4952":
						return "2.0 SP2 (KB983590)";
					case "3.0.4506.30":
						return "3.0 RTM";
					case "3.0.4506.26":
						return "3.0 RTM (Vista)";
					case "3.0.4506.590":
						return "3.0 SP1 Beta";
					case "3.0.4506.648":
						return "3.0 SP1";
					case "3.0.4506.2123":
						return "3.0 SP2";
					case "3.0.4506.2254":
						return "3.0 SP2 (installed with 3.5 SP1)";
					case "3.5.20404.0":
						return "3.5 Beta 1";
					case "3.5.20706.1":
						return "3.5 Beta 2";
					case "3.5.21022.8":
						return "3.5 RTM";
					case "3.5.30428.1":
						return "3.5 SP1 Beta";
					case "3.5.30729.01":
						return "3.5 SP1 RTM";
					case "3.5.30729.4926":
						return "3.5 SP1 (Windows 7 Edition)";
					case "4.0.20506":
						return "4.0 Beta 1";
					case "4.0.21006":
						return "4.0 Beta 2";
					case "4.0.30128.1":
						return "4.0 RC";
					case "4.0.30319.1":
						return "4.0 RTM";
					case "4.5.40805":
						return "4.5 Developer Preview";
					case "4.0.30319.17020":
						return "4.5 Developer Preview";
					case "4.5.50131":
						return "4.5 Beta (Consumer Preview)";
					case "4.0.30319.17379":
						return "4.5 Beta (Consumer Preview)";
					case "4.5.50501":
						return "4.5 RC (Release Preview)";
					case "4.0.30319.17626":
						return "4.5 RC (Release Preview)";
					case "4.5.50709":
						return "4.5 RTM";
					case "4.0.30319.17929":
						return "4.5 RTM";
					default:
						return GetCloseReadableRuntimeVersion(cleanedVersion);
				}
			}
			catch {
				return "Unknown .NET Framework";
			}
		}
		public static string GetCloseReadableRuntimeVersion(string cleanedVersion) {
			if ("1.0.2204.21".StartsWith(cleanedVersion))
				return "1.0 Beta 1";
			if ("1.0.2914.0".StartsWith(cleanedVersion))
				return "1.0 Beta 2";
			if ("1.0.3705.0".StartsWith(cleanedVersion))
				return "1.0 RTM";
			if ("1.0.3705.209".StartsWith(cleanedVersion))
				return "1.0 SP1";
			if ("1.0.3705.288".StartsWith(cleanedVersion))
				return "1.0 SP2";
			if ("1.0.3705.6018".StartsWith(cleanedVersion))
				return "1.0 SP3";
			if ("1.1.4322.510".StartsWith(cleanedVersion))
				return "1.1 Final Beta";
			if ("1.1.4322.573".StartsWith(cleanedVersion))
				return "1.1 RTM";
			if ("1.1.4322.2032".StartsWith(cleanedVersion))
				return "1.1 SP1";
			if ("1.1.4322.2300".StartsWith(cleanedVersion))
				return "1.1 SP1 (Server 2003)";
			if ("1.1.4322.2310".StartsWith(cleanedVersion))
				return "1.1 (KB893251)";
			if ("1.1.4322.2407".StartsWith(cleanedVersion))
				return "1.1 (KB927495)";
			if ("1.1.4322.2443".StartsWith(cleanedVersion))
				return "1.1 (KB953297)";
			if ("2.0.40607.16".StartsWith(cleanedVersion))
				return "2.0 Beta 1";
			if ("2.0.50215.44".StartsWith(cleanedVersion))
				return "2.0 Beta 2";
			if ("2.0.50727.42".StartsWith(cleanedVersion))
				return "2.0 RTM (x86)";
			if ("2.0.50727.312".StartsWith(cleanedVersion))
				return "2.0 RTM (Vista)";
			if ("2.0.50727.832".StartsWith(cleanedVersion))
				return "2.0 (KB928365)";
			if ("2.0.50727.1433".StartsWith(cleanedVersion))
				return "2.0 SP1 (x86)";
			if ("2.0.50727.1434".StartsWith(cleanedVersion))
				return "2.0 SP1 (Server 2008 andVista SP1)";
			if ("2.0.50727.3053".StartsWith(cleanedVersion))
				return "2.0 SP2 (installed with 3.5 SP1)";
			if ("2.0.50727.3082".StartsWith(cleanedVersion))
				return "2.0 SP2 (x86) (installed with3.5 Family Update)";
			if ("2.0.50727.3603".StartsWith(cleanedVersion))
				return "2.0 SP2 (KB974417)";
			if ("2.0.50727.3607".StartsWith(cleanedVersion))
				return "2.0 SP2 (KB976569)";
			if ("2.0.50727.3615".StartsWith(cleanedVersion))
				return "2.0 SP2 (KB983583)";
			if ("2.0.50727.4016".StartsWith(cleanedVersion))
				return "2.0 SP2 (Windows Vista SP2 / Server 2008 SP2)";
			if ("2.0.50727.4918".StartsWith(cleanedVersion))
				return "2.0 SP2 (installed with Windows 7 RC)";
			if ("2.0.50727.4927".StartsWith(cleanedVersion))
				return "2.0 SP2 (installed with Windows 7 RTM)";
			if ("2.0.50727.4952".StartsWith(cleanedVersion))
				return "2.0 SP2 (KB983590)";
			if ("3.0.4506.30".StartsWith(cleanedVersion))
				return "3.0 RTM";
			if ("3.0.4506.26".StartsWith(cleanedVersion))
				return "3.0 RTM (Vista)";
			if ("3.0.4506.590".StartsWith(cleanedVersion))
				return "3.0 SP1 Beta";
			if ("3.0.4506.648".StartsWith(cleanedVersion))
				return "3.0 SP1";
			if ("3.0.4506.2123".StartsWith(cleanedVersion))
				return "3.0 SP2";
			if ("3.0.4506.2254".StartsWith(cleanedVersion))
				return "3.0 SP2 (installed with 3.5 SP1)";
			if ("3.5.20404.0".StartsWith(cleanedVersion))
				return "3.5 Beta 1";
			if ("3.5.20706.1".StartsWith(cleanedVersion))
				return "3.5 Beta 2";
			if ("3.5.21022.8".StartsWith(cleanedVersion))
				return "3.5 RTM";
			if ("3.5.30428.1".StartsWith(cleanedVersion))
				return "3.5 SP1 Beta";
			if ("3.5.30729.01".StartsWith(cleanedVersion))
				return "3.5 SP1 RTM";
			if ("3.5.30729.4926".StartsWith(cleanedVersion))
				return "3.5 SP1 (Windows 7 Edition)";
			if ("4.0.20506".StartsWith(cleanedVersion))
				return "4.0 Beta 1";
			if ("4.0.21006".StartsWith(cleanedVersion))
				return "4.0 Beta 2";
			if ("4.0.30128.1".StartsWith(cleanedVersion))
				return "4.0 RC";
			if ("4.0.30319.1".StartsWith(cleanedVersion))
				return "4.0 RTM";
			if ("4.5.40805".StartsWith(cleanedVersion))
				return "4.5 Developer Preview";
			if ("4.0.30319.17020".StartsWith(cleanedVersion))
				return "4.5 Developer Preview";
			if ("4.5.50131".StartsWith(cleanedVersion))
				return "4.5 Beta (Consumer Preview)";
			if ("4.0.30319.17379".StartsWith(cleanedVersion))
				return "4.5 Beta (Consumer Preview)";
			if ("4.5.50501".StartsWith(cleanedVersion))
				return "4.5 RC (Release Preview)";
			if ("4.0.30319.17626".StartsWith(cleanedVersion))
				return "4.5 RC (Release Preview)";
			if ("4.5.50709".StartsWith(cleanedVersion))
				return "4.5 RTM";
			if ("4.0.30319.17929".StartsWith(cleanedVersion))
				return "4.5 RTM";

			return "Unknown .NET Framework";
		}
		public static string CutFileName(string fileName, int cut = 60) {
			try {
				if (fileName.Length < cut)
					return fileName;

				List<string> folders = fileName.Split('\\').ToList();

				bool hasCut = false;
				while (folders.Sum(p => p.Length + 1) > cut && folders.Count >= 3) {
					folders.RemoveAt(1);
					hasCut = true;
				}

				if (!hasCut)
					return fileName;

				string toReturn;
				if (folders.Count > 2) {
					toReturn = folders[1];
					for (int i = 2; i < folders.Count; i++) {
						toReturn = Path.Combine(toReturn, folders[i]);
					}
					toReturn = folders[0] + "\\...\\" + toReturn;
				}
				else if (folders.Count == 2) {
					toReturn = folders[0] + "\\...\\" + folders[1];
				}
				else {
					toReturn = folders[0];
				}

				return toReturn;
			}
			catch {
				return fileName;
			}
		}
		public static string FileSizeToString(long fizeSize) {
			if (fizeSize < 1024) {
				return fizeSize + " B";
			}
			if (fizeSize < (1024 * 1024)) {
				float size = fizeSize / 1024f;

				return size < 10 ? String.Format("{0:0.00} KB", size) : String.Format(size < 100 ? "{0:0.0} KB" : "{0:0} KB", size);
			}
			else {
				float size = fizeSize / (float)(1024 * 1024);

				return size < 10 ? String.Format("{0:0.00} MB", size) : String.Format(size < 100 ? "{0:0.0} MB" : "{0:0} MB", size);
			}
		}
		public static string FileSizeToString(ulong fizeSize) {
			if (fizeSize < 1024) {
				return fizeSize + " B";
			}
			if (fizeSize < (1024 * 1024)) {
				float size = fizeSize / 1024f;

				return size < 10 ? String.Format("{0:0.00} KB", size) : String.Format(size < 100 ? "{0:0.0} KB" : "{0:0} KB", size);
			}
			if (fizeSize < (1024 * 1024 * 1024)) {
				float size = fizeSize / (float)(1024 * 1024);

				return size < 10 ? String.Format("{0:0.00} MB", size) : String.Format(size < 100 ? "{0:0.0} MB" : "{0:0} MB", size);
			}
			else {
				float size = fizeSize / (float)(1024 * 1024 * 1024);

				return size < 10 ? String.Format("{0:0.00} GB", size) : String.Format(size < 100 ? "{0:0.0} GB" : "{0:0} GB", size);
			}
		}
		public static bool CanWriteTo(string path) {
			try {
				path = path ?? Directory.GetCurrentDirectory();

				using (new FileStream(Path.Combine(path, "util.test"), FileMode.Create, FileAccess.ReadWrite)) { }

				try {
					File.Delete(Path.Combine(path, "util.test"));
				}
				catch { }

				return true;
			}
			catch {
				return false;
			}
		}
		public static string WildcardToRegex(string pattern) {
			return "^" + Regex.Escape(pattern)
							  .Replace(@"\*", ".*")
							  .Replace(@"\?", ".")
					   + "$";
		}
		public static string WildcardToRegexLine(string pattern) {
			return Regex.Escape(pattern)
							  .Replace(@"\*", ".*")
							  .Replace(@"\?", ".");
		}
		public static string CompactPath(string longPathName, int wantedLength) {
			StringBuilder sb = new StringBuilder(wantedLength + 1);
			NativeMethods.PathCompactPathEx(sb, longPathName, wantedLength + 1, 0);
			return sb.ToString();
		}
		public static byte[] StringToByteArray(string hex) {
			return Enumerable.Range(0, hex.Length)
							 .Where(x => x % 2 == 0)
							 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
							 .ToArray();
		}
		public static string ByteArrayToString(byte[] data) {
			StringBuilder sb = new StringBuilder(data.Length * 2);

			for (int index = 0, length = data.Length; index < length; index++) {
				sb.AppendFormat("{0:x2}", data[index]);
			}

			return sb.ToString();
		}
		public static string ObjectToStringHash(object obj) {
			return ByteArrayToString(BitConverter.GetBytes(obj.GetHashCode()));
		}

		public static bool ByteArrayCompare(byte[] b1, byte[] b2) {
			if (b1 == null && b2 == null)
				return true;
			if (b1 == null || b2 == null)
				return false;
			return b1.Length == b2.Length && NativeMethods.memcmp(b1, b2, b1.Length) == 0;
		}
		public static bool ByteArrayCompare(byte[] b1, int offset1, int length, byte[] b2, int offset2) {
			byte[] a1 = new byte[length];
			byte[] a2 = new byte[length];

			Buffer.BlockCopy(b1, offset1, a1, 0, length);
			Buffer.BlockCopy(b2, offset2, a2, 0, length);

			return a1.Length == a2.Length && NativeMethods.memcmp(a1, a2, length) == 0;
		}
		public static int ByteArrayCompareToInt(byte[] b1, byte[] b2) {
			if (b1 == null && b2 == null)
				return 0;
			if (b1 == null)
				return 1;
			if (b2 == null)
				return -1;
			return b1.Length != b2.Length ? 0 : NativeMethods.memcmp(b1, b2, b1.Length);
		}
		public static string SecondsToString(long seconds) {
			TimeSpan t = TimeSpan.FromSeconds(seconds);

			if (t.Hours > 0) {
				return string.Format("{0:D} hour{3}, {1:D} minute{4} and {2:D} second{5}",
				t.Hours,
				t.Minutes,
				t.Seconds,
				t.Hours > 1 ? "s" : "",
				t.Minutes > 1 ? "s" : "",
				t.Seconds > 1 ? "s" : "");
			}
			else if (t.Minutes > 0) {
				return string.Format("{0:D} minute{2} and {1:D} second{3}",
				t.Minutes,
				t.Seconds,
				t.Minutes > 1 ? "s" : "",
				t.Seconds > 1 ? "s" : "");
			}
			else {
				return string.Format("{0:D} second{1}",
				t.Seconds,
				t.Seconds > 1 ? "s" : "");
			}
		}

		public static string Aggregate(IList list, string s) {
			StringBuilder toReturn = new StringBuilder();

			for (int i = 0; i < list.Count; i++) {
				toReturn.Append(list[i]);

				if (i < list.Count - 1) {
					toReturn.Append(s);
				}
			}

			return toReturn.ToString();
		}

		public static string TypeToString(object obj) {
			string value = obj.GetType().ToString();
			int last = value.LastIndexOf('.');

			if (last > 0)
				return value.Substring(last + 1, value.Length - last - 1).Replace("+", ".");
			return value;
		}

		public static bool IsWinXP() {
			OperatingSystem os = Environment.OSVersion;
			return (os.Platform == PlatformID.Win32NT) && (os.Version.Major == 5 && os.Version.Minor >= 1);
		}

		public static bool IsWinXPOrHigher() {
			OperatingSystem os = Environment.OSVersion;
			return (os.Platform == PlatformID.Win32NT) && ((os.Version.Major > 5) || ((os.Version.Major == 5) && (os.Version.Minor >= 1)));
		}

		public static bool IsWinVistaOrHigher() {
			OperatingSystem os = Environment.OSVersion;
			return (os.Platform == PlatformID.Win32NT) && (os.Version.Major >= 6);
		}

		public static int Align(int size) {
			return size % 8 > 0 ? (8 - size % 8) + size : size;
		}

		public static byte[] Copy(byte[] input) {
			byte[] data = new byte[input.Length];
			Buffer.BlockCopy(input, 0, data, 0, data.Length);
			return data;
		}

		public static void FileModified(string path, string filter, Action<object, FileSystemEventArgs> method) {
			FileSystemWatcher fsw = new FileSystemWatcher();

			fsw.Path = path ?? Directory.GetCurrentDirectory();

			fsw.Filter = filter;
			fsw.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
			fsw.Changed += (sender, e) => _fileChanged(sender, e, fsw, method);
			fsw.Created += (sender, e) => _fileChanged(sender, e, fsw, method);
			fsw.Renamed += (sender, e) => _fileChanged(sender, e, fsw, method);
			fsw.Deleted += (sender, e) => _fileChanged(sender, e, fsw, method);
			fsw.EnableRaisingEvents = true;
		}

		private static void _fileChanged(object sender, FileSystemEventArgs e, FileSystemWatcher fsw, Action<object, FileSystemEventArgs> method) {
			try {
				// Raising events is turned off to avoid 
				// receiving the event twice.
				fsw.EnableRaisingEvents = false;
				method(sender, e);
			}
			finally {
				fsw.EnableRaisingEvents = true;
			}
		}

		public static string ByteArrayToStringInt(byte[] computeByteHash) {
			StringBuilder builder = new StringBuilder();

			if (computeByteHash.Length == 0) return "";

			for (int index = 0; index < computeByteHash.Length - 1; index++) {
				byte b = computeByteHash[index];
				builder.Append(b);
				builder.Append("-");
			}

			return builder.ToString() + computeByteHash.Last();
		}

		public static string StringLimit(string text, int count) {
			if (text.Length <= count) return text;
			return text.Substring(0, count);
		}
	}

	public static class Wow {
		public static bool Is64BitProcess {
			get { return IntPtr.Size == 8; }
		}

		public static bool Is64BitOperatingSystem {
			get {
				// Clearly if this is a 64-bit process we must be on a 64-bit OS.
				if (Is64BitProcess)
					return true;
				// Ok, so we are a 32-bit process, but is the OS 64-bit?
				// If we are running under Wow64 than the OS is 64-bit.
				bool isWow64;
				return ModuleContainsFunction("kernel32.dll", "IsWow64Process") && NativeMethods.IsWow64Process(NativeMethods.GetCurrentProcess(), out isWow64) && isWow64;
			}
		}

		static bool ModuleContainsFunction(string moduleName, string methodName) {
			IntPtr hModule = NativeMethods.GetModuleHandle(moduleName);
			if (hModule != IntPtr.Zero)
				return NativeMethods.GetProcAddress(hModule, methodName) != IntPtr.Zero;
			return false;
		}
	}

	//public static class DataConverter {
	//    public static int ToInt32(byte[] data, int offset) {
	//        return data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24);
	//    }
	//}
}
