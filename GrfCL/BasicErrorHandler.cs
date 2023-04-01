using System;
using ErrorManager;
using Utilities.CommandLine;

namespace GrfCL {
	public class BasicErrorHandler : IErrorHandler {
		public static bool BreakOnExceptions;
		public static bool ExitOnExceptions;

		#region IErrorHandler Members

		public void Handle(Exception exception, ErrorLevel errorLevel) {
			Handle(exception.Message, errorLevel);
		}

		public void Handle(string exception, ErrorLevel errorLevel) {
			Console.WriteLine("//");
			Console.WriteLine("//  GRF Error Handler has thrown an exception : ");
			Console.WriteLine("//  Error level : " + errorLevel);
			Console.WriteLine("//  Message : ");
			Console.WriteLine(CLHelper.Indent(exception, 8, false));
			Console.WriteLine("//" + CLHelper.Fill('_', Console.WindowWidth - 2));

			if (BreakOnExceptions)
				GrfCL.Break();

			if (ExitOnExceptions)
				throw new Exception(exception);
		}

		public void Handle(object caller, Exception exception, ErrorLevel errorLevel) {
			Handle(caller, exception.Message, errorLevel);
		}

		public void Handle(object caller, string exception, ErrorLevel errorLevel) {
			Console.WriteLine("//");
			Console.WriteLine("//  GRF Error Handler has thrown an exception : ");
			Console.WriteLine("//  Error level : " + errorLevel);
			Console.WriteLine("//  Message : ");
			Console.WriteLine(CLHelper.Indent(exception, 8, false));
			Console.WriteLine("//  Object responsible : " + caller);
			Console.WriteLine("//" + CLHelper.Fill('_', Console.WindowWidth - 2));

			if (BreakOnExceptions)
				GrfCL.Break();

			if (ExitOnExceptions)
				throw new Exception(exception);
		}

		public bool YesNoRequest(string message, string caption) {
			Console.WriteLine("//");
			Console.WriteLine("//  Caption : " + caption);
			Console.WriteLine("//  Request : " + message);
			Console.Write("//  Yes or no (Y | N) ? ");
			Console.WriteLine("//" + CLHelper.Fill('_', Console.WindowWidth - 2));
			
			string answer = Console.ReadLine();
			
			if (answer != null && answer.ToLower().StartsWith("y")) {
				Console.WriteLine("Answer : Yes");
				Console.WriteLine();
				return true;
			}

			Console.WriteLine("Answer : No");
			Console.WriteLine();
			return false;
		}

		#endregion
	}
}
