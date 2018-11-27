using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using Common.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeoJsonConcat
{
	internal class Program
	{
		/// <summary>
		///     The Log (Common.Logging)
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);


		private static void Main(string[] args)
		{
			var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
			var fileInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);

			var version = versionInfo.ProductVersion;
			string assemblyDescription = null;

			var descriptionAttribute = Assembly.GetExecutingAssembly()
				.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)
				.OfType<AssemblyDescriptionAttribute>()
				.FirstOrDefault();

			if (descriptionAttribute != null)
				assemblyDescription = descriptionAttribute.Description;

			var executableInfoMessage =
				$"{assemblyDescription}{Environment.NewLine}{Path.GetFileName(Assembly.GetExecutingAssembly().Location)} v.{version} (last modified on {fileInfo.LastWriteTime})";
			Console.WriteLine(executableInfoMessage);
			Log.Info(executableInfoMessage);

			Parser.Default.ParseArguments<CommandLineOptions>(args)
				.WithParsed(RunOptionsAndReturnExitCode)
				.WithNotParsed(HandleParseError);
		}

		private static void HandleParseError(IEnumerable<Error> errs)
		{
			Log.Debug("Enter");

			/*
			foreach (var err in errs)
			{
				string message = null;

				switch (err.Tag)
				{
					case ErrorType.BadFormatTokenError:
						break;

					case ErrorType.MissingValueOptionError:
						break;

					case ErrorType.UnknownOptionError:
						break;

					case ErrorType.MissingRequiredOptionError:
						message = $"Missing Required Parameter: {((MissingRequiredOptionError)err).NameInfo.NameText}";

						break;

					case ErrorType.MutuallyExclusiveSetError:
						break;

					case ErrorType.BadFormatConversionError:
						break;

					case ErrorType.SequenceOutOfRangeError:
						break;

					case ErrorType.RepeatedOptionError:
						break;

					case ErrorType.NoVerbSelectedError:
						break;

					case ErrorType.BadVerbSelectedError:
						break;

					case ErrorType.HelpRequestedError:
						break;

					case ErrorType.HelpVerbRequestedError:
						break;

					case ErrorType.VersionRequestedError:
						break;

					default:
						Console.WriteLine($"Error: {err.Tag}");
						break;
				}

				if (message != null && !message.IsNullOrEmpty())
				{
				}
				;
			}		*/
		}

		private static void RunOptionsAndReturnExitCode(CommandLineOptions opts)
		{
			Log.Debug("Enter");

			ConcatenateGeoJsonFiles(opts);
		}

		public static void ConcatenateGeoJsonFiles(CommandLineOptions opts)
		{
			if (!string.IsNullOrEmpty(opts.InputDirectory))
			{
				var files = Directory.GetFiles(opts.InputDirectory, opts.Extensions, SearchOption.TopDirectoryOnly);

				if (files.Length == 0)
					ConsoleWriteError(
						$"There are no files in {opts.InputDirectory} that match extensions: {opts.Extensions}");

				var skippedFiles = new Dictionary<string, string>();
				var crsNames = new Dictionary<string, string>();
				var c = 0;

				foreach (var file in files)
				{
					c++;
					//if (c > 10)
					//	break;

					Console.Write($"Reading file {file}...");

					using (var sr = File.OpenText(file))
					using (var reader = new JsonTextReader(sr))
					{
						var featureCollection = (JObject) JToken.ReadFrom(reader);

						Console.WriteLine("complete.");

						var crsObject = featureCollection["crs"];

						if (crsObject != null)
						{
							var crsName = featureCollection["crs"]["properties"].Value<string>("name");
							crsNames.Add(file, crsName);
						}
						else
						{
							ConsoleWriteError(
								$"Skipping file: {file}.  This file does not contain a GeoJson FeatureCollection.");
							skippedFiles.Add(file, "This file does not contain a GeoJson FeatureCollection.");
						}

						// Console.Write($"CRS={crsName}");
					}
				}


				var distinctCrsNames = crsNames.Values.Distinct().ToList();

				if (distinctCrsNames.Count > 1)
					ConsoleWriteError("The GeoJson Files have different spatial reference systems");
				else
					Console.Write(
						$"Great news everyone! All files share the same spatial reference system: {distinctCrsNames.First()}");

				var groups = (from kvp in crsNames
					group kvp by kvp.Value
					into g
					select new {CRSName = g.Key, Files = g.ToList()}).ToList();

				Console.WriteLine($"Split files into {groups.Count} group(s) according to spatial reference systems.");

				var gcount = 0;
				foreach (var g in groups)
				{
					Console.WriteLine(
						$"Starting {g.CRSName} files (Group {++gcount} of {groups.Count}, {g.Files.Count} Files)");

					var outputFile = opts.OutputFileName;

					if (groups.Count() > 1) outputFile = $"{outputFile}.{g.CRSName}";

					c = 0;
					var inputFiles = g.Files;
					var tempFilePath = $"{opts.OutputFileName}.tmp";

					foreach (var kvp in inputFiles)
					{
						Console.Write($"Reading {++c} of {inputFiles.Count} files:  {kvp.Key}...");

						using (var sr = File.OpenText(kvp.Key))
						using (var reader = new JsonTextReader(sr))
						{
							Console.WriteLine("complete.");

							if (c == 1)
							{
								var jsonString = ((JObject) JToken.ReadFrom(reader)).ToString(Formatting.None);

								// write out the temp file, but string off the "]}" from the end.
								File.WriteAllText(tempFilePath, jsonString.Substring(0, jsonString.Length - 2));
							}
							else
							{
								var featureCollection = (JObject) JToken.ReadFrom(reader);

								File.AppendAllText(tempFilePath,
									$",{featureCollection["features"].ToString(Formatting.None)}");

								Console.WriteLine($"Appended {kvp.Key} features to {tempFilePath}.");
							}

							// Console.Write($"CRS={crsName}");
						}
					}

					File.AppendAllText(tempFilePath, "]}"); // Restore the Features array and property closers

					File.Copy(tempFilePath, outputFile, true);


					Console.WriteLine($"GeoJson Concat Complete.{Environment.NewLine}Output File: {outputFile}");
				}
			}
		}


		/// <summary>
		///     Writes a string for an error to the console.
		/// </summary>
		/// <param name="message">The message.</param>
		public static void ConsoleWriteError(string message)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(message);
			Console.ResetColor();
		}
	}
}