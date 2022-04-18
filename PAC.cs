using System.Text;

namespace PACManager
{
	public static class PAC
	{
		public static bool Pack()
		{
			if (!Directory.Exists("output"))
			{
				Console.Out.WriteLine(new DirectoryNotFoundException("You have not unpacked GAME.PAC yet."));
				return false;
			}
			Console.Out.WriteLine("Packing...");
			BinaryWriter pac = new(File.Create("temp_GAME.PAC"));
			BinaryWriter pah = new(File.Create("temp_GAME.PAH"));
			string[] files = Directory.GetFiles("output");
			int offset = 0;
			string[] file_names = new string[files.Length];
			for (int i = 0; i < files.Length; i++)
			{
				file_names[i] = Path.GetFileName(files[i]);
			}
			int[] file_data_lengths = new int[files.Length];
			int[] file_data_ptrs = new int[files.Length];
			byte[] buffer = new byte[0x800];
			Console.Out.WriteLine("Writing to pac...");
			for (int i = 0; i < files.Length; i++)
			{
				byte[] data = File.ReadAllBytes(files[i]);
				// store the length of this file
				file_data_lengths[i] = data.Length;
				// store the current position in pac where this file will be
				file_data_ptrs[i] = offset;
				// ready the next position
				offset += file_data_lengths[i];
				// write the data
				pac.Write(data, 0, file_data_lengths[i]);
				// align the pac
				int alignment = (int)(0x800 - (pac.BaseStream.Position % 0x800));
				if (alignment != 0x800)
				{
					pac.Write(buffer, 0, alignment);
					offset += alignment;
				}
			}
			pac.Flush();
			pac.Close();



			Console.Out.WriteLine("Writing to pah...");
			pah.Write(files.Length);
			// write the pointer to the start of data
			const int header_size = 0x70;
			pah.Write(header_size);
			// create an array of lists for alphabet use
			List<ushort>[] alphabet_list = new List<ushort>[26];
			// initialize a value to contain the total number of bytes used by alphabets
			int total_alphabet_count = 0;
			// buffer the initial alphabet
			int[] alphabet = new int[26];
			// initialize every list in the alphabet array
			for (int i = 0; i < alphabet_list.Length; i++)
			{
				alphabet_list[i] = new List<ushort>();
			}
			byte lower_a = Program.shift_jis.GetBytes("a")[0];
			byte upper_a = Program.shift_jis.GetBytes("A")[0];
			// iterate through each file's name
			for (int i = 0; i < file_names.Length; i++)
			{
				byte[] bytes = Program.shift_jis.GetBytes(file_names[i]);
				if (bytes[0] > lower_a)
				{
					bytes[0] -= (byte)(lower_a - upper_a);
				}
				bytes[0] -= upper_a;
				alphabet_list[bytes[0]].Add((ushort)i);
			}
			for (int i = 0; i < alphabet_list.Length; i++)
			{
				alphabet[i] = total_alphabet_count + (files.Length * 0x10) + header_size;
				total_alphabet_count += (alphabet_list[i].Count + 1) * sizeof(ushort);
			}
			int file_name_offset = (files.Length * 0x10) + header_size + total_alphabet_count;
			for (int i = 0; i < alphabet.Length; i++)
			{
				pah.Write(alphabet[i]);
			}

			// write the pointers
			for (int i = 0; i < files.Length; i++)
			{
				pah.Write(file_data_ptrs[i]);
				pah.Write(file_data_lengths[i]);
				pah.Write(0);
				pah.Write(file_name_offset);
				file_name_offset += file_names[i].Length + 1;
				if (file_name_offset % 2 != 0)
				{
					file_name_offset++;
				}
			}

			// write the alphabets
			for (int i = 0; i < alphabet_list.Length; i++)
			{
				pah.Write(BitConverter.GetBytes((short)alphabet_list[i].Count), 0, 2);
				for (int j = 0; j < alphabet_list[i].Count; j++)
				{
					pah.Write(BitConverter.GetBytes(alphabet_list[i][j]), 0, 2);
				}
			}

			// write file names
			for (int i = 0; i < files.Length; i++)
			{
				byte[] bytes = Program.shift_jis.GetBytes(file_names[i]);
				pah.Write(bytes, 0, bytes.Length);
				pah.Write(buffer, 0, 1);
				if (pah.BaseStream.Position % 2 != 0)
				{
					pah.Write(buffer, 0, 1);
				}
			}
			pah.Flush();
			pah.Close();
			Console.Out.WriteLine("Done.");

			if (File.Exists("GAME.PAH"))
			{
				File.Delete("GAME.PAH");
			}
			File.Move("temp_GAME.PAH", "GAME.PAH");
			if (File.Exists("GAME.PAC"))
			{
				File.Delete("GAME.PAC");
			}
			File.Move("temp_GAME.PAC", "GAME.PAC");
			return true;
		}

		public static bool Unpack()
		{
			if (!File.Exists("GAME.PAC"))
			{
				Console.Out.WriteLine(new FileNotFoundException("GAME.PAC does not exist in this directory."));
				return false;
			}
			if (!File.Exists("GAME.PAH"))
			{
				Console.Out.WriteLine(new FileNotFoundException("GAME.PAH does not exist in this directory."));
				return false;
			}
			Console.Out.WriteLine("Unpacking...");
			BinaryReader pah = new(File.OpenRead("GAME.PAH"));
			int file_count = pah.ReadInt32();
			pah.BaseStream.Position = pah.ReadInt32();
			BinaryReader pac = new(File.OpenRead("GAME.PAC"));
			Directory.CreateDirectory("output");
			Console.Out.WriteLine();
			for (int file_id = 0; file_id < file_count; file_id++)
			{
				pac.BaseStream.Position = pah.ReadInt32();
				byte[] data = pac.ReadBytes(pah.ReadInt32());
				pah.BaseStream.Position += 4;
				int old_pos = (int)pah.BaseStream.Position + 4;
				pah.BaseStream.Position = pah.ReadInt32();
				string file = $"output/{pah.ReadZeroTerminatedString()}";
				Console.Out.WriteLine($"Writing {file}...");
				BinaryWriter temp = new(File.Create(file));
				pah.BaseStream.Position = old_pos;
				temp.Write(data);
				temp.Flush();
				temp.Close();
			}
			Console.Out.WriteLine("Done.");
			return true;
		}
	}
}
