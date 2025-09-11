using System;
using System.Collections.Generic;

namespace Utilities.CommandLine {
	/// <summary>
	/// Parses a string of commands to Generic Command Line Option objects
	/// </summary>
	public static class CommandLineParser {
		public static List<GenericCLOption> GetOptions(string commandLine, bool ignoreFirst = true) {
			try {
				List<GenericCLOption> options = new List<GenericCLOption>();
				string lineElement = commandLine;

				int position = 0;

				if (ignoreFirst)
					_readNext(ref position, lineElement);

				while (position < lineElement.Length) {
					string element = _readNext(ref position, lineElement);

					if (element == null)
						break;

					GenericCLOption command = new GenericCLOption();
					command.CommandName = element;
					_addArguments(command, ref position, lineElement);
					options.Add(command);
				}

				return options;
			}
			catch {
				return new List<GenericCLOption>();
			}
		}

		private static void _addArguments(GenericCLOption command, ref int position, string lineElement) {
			while (_isArgument(_peekNext(position, lineElement))) {
				command.Args.Add(_readNext(ref position, lineElement));
			}
		}

		private static bool _isArgument(string peekNext) {
			if (peekNext == null || _isCommand(peekNext))
				return false;
			return true;
		}

		private static bool _isCommand(string nextElement) {
			return nextElement.StartsWith("-");
		}

		private static string _peekNext(int position, string lineElement) {
			return _readNext(ref position, lineElement);
		}

		private static string _readNext(ref int position, string lineElement) {
			while (position < lineElement.Length && lineElement[position] == ' ')
				position++;

			if (position >= lineElement.Length)
				return null;

			// This is an option
			if (lineElement[position] == '-') {
				int spaceIndex = lineElement.IndexOf(" ", position, StringComparison.Ordinal);
				if (spaceIndex == -1) {
					string temp = lineElement.Substring(position, lineElement.Length - position);
					position = lineElement.Length;
					return temp;
				}
				string toReturn = lineElement.Substring(position, spaceIndex - position);
				position = spaceIndex + 1;
				return toReturn;
			}

			// This is a quote argument (the quotes are removed)
			if (lineElement[position] == '\"') {
				int spaceIndex = lineElement.IndexOf("\"", position + 1, StringComparison.Ordinal);
				if (spaceIndex == -1)
					return null;
				string toReturn = lineElement.Substring(position + 1, spaceIndex - position - 1);
				position = spaceIndex + 2;
				return toReturn;
			}
			else {
				int spaceIndex = lineElement.IndexOf(" ", position + 1, StringComparison.Ordinal);

				if (spaceIndex == -1)
					spaceIndex = lineElement.Length;

				string toReturn = lineElement.Substring(position, spaceIndex - position);
				position = spaceIndex + 1;
				return toReturn;
			}
		}
	}
}
