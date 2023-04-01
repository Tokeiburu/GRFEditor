using System.Collections.Generic;
using System.Linq;

namespace Utilities.CommandLine {
	public class GenericCLOption {
		public List<string> Args = new List<string>();
		private OptionalArguments _optionalArgs;
		public string CommandName { get; set; }

		public OptionalArguments OptionalArgs {
			get { return _optionalArgs ?? (_optionalArgs = new OptionalArguments(Args)); }
		}
	}

	public class OptionalArguments {
		private readonly Dictionary<string, string> _args = new Dictionary<string, string>();

		public OptionalArguments(IEnumerable<string> args) {
			List<string[]> dic = args.Select(p => p.Split('=')).ToList();

			foreach (string[] di in dic) {
				if (di.Length < 2 || !di[0].StartsWith("/"))
					continue;

				_args.Add(di[0], di.Skip(1).Aggregate((a, b) => a + '=' + b).Trim('\"'));
			}
		}

		public string this[string arg] {
			get {
				if (arg.EndsWith("="))
					arg = arg.Remove(arg.Length - 1, 1);

				return _args.ContainsKey(arg) ? _args[arg] : null;
			}
		}
	}
}
