using System;
using System.IO;
using System.Text;

namespace PACManager
{
	public class Program
	{
		public static Encoding shift_jis { get; private set; }

		static void Main(params string[] args)
		{
			if (args.Length == 0)
			{
				throw new ArgumentException("Invalid arguments");
			}

			shift_jis = CodePagesEncodingProvider.Instance.GetEncoding("shift-jis");

			switch (args[0])
			{
				case "/d":
					{
						PAC.Unpack();
						break;
					}
				case "/c":
					{
						PAC.Pack();
						break;
					}
				default:
					{
						throw new ArgumentException("Invalid arguments");
					}
			}
		}
	}
}