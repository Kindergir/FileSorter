using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileGenerator
{
	public class FileGenerator
	{
		private readonly Random _random = new Random();
		private const string Letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

		public string Generate(int sizeInKilobytes)
		{
			var resultFileName = $"Result_generation_{_random.Next(0, int.MaxValue)}";
			var resultFilePath = Path.Combine(Directory.GetCurrentDirectory(), resultFileName);

			var repeatingData = new List<string>(100);

			using var writer = new StreamWriter(resultFilePath);
			while (true)
			{
				string currentLineContent = null;

				var currentRepetitionFlag = _random.Next(0, 5);
				if (currentRepetitionFlag == 5)
				{
					if (repeatingData.Count == 0)
					{
						currentLineContent = GenerateRandomContent();
						repeatingData.Add(currentLineContent);
					}
					else
					{
						var currentRandomRepetitionIndex = _random.Next(0, repeatingData.Count - 1);
						currentLineContent = repeatingData[currentRandomRepetitionIndex];
					}
				}
				else if (repeatingData.Count < 100)
				{
					currentLineContent = GenerateRandomContent();

					if (repeatingData.Count < 100)
					{
						var currentKeepingFlag = _random.Next(0, 10);
						if (currentKeepingFlag == 10)
						{
							repeatingData.Add(currentLineContent);
						}
					}
				}

				var currentLine = $"{_random.Next(0, 100000)}. {currentLineContent}";
				writer.WriteLine(currentLine);

				var fileInfo = new FileInfo(resultFilePath);
				if (fileInfo.Length / 1000 >= sizeInKilobytes)
				{
					break;
				}
			}

			return resultFilePath;
		}

		private string GenerateRandomContent()
		{
			var randomLength = _random.Next(1, 500);
			var builder = new StringBuilder();
			for (int i = 0; i < randomLength; ++i)
			{
				var letterIndex = _random.Next(0, Letters.Length);
				var letter = Letters[letterIndex];
				builder.Append(letter);
			}
			return builder.ToString();
		}
	}
}