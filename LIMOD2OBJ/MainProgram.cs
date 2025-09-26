using System;
using System.Globalization;
using System.Threading;

namespace LIMOD2OBJ
{
	internal class MainProgram
	{
		//Getting the file
		static void Main(string[] args)
		{
			Console.Title = "LIMOD2OBJ";
			Console.Clear();
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			if (args.Length > 0)
			{
				Converter.ConvertMOD(args[0], true, args[1]);
			}
			else
			{
				Console.WriteLine("What do you want to do?");
				Console.WriteLine("1: Convert a single .MOD file");
				Console.WriteLine("2: Extract all .MOD files from World DataBase");
				Console.WriteLine("3: Extract all .MOD files from World DataBase and convert them");
				Console.WriteLine("0: Exit the program");

				switch (Console.ReadLine())
				{
					default:
						{
							Console.WriteLine("Invalid option");
							Thread.Sleep(1000);
							Main(new string[] { });
							break;
						}
					case "0":
						{
							Environment.Exit(0);
							break;
						}
					case "1":
						{
							ConvertSingleMOD();
							break;
						}
					case "2":
						{
							PreparationToExtractWDB();
							break;
						}
					case "3":
						{
							PreparationToExtractWDB(true);
							break;
						}
				}
			}
		}

		static void ConvertSingleMOD()
		{
			Console.Clear();
			Console.WriteLine("Drag & Drop a .MOD file.");
			Converter.ConvertMOD(Console.ReadLine().Replace("\"", ""), true);
		}

		static void PreparationToExtractWDB(bool convert = false)
		{
			Console.Clear();
			Console.WriteLine("Drag & Drop a .WDB file.");
			Extracter.ParseWDB(Console.ReadLine().Replace("\"", ""), convert);
			if (convert)
				Console.WriteLine("Everything was extracted and converted. Now enjoy your files.");
			else
				Console.WriteLine("Everything was extracted. Now enjoy your files.");
		}
	}
}
