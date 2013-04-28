using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Splunk.Sharepoint.ModularInputs
{
	/// <summary>
	/// List of appropriate log levels for logging functions
	/// </summary>
	public enum LogLevel
	{
		/// <summary>
		/// Debug Messages
		/// </summary>
		DEBUG,
		/// <summary>
		/// Informational Messages
		/// </summary>
		INFO,
		/// <summary>
		/// Warning Messages
		/// </summary>
		WARN,
		/// <summary>
		/// Error Messages
		/// </summary>
		ERROR,
		/// <summary>
		/// Fatal Error Messages
		/// </summary>
		FATAL
	}

	/// <summary>
	/// The SharepointLogger class is used to hold all the specifics for logging in a Splunk Modular Input
	/// </summary>
	public class SharepointLogger
	{
		private static Dictionary<string, SharepointLogger> _logcache = null;

		/// <summary>
		/// Private method that converts a LogLevel into a string
		/// </summary>
		/// <param name="level">The log level to convert</param>
		/// <returns>The string representation of the log level</returns>
		static string _LL(LogLevel level)
		{
			switch (level)
			{
				case LogLevel.DEBUG: return "DEBUG";
				case LogLevel.INFO: return "INFO";
				case LogLevel.WARN: return "WARN";
				case LogLevel.ERROR: return "ERROR";
				case LogLevel.FATAL: return "FATAL";
				default: return "ERROR";
			}
		}

		/// <summary>
		/// Convenience method to write log messages to splunkd.log
		/// </summary>
		/// <param name="msg">The message</param>
		public static void SystemLogger(string msg)
		{
			SystemLogger(LogLevel.INFO, msg);
		}

		/// <summary>
		/// Convenience method to write log messages to splunkd.log
		/// </summary>
		/// <param name="level">The log level</param>
		/// <param name="msg">The message</param>
		public static void SystemLogger(LogLevel level, string msg)
		{
			Console.Error.WriteLine("{0} {1}", _LL(level), msg);
			Console.Error.Flush();
		}

		/// <summary>
		/// Opens a custom logging facility in %SPLUNK_HOME%\var\log\splunk
		/// and returns the logging handle to it.
		/// </summary>
		/// <param name="name">The name of the logfile (without .log)</param>
		/// <returns>THe logging handle</returns>
		public static SharepointLogger getCustomSharepointLogger(string name)
		{
			if (_logcache == null)
			{
				// Create a new Log Cache if it does not exist
				_logcache = new Dictionary<string, SharepointLogger>();
			}

			// If the name is not in the current log cache, then
			// create a new SharepointLogger
			if (!_logcache.ContainsKey(name))
			{
				SharepointLogger l = new SharepointLogger(name);
				_logcache.Add(name, l);
			}

			// Return the SharepointLogger from within the log cache
			return _logcache[name];
		}

		/// <summary>
		/// Private storage for the output stream
		/// </summary>
		private StreamWriter _out = null;

		/// <summary>
		/// Creates a Logging connection
		/// </summary>
		/// <param name="name">The name of the log</param>
		public SharepointLogger(string name)
		{
            Console.WriteLine("CAlling Logging.cs");//modified by shaju
			try
			{
				string splunkdir = Environment.GetEnvironmentVariable("SPLUNK_HOME");
				string logdir = Path.Combine(splunkdir, "log");
				string logfile = Path.Combine(logdir, name + ".log");
				_out = File.AppendText(logfile);
			}
			catch (Exception ex)
			{
				SystemLogger(LogLevel.FATAL, String.Format("Error writing to log file: {0}", ex.Message));
				throw;
			}
		}

		/// <summary>
		/// Log a message
		/// </summary>
		/// <param name="level">The level of the message to be logged</param>
		/// <param name="msg">The message to be logged</param>
		public void Log(LogLevel level, string msg)
		{
			_out.WriteLine("{0} [{1}] {2} {3}", DateTime.Now.ToString("o"), Process.GetCurrentProcess().Id, _LL(level), msg);
			_out.Flush();
		}
	}
}
