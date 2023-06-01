using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileGenerator
{
	public class FileGenerator
	{
		private readonly Random _random = new Random();
		private const int _wordsCount = 100000;
		private const int _number1024 = 1024;
		private const string Letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
		private readonly List<string> _words = new List<string>(_wordsCount);

		public FileGenerator()
		{
			for (int i = 0; i < _wordsCount; i++)
			{
				_words.Add(GenerateRandomContent());
			}
		}

		public string Generate(int sizeInKilobytes)
		{
			var resultFileName = $"Result_generation_{_random.Next(0, int.MaxValue)}";
			var resultFilePath = Path.Combine(Directory.GetCurrentDirectory(), resultFileName);

			using var writer = new StreamWriter(resultFilePath, true, Encoding.UTF8, 128);
			long kilobytes = 0;
			while (true)
			{
				var currentIndex = _random.Next(0, _words.Count - 1);
				var currentLineContent = _words[currentIndex];

				var currentLine = $"{_random.Next(0, 100000)}. {currentLineContent}";
				writer.WriteLine(currentLine);

				var fileInfo = new FileInfo(resultFilePath);
				var fileSizeInKilobytes = fileInfo.Length / _number1024;
				if (fileSizeInKilobytes >= sizeInKilobytes)
				{
					break;
				}

				if (fileSizeInKilobytes - kilobytes >= _number1024)
				{
					Console.WriteLine($"SIZE IS {fileSizeInKilobytes} KILOBYTES");
					kilobytes = fileSizeInKilobytes;
					writer.Flush();
				}
			}

			return resultFilePath;
		}

		private string GenerateRandomContent()
		{
			var randomLength = _random.Next(1, 30);
			
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