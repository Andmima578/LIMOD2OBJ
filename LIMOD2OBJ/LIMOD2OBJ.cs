using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace LIMOD2OBJ
{
	internal class LIMOD2OBJ
	{
		static FileStream modFile;
		static BinaryReader br;
		static string fileName = "";

		//Getting the file
		static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			if (args.Length > 0)
			{
				Preparation(args[0]);
			}
			else
			{
				Console.WriteLine("Drag & Drop a .MOD file.");
				Preparation(Console.ReadLine().Replace("\"", ""));
			}
		}

		//This is where the main script is
		static void Preparation(string filePath = "")
		{
			modFile = File.OpenRead(filePath);
			br = new BinaryReader(modFile);


			uint magicNumber = br.ReadUInt32();
			uint dataLength;
			uint version;
			if (magicNumber == 19) //Standard .MOD files, nothing special.
			{
				dataLength = br.ReadUInt32();
				version = br.ReadUInt32();
				modFile.Seek(8, SeekOrigin.Current);
				fileName = new string(br.ReadChars(br.ReadInt32()));
				Console.WriteLine("Completely valid .MOD file!");

				modFile.Seek(16 * br.ReadUInt32(), SeekOrigin.Current);
				modFile.Seek(16 * br.ReadUInt32(), SeekOrigin.Current);

				uint objHeaderCount = br.ReadUInt32();
				for (uint obj = 0; obj < objHeaderCount; obj++)
				{
					string objName = new string(br.ReadChars(br.ReadInt32()));
					modFile.Seek(16 * br.ReadUInt16(), SeekOrigin.Current);
					modFile.Seek(20 * br.ReadUInt16(), SeekOrigin.Current);
					modFile.Seek(4, SeekOrigin.Current);
					objHeaderCount += br.ReadUInt32();
					Console.WriteLine(objName);
				}

				uint objCount = 1;

				for (uint obj = 0; obj < objCount; obj++)
				{
					string objName = new string(br.ReadChars(br.ReadInt32()));
					Console.WriteLine(objName);
					modFile.Seek(40, SeekOrigin.Current);
					string objAltName = new string(br.ReadChars(br.ReadInt32()));
					if (br.ReadByte() != 1)
					{
						ParseModel(objName);
					}
					objCount += br.ReadUInt32();
				}

				uint textureCount = br.ReadUInt32();
				modFile.Seek(4, SeekOrigin.Current);
				for (uint texture = 0; texture < textureCount; texture++)
				{
					string textureName = new string(br.ReadChars(br.ReadInt32()));
					ParseTexture(textureName.Replace("^", ""));
					if (textureName[0] == '^')
						ParseTexture(textureName.Replace("^", "hidden_"));
				}
			}
			else if (magicNumber > 19) //Wowza! .MOD files from sub0?!
			{
				dataLength = magicNumber;
				version = br.ReadUInt32();
				fileName = new string(br.ReadChars(br.ReadInt32()));
				Console.WriteLine("Must be one of those sub0 .MOD files");
				ParseModel(fileName);

				uint textureCount = br.ReadUInt32();
				for (uint texture = 0; texture < textureCount; texture++)
				{
					string textureName = new string(br.ReadChars(br.ReadInt32()));
					ParseTexture(textureName);
				}
			}
			else //Probably a universal, who knows.
			{
				fileName = new string(br.ReadChars((int)magicNumber));
				Console.WriteLine("Wow! A universal .MOD file?");
				ParseModel(fileName);
			}
			Console.WriteLine(fileName);

			br.Close();
			modFile.Close();
		}

		//Interesting stuff
		static void ParseModel(string modelName)
		{
			uint LODCount = br.ReadUInt32();
			if (LODCount <= 0)
			{
				return;
			}
			uint modelDataLength = br.ReadUInt32();
			for (uint lod = 0; lod < LODCount; lod++)
			{
				//Model header data
				modFile.Seek(4, SeekOrigin.Current);
				ushort materialCount = br.ReadUInt16();
				if (materialCount <= 0)
				{
					modFile.Seek(2, SeekOrigin.Current);
					return;
				}
				modFile.Seek(2, SeekOrigin.Current);
				ushort vertexCount = br.ReadUInt16();
				ushort normalCount = br.ReadUInt16();
				ushort uvCount = br.ReadUInt16();
				modFile.Seek(2, SeekOrigin.Current);

				//Vertices
				float[,] vertices = new float[vertexCount, 3];
				for (ushort vertex = 0; vertex < vertexCount; vertex++)
				{
					vertices[vertex, 0] = br.ReadSingle();
					vertices[vertex, 1] = br.ReadSingle();
					vertices[vertex, 2] = br.ReadSingle();
					Console.WriteLine("Vertex " + vertex + ": " + vertices[vertex, 0] + ", " + vertices[vertex, 1] + ", " + vertices[vertex, 2]);
				}

				//Normals
				float[,] normals = new float[normalCount / 2, 3];
				for (ushort normal = 0; normal < normalCount / 2; normal++)
				{
					normals[normal, 0] = br.ReadSingle();
					normals[normal, 1] = br.ReadSingle();
					normals[normal, 2] = br.ReadSingle();
					Console.WriteLine("Normal " + normal + ": " + normals[normal, 0] + ", " + normals[normal, 1] + ", " + normals[normal, 2]);
				}

				//UV maps
				float[,] uvs = new float[uvCount, 2];
				for (ushort uv = 0; uv < uvCount; uv++)
				{
					uvs[uv, 0] = br.ReadSingle();
					uvs[uv, 1] = br.ReadSingle();
					Console.WriteLine("UV " + uv + ": " + uvs[uv, 0] + ", " + uvs[uv, 1]);
				}
				ushort[][,,] triangles = new ushort[materialCount][,,];
				uint[][,] uvIndices = new uint[materialCount][,];

				string[] materialNames = new string[materialCount];
				float[] materialAlphas = new float[materialCount];
				bool[] materialsSmooth = new bool[materialCount];
				byte[] materialsSpecular = new byte[materialCount];
				bool[] materialsTransparent = new bool[materialCount];
				byte[][] materialColors = new byte[materialCount][];
				string[] materialTextures = new string[materialCount];

				//Triangle data
				for (ushort material = 0; material < materialCount; material++)
				{
					ushort triangleCount = br.ReadUInt16();
					ushort normal = br.ReadUInt16();

					ushort[,,] tempTriangles = new ushort[triangleCount, 3, 2];
					for (ushort triangle = 0; triangle < triangleCount; triangle++)
					{
						for (ushort vertex = 0; vertex < 3; vertex++)
						{
							tempTriangles[triangle, vertex, 0] = br.ReadUInt16();
							tempTriangles[triangle, vertex, 1] = br.ReadUInt16();
						}
					}
					triangles[material] = tempTriangles;

					uint uvIndexCount = br.ReadUInt32() / 3;
					uint[,] tempUVIndices = new uint[uvIndexCount, 3];

					for (uint uvIndex = 0; uvIndex < uvIndexCount; uvIndex++)
					{
						tempUVIndices[uvIndex, 0] = br.ReadUInt32();
						tempUVIndices[uvIndex, 1] = br.ReadUInt32();
						tempUVIndices[uvIndex, 2] = br.ReadUInt32();
					}
					uvIndices[material] = tempUVIndices;

					byte[] materialColor = { br.ReadByte(), br.ReadByte(), br.ReadByte() };
					float materialAlpha = br.ReadSingle();
					bool materialSmooth = br.ReadBoolean();
					byte materialSpecular = br.ReadByte();
					bool materialTransparent = br.ReadBoolean();
					modFile.Seek(1, SeekOrigin.Current);
					string gifName = new string(br.ReadChars(br.ReadInt32()));
					Console.WriteLine(gifName);
					string materialName = new string(br.ReadChars(br.ReadInt32()));
					Console.WriteLine(materialName + ": " + materialColor[0] + ", " + materialColor[1] + ", " + materialColor[2]);
					materialNames[material] = materialName;
					materialAlphas[material] = materialAlpha;
					materialsSmooth[material] = materialSmooth;
					materialsSpecular[material] = materialSpecular;
					materialsTransparent[material] = materialTransparent;

					materialColors[material] = materialColor;
					materialTextures[material] = gifName;
				}
				CreateMTL(modelName + "_" + (LODCount - 1 - lod), materialNames, materialAlphas, materialsSpecular, materialsTransparent, materialColors, materialTextures);
				ConvertToOBJ(modelName + "_" + (LODCount - 1 - lod), vertices, normals, uvs, materialNames, materialsSmooth, triangles, uvIndices);
			}
		}

		//Not so interesting stuff
		static void ParseTexture(string textureName)
		{
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
			ConvertToBMP(textureName, width, height, palette, pixels);
		}

		static void CreateMTL(string modelName, string[] materialNames, float[] materialAlphas, byte[] materialsSpecular, bool[] materialsTransparent, byte[][] materialColors, string[] materialTextures)
		{
			Directory.CreateDirectory(fileName);
			FileStream mtlFile = new FileStream(fileName + @"\" + modelName + ".MTL", FileMode.Create);
			StreamWriter sw = new StreamWriter(mtlFile, Encoding.ASCII);
			sw.AutoFlush = true;
			for (int material = 0; material < materialNames.Length; material++)
			{
				string materialName = materialNames[material];
				sw.WriteLine("newmtl " + materialNames[material]);
				sw.WriteLine("Ka 1.0 1.0 1.0");
				sw.WriteLine("Kd " + materialColors[material][0] / 255f + " " + materialColors[material][1] / 255f + " " + materialColors[material][2] / 255f);
				if (materialsSpecular[material] > 0)
					sw.WriteLine("Ks 1.0 1.0 1.0");
				else
					sw.WriteLine("Ks 0.0 0.0 0.0");
				sw.WriteLine("Ns " + (materialsSpecular[material] / 511f * 1000));
				if (materialsTransparent[material] && materialAlphas[material] > 0)
					sw.WriteLine("d " + materialAlphas[material]);
				sw.WriteLine("illum 2");
				if (materialTextures[material] != "")
					sw.WriteLine("map_Kd " + materialTextures[material].Replace(".GIF", ".BMP").Replace(".gif", ".bmp").Replace("^", ""));
			}
		}

		//A popular and easy format
		static void ConvertToOBJ(string modelName, float[,] vertices, float[,] normals, float[,] uvs, string[] materials, bool[] materialsSmooth, ushort[][,,] triangles, uint[][,] uvIndices)
		{
			Directory.CreateDirectory(fileName);
			FileStream objFile = new FileStream(fileName + @"\" + modelName + ".OBJ", FileMode.Create);
			StreamWriter sw = new StreamWriter(objFile, Encoding.ASCII);
			sw.AutoFlush = true;
			sw.WriteLine("mtllib " + modelName + ".MTL");
			sw.WriteLine("g " + modelName, 2);
			for (int vertex = 0; vertex < vertices.GetLength(0); vertex++)
			{
				sw.WriteLine("v " + vertices[vertex, 0] + " " + vertices[vertex, 1] + " " + -vertices[vertex, 2]);
			}

			for (int normal = 0; normal < normals.GetLength(0); normal++)
			{
				sw.WriteLine("vn " + normals[normal, 0] + " " + normals[normal, 1] + " " + -normals[normal, 2]);
			}

			for (int uv = 0; uv < uvs.GetLength(0); uv++)
			{
				sw.WriteLine("vt " + uvs[uv, 0] + " " + uvs[uv, 1]);
			}

			for (int material = 0; material < materials.Length; material++)
			{
				sw.WriteLine("usemtl " + materials[material]);
				List<ushort> vertexDefinition = new List<ushort>();
				List<ushort> vertexIndices = new List<ushort>();
				List<ushort> normalDefinition = new List<ushort>();
				List<ushort> normalIndices = new List<ushort>();
				List<uint?> texCrdDefinition = new List<uint?>();
				List<uint?> texCrdIndices = new List<uint?>();
				for (int triangle = 0; triangle < triangles[material].GetLength(0); triangle++)
				{
					for (int vertex = 0; vertex < 3; vertex++)
					{
						ushort vertice = triangles[material][triangle, vertex, 0];
						ushort normal = triangles[material][triangle, vertex, 1];
						uint? uvIndex = null;
						if (uvIndices[material].GetLength(0) > 0)
						{
							uvIndex = uvIndices[material][triangle, vertex];
						}

						if (normal >= 32768)
						{
							vertexDefinition.Add(vertice);
							vertexIndices.Add((ushort)(vertice + 1));
							normalDefinition.Add((ushort)(normal - 32768));
							normalIndices.Add((ushort)(normal - 32768 + 1));
							texCrdDefinition.Add(uvIndex);
							if (uvIndex != null)
								texCrdIndices.Add(uvIndex + 1);
							else
								texCrdIndices.Add(null);
                        }
						else
                        {
                            vertexIndices.Add((ushort)(vertexDefinition[vertice] + 1));
							normalIndices.Add((ushort)(normalDefinition[vertice] + 1));
                            if (texCrdDefinition[vertice] != null)
								texCrdIndices.Add(texCrdDefinition[vertice] + 1);
                            else
                                texCrdIndices.Add(null);
                        }
					}
				}
				vertexIndices.Reverse();
				texCrdIndices.Reverse();
				normalIndices.Reverse();
				for (int vertexIndex = 0; vertexIndex < vertexIndices.Count; vertexIndex += 3)
				{
					string face = "f";
					for (int vertex = 0; vertex < 3; vertex++)
					{
						if (texCrdIndices.Count > 0)
						{
							if (texCrdIndices[vertexIndex + vertex] != null)
							{
								if (materialsSmooth[material])
									face += " " + vertexIndices[vertexIndex + vertex] + "/" + texCrdIndices[vertexIndex + vertex] + "/" + normalIndices[vertexIndex + vertex];
								else
                                    face += " " + vertexIndices[vertexIndex + vertex] + "/" + texCrdIndices[vertexIndex + vertex] + "/" + normalIndices[vertexIndex + vertex];
                            }
							else
							{
								if (materialsSmooth[material])
									face += " " + vertexIndices[vertexIndex + vertex] + "//" + normalIndices[vertexIndex + vertex];
								else
									face += " " + vertexIndices[vertexIndex + vertex] + "//" + normalIndices[vertexIndex + 2];
							}
						}
						else
						{
							if (materialsSmooth[material])
								face += " " + vertexIndices[vertexIndex + vertex] + "//" + normalIndices[vertexIndex + vertex];
							else
								face += " " + vertexIndices[vertexIndex + vertex] + "//" + normalIndices[vertexIndex + 2];
						}
					}
					sw.WriteLine(face);
				}
			}
		}

		//The closest format to LI's .TEX is .BMP
		static void ConvertToBMP(string textureName, uint width, uint height, byte[,] palette, byte[] pixels)
		{
			uint fileLength = (uint)(width * height + palette.GetLength(0) * 4 + 54);
			Directory.CreateDirectory(fileName);
			FileStream bmpFile = new FileStream(fileName + @"\" + textureName.Replace(".GIF", ".BMP").Replace(".gif", ".bmp"), FileMode.Create);
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
		}
	}
}
