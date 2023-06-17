using System;
using System.Text;

namespace PACManager
{
	public class Program
	{
		public static Encoding? shift_jis { get; private set; }

		static void Main(params string[] args)
		{
			var arg_except = new ArgumentException("InvalidArguments. Run with -h or -? for help.");
			var file_type_except = new FileLoadException("Argument '-d' requires one PAC and one PAH file as additional arguments.");
			if (args.Length == 0)
			{
				throw arg_except;
			}

			shift_jis = CodePagesEncodingProvider.Instance.GetEncoding("shift-jis");
			if (shift_jis == null)
			{
				throw new NullReferenceException("Failed to get the Shift-JIS encoding.");
			}

			if (Directory.Exists(args[0]))
			{
				PAC.Pack(args[0]);
			}
			else if (File.Exists(args[0]))
			{
				string path = Path.GetFullPath(args[0]);
				string file_name = Path.GetFileNameWithoutExtension(args[0]);
				string pac = Path.Combine(path, file_name) + ".PAC";
				if (!File.Exists(pac))
				{
					throw new FileLoadException($"Invalid path to PAC: {pac}");
				}
				string pah = Path.Combine(path, file_name) + ".PAH";
				if (File.Exists(pah))
				{
					throw new FileLoadException($"Invalid path to PAH: {pah}");
				}
				PAC.Unpack(pac, pah);
			}

			switch (args[0])
			{
				case "-d":
					{
						if (args.Length < 3)
						{
							throw arg_except;
						}
						string pac;
						string pah;
						if (Path.GetExtension(args[1]).Contains("PAC"))
						{
							if (!Path.GetExtension(args[2]).Contains("PAH"))
							{
								throw file_type_except;
							}
							pac = args[1];
							pah = args[2];
						}
						else if (Path.GetExtension(args[1]).Contains("PAH"))
						{
							if (!Path.GetExtension(args[2]).Contains("PAC"))
							{
								throw file_type_except;
							}
							pac = args[2];
							pah = args[1];
						}
						else
						{
							throw file_type_except;
						}
						PAC.Unpack(pac, pah);
						break;
					}
				case "-c":
					{
						string output_path;
						if (args.Length < 2)
						{
							output_path = Directory.GetCurrentDirectory();
						}
						else
						{
							output_path = args[1];
						}
						PAC.Pack(output_path);
						break;
					}
				case "-h":
					{
						Console.WriteLine($"PACManager.exe -[dc] [args...]");
						Console.WriteLine($"\t{"-d \"PATH\\TO\\PAC\" \"PATH\\TO\\PAH\"",-30} - Unpacks the given PAC file using the given PAH file.\n\t\tOutputs to /output/ folder in the same directory as this program.");
						Console.WriteLine($"\t{"-c \"PATH\\TO\\FILES\"",-30} - Packs all files in the /output/ directory.");
						Console.WriteLine($"\t{"-h",-30} - Prints arguments.");
						Console.WriteLine("Drag and Drop is also supported. Simply drag and drop a PAC, PAH, or directory.");
						break;
					}
				default:
					{
						throw arg_except;
					}
			}
		}
	}
}