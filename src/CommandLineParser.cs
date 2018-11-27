using System.Reflection;
using CommandLine;
using Common.Logging;

// ReSharper disable UnusedMember.Global

namespace GeoJsonConcat
{
	/// <summary>
	///   -o D:\tmp\ulstercountyny\Tax_Parcels\Tax_Parcels\All-Tax_Parcels.geojson -x *.json -d D:\tmp\ulstercountyny\Tax_Parcels\Tax_Parcels
	/// </summary>
	public class CommandLineOptions
	{
		/// <summary>
		///     The Log (Common.Logging)
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		
		[Option('o', "output-name", Required = false,
			HelpText = "The output file name.")]
		public string OutputFileName { get; set; }


		[Option('x', Required = false,
			HelpText = "The file extensions to filter the input directory.",
			Default = "*.geojson")]
		public string Extensions { get; set; }


		[Option('d', Required = false,
			HelpText = "The directory containing GeoJson files")]
		public string InputDirectory { get; set; }

		[Option('v', Required = false, HelpText = "Verbose output")]
		public bool Verbose { get; set; }
	}
}