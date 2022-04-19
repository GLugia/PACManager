﻿using System.Text;

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


			Dictionary<string, ushort> file_dict = new();
			string[] files = Directory.GetFiles("output");
			string[] file_names = files.Select(a => Path.GetFileName(a)).ToArray();
			for (ushort i = 0; i < file_names.Length; i++)
			{
				file_dict.Add(file_names[i], i);
			}

			Console.Out.WriteLine("Writing to pac...");
			BinaryWriter pac = new(File.Create("temp_GAME.PAC"));
			int[] file_data_lengths = new int[files.Length];
			int[] file_data_ptrs = new int[files.Length];
			byte[] buffer = new byte[0x800];
			for (int i = 0; i < files.Length; i++)
			{
				byte[] data = File.ReadAllBytes(files[i]);
				// store the length of this file
				file_data_lengths[i] = data.Length;
				// store the current position in pac where this file will be
				file_data_ptrs[i] = (int)pac.BaseStream.Position;
				// write the data
				pac.Write(data, 0, file_data_lengths[i]);
				// align the data to 0x800
				int alignment = buffer.Length - (file_data_lengths[i] % buffer.Length);
				if (alignment != buffer.Length)
				{
					pac.Write(buffer, 0, alignment);
				}
			}
			pac.Flush();
			pac.Close();



			BinaryWriter pah = new(File.Create("temp_GAME.PAH"));
			Console.Out.WriteLine("Writing to pah...");
			pah.Write(files.Length);
			// write the pointer to the start of data
			const int header_size = 0x70;
			pah.Write(header_size);

			// sort each file into an alphabet

			// create an array of lists for alphabet use
			List<string>[] alphabet_list = new List<string>[26];
			// initialize a value to contain the total number of bytes used by alphabets
			int total_alphabet_count = 0;
			// buffer the initial alphabet
			int[] alphabet = new int[26];
			// initialize every list in the alphabet array
			for (int i = 0; i < alphabet_list.Length; i++)
			{
				alphabet_list[i] = new List<string>();
			}

			byte upper_z = Program.shift_jis.GetBytes("Z")[0];
			byte upper_a = Program.shift_jis.GetBytes("A")[0];
			byte lower_a = Program.shift_jis.GetBytes("a")[0];
			// iterate through each file's name
			for (int i = 0; i < file_names.Length; i++)
			{
				// get the first letter of the name
				byte c = (byte)file_names[i][0];
				// if the letter's value is higher than 'Z'
				if (c > upper_z)
				{
					// remove the 6 non-alphabet chars
					c -= (byte)(lower_a - upper_z);
				}
				// subtract the value of 'A' to get the index in the array
				c -= upper_a;
				// add the file's name to this index
				alphabet_list[c].Add(file_names[i]);
			}
			for (int i = 0; i < alphabet_list.Length; i++)
			{
				// sort the files for each letter index
				alphabet_list[i].Sort();
				// find the offsets for each letter's position in pah
				alphabet[i] = total_alphabet_count + (file_names.Length * 0x10) + header_size;
				total_alphabet_count += (alphabet_list[i].Count + 1) * sizeof(ushort);
			}
			int file_name_offset = (files.Length * 0x10) + header_size + total_alphabet_count;

			// write the pointers to each letter
			for (int i = 0; i < alphabet.Length; i++)
			{
				pah.Write(alphabet[i]);
			}

			// write the file data
			for (int i = 0; i < files.Length; i++)
			{
				// pointers to their starting position in pac
				pah.Write(file_data_ptrs[i]);
				// length of the file not including padding
				pah.Write(file_data_lengths[i]);
				// separate pac and pah? idk
				pah.Write(0);
				// pointer to file name in pah
				pah.Write(file_name_offset);
				// increment the pointer for the next file
				file_name_offset += file_names[i].Length + 1;
				if (file_name_offset % 2 != 0)
				{
					file_name_offset++;
				}
			}

			// write the alphabets
			for (int i = 0; i < alphabet_list.Length; i++)
			{
				// write the length of the array
				pah.Write(BitConverter.GetBytes((short)alphabet_list[i].Count), 0, 2);
				for (int j = 0; j < alphabet_list[i].Count; j++)
				{
					// write the id of the file at this index
					pah.Write(BitConverter.GetBytes(file_dict[alphabet_list[i][j]]), 0, 2);
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

			// this is only necessary for matching
			pah.Write(buffer, 0, 0x30);
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
			if (Directory.Exists("output"))
			{
				Directory.Delete("output", true);
			}
			Directory.CreateDirectory("output");
			Console.Out.WriteLine();
			byte[] data;
			int old_pos;
			string name;
			for (int file_id = 0; file_id < file_count; file_id++)
			{
				pac.BaseStream.Position = pah.ReadInt32();
				data = pac.ReadBytes(pah.ReadInt32());
				pah.BaseStream.Position += sizeof(int);
				old_pos = (int)pah.BaseStream.Position + sizeof(int);
				pah.BaseStream.Position = pah.ReadInt32();
				name = pah.ReadZeroTerminatedString();
				Console.Out.WriteLine($"Writing {name}...");
				BinaryWriter temp = new(File.Create($"output/{name}"));
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
