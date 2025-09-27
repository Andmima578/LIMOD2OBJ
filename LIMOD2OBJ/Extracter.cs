using System;
using System.IO;
using System.Text;

namespace LIMOD2OBJ
{
	public class Extracter
	{
		static FileStream wdbFile;
		static FileInfo wdbFileInfo;
		static string wdbFileName = "";
        static BinaryReader br;

		public static void ParseWDB(string filePath = "", bool convert = false)
		{
			if (convert)
                Console.WriteLine("Extracting and converting .MOD files from the World DataBase. Please wait!");
            else
				Console.WriteLine("Extracting .MOD files from the World DataBase. Please wait!");
			wdbFile = File.OpenRead(filePath);
			br = new BinaryReader(wdbFile);
			wdbFileInfo = new FileInfo(filePath);
			wdbFileName = wdbFileInfo.Name.Replace(wdbFileInfo.Extension, "");

			//Grouped Models
			Directory.CreateDirectory(wdbFileName);
			uint groups = br.ReadUInt32();
			for (uint group = 0; group < groups; group++)
			{
				string groupName = new string(br.ReadChars(br.ReadInt32() - 1));
				wdbFile.Seek(1, SeekOrigin.Current);
				string groupPath = wdbFileName + @"\GroupedModels\" + groupName;
				Directory.CreateDirectory(groupPath);
				for (byte subgroup = 0; subgroup < 2; subgroup++)
				{
					uint modelCount = br.ReadUInt32();
					for (uint model = 0; model < modelCount; model++)
					{
						Directory.CreateDirectory(groupPath + @"\" + "sub" + subgroup);
						string modName = new string(br.ReadChars(br.ReadInt32() - 1));
						wdbFile.Seek(1, SeekOrigin.Current);
						int modelSize = br.ReadInt32();
						uint modelPosition = br.ReadUInt32();
						if (subgroup == 1)
						{
							string legoEntityPresenter = new string(br.ReadChars(br.ReadInt32() - 1));
							wdbFile.Seek(38, SeekOrigin.Current);
						}
						long oldPosition = wdbFile.Position;
						wdbFile.Position = modelPosition;
						FileStream modFile = File.Create(groupPath + @"\" + "sub" + subgroup + @"\" + modName + ".MOD", modelSize);
						modFile.Write(br.ReadBytes(modelSize), 0, modelSize);
						modFile.Close();
						if (convert)
							Converter.ConvertMOD(groupPath + @"\" + "sub" + subgroup + @"\" + modName + ".MOD", false, groupPath + @"\" + "sub" + subgroup);
						wdbFile.Position = oldPosition;
					}
				}
			}

			//Dummy Textures
			uint dummyTexturesSize = br.ReadUInt32();
			uint dummyTextureCount = br.ReadUInt32();
			for (uint dummyTexture = 0; dummyTexture < dummyTextureCount; dummyTexture++)
			{
				string dummyTextureName = new string(br.ReadChars(br.ReadInt32()));
				uint width = br.ReadUInt32();
				uint height = br.ReadUInt32();
				uint colorsTotal = br.ReadUInt32();
				byte[,] palette = new byte[colorsTotal, 3];
				for (int color = 0; color < colorsTotal; color++)
				{
					palette[color, 0] = br.ReadByte();
					palette[color, 1] = br.ReadByte();
					palette[color, 2] = br.ReadByte();
				}
				byte[] pixels = new byte[width * height];
				for (int pixel = 0; pixel < width * height; pixel++)
				{
					pixels[pixel] = br.ReadByte();
				}
				ConvertToBMP("DummyTextures", dummyTextureName, width, height, palette, pixels);
			}

            //Universal Models
            Directory.CreateDirectory(wdbFileName + @"\UniversalModels");
            uint universalModelsTexturesSize = br.ReadUInt32();
			long universalModelsPositionStart = wdbFile.Position;
            uint universalModelsSize = br.ReadUInt32();
			uint universalModelsCount = br.ReadUInt32();
            long universalModelsLastPosition = wdbFile.Position;
			for (uint universalModel = 0; universalModel < universalModelsCount; universalModel++)
            {
                universalModelsLastPosition = wdbFile.Position;
                string modName = new string(br.ReadChars(br.ReadInt32()));
				wdbFile.Seek(4, SeekOrigin.Current);
                uint modelOffset = br.ReadUInt32();
				int modelSize = (int)(universalModelsPositionStart + modelOffset - universalModelsLastPosition);
				wdbFile.Position = universalModelsLastPosition;
                FileStream modFile = File.Create(wdbFileName + @"\UniversalModels\" + modName + ".MOD", modelSize);
                modFile.Write(br.ReadBytes(modelSize), 0, modelSize);
                modFile.Close();
				if (convert)
                    Converter.ConvertMOD(wdbFileName + @"\UniversalModels\" + modName + ".MOD", false, wdbFileName + @"\UniversalModels");
            }

            //Universal Textures
            uint universalTextureCount = br.ReadUInt32();
            for (uint universalTexture = 0; universalTexture < universalTextureCount; universalTexture++)
            {
                string universalTextureName = new string(br.ReadChars(br.ReadInt32()));
                uint width = br.ReadUInt32();
                uint height = br.ReadUInt32();
                uint colorsTotal = br.ReadUInt32();
                byte[,] palette = new byte[colorsTotal, 3];
                for (int color = 0; color < colorsTotal; color++)
                {
                    palette[color, 0] = br.ReadByte();
                    palette[color, 1] = br.ReadByte();
                    palette[color, 2] = br.ReadByte();
                }
                byte[] pixels = new byte[width * height];
                for (int pixel = 0; pixel < width * height; pixel++)
                {
                    pixels[pixel] = br.ReadByte();
                }
                ConvertToBMP("UniversalModels", universalTextureName, width, height, palette, pixels);
            }

            wdbFile.Close();
		}

		//Same code from mod converter, just different
		static void ConvertToBMP(string folder, string textureName, uint width, uint height, byte[,] palette, byte[] pixels)
		{
			uint fileLength = (uint)(width * height + palette.GetLength(0) * 4 + 54);
			Directory.CreateDirectory(wdbFileName + @"\" + folder);
			FileStream bmpFile = new FileStream(wdbFileName + @"\" + folder + @"\" + textureName.Replace(".GIF", ".BMP").Replace(".gif", ".bmp"), FileMode.Create);
			BinaryWriter bw = new BinaryWriter(bmpFile, Encoding.ASCII);
			bw.Write("BM".ToCharArray());
			bw.Write(fileLength);
			bw.Write(0x00000000);
			bw.Write(54 + palette.GetLength(0) * 4);
			bw.Write(40);
			bw.Write(width);
			bw.Write(height);
			bw.Write((ushort)1);
			bw.Write((ushort)8);
			bw.Write(0x0000);
			bw.Write(width * height);
			bw.Write(2835);
			bw.Write(2835);
			bw.Write(palette.GetLength(0));
			bw.Write(palette.GetLength(0));
			for (int color = 0; color < palette.GetLength(0); color++)
			{
				bw.Write(palette[color, 2]); //B
				bw.Write(palette[color, 1]); //G
				bw.Write(palette[color, 0]); //R
				bw.Write((byte)0xff); //A
			}
			bw.Write(pixels);
			bw.Close();
			bmpFile.Close();
		}
	}
}
