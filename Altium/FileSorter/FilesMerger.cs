using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FileSorter
{
	public static class FilesMerger
	{
		private const int BufferSize = 128;

		public static string Merge(HashSet<string> filesPaths)
		{
			// TODO fix output file name and path
			var resultFileName = $"Result_of_work_{new Random().Next(0, int.MaxValue)}.txt";
			//return RecursivePairMerge(filesPaths, filesPaths.Count).First();
			return MergeByLine(filesPaths, resultFileName);
		}

		private static string MergeByLine(HashSet<string> filesPaths, string outputFileName)
		{
			using var writer = new StreamWriter(outputFileName, true, Encoding.UTF8);

			var readers = filesPaths
				.Select(x => new LineSelector(new StreamReader(x, Encoding.UTF8, false, BufferSize), x))
				.ToList();

			while (readers.Count > 0)
			{
				var minLine = FindMinLine(readers);
				writer.WriteLine(minLine.selector.CurrentLine.OriginalValue);

				if (readers[minLine.index].EndOfStream)
				{
					readers[minLine.index].Dispose();
					readers.RemoveAt(minLine.index);
				}
				else
				{
					readers[minLine.index].ReadLine();
				}
			}

			return outputFileName;
		}

		private static (LineSelector selector, int index) FindMinLine(List<LineSelector> lines)
		{
			LineSelector minLine = null;
			var minIndex = 0;
			for (int i = 0; i < lines.Count; ++i)
			{
				if (minLine == null)
				{
					minLine = lines[i];
					minIndex = 0;
				}
				else if (minLine.CurrentLine > lines[i].CurrentLine)
				{
					minLine = lines[i];
					minIndex = i;
				}
			}
			return (minLine, minIndex);
		}

		private static HashSet<string> RecursivePairMerge(HashSet<string> filesPaths, int currentMergedFileNumber)
		{
			if (filesPaths.Count == 1)
			{
				return filesPaths;
			}

			var currentLevelPairs = new HashSet<string>();

			int currentFileNumber = 0;
			string previousFileName = "";

			foreach (var path in filesPaths)
			{
				if (currentFileNumber == 0)
				{
					currentFileNumber = 1;
				}
				else
				{
					var mergedFileName = MergeFilesPair(previousFileName, path, currentMergedFileNumber);

					File.Delete(previousFileName);
					File.Delete(path);

					++currentMergedFileNumber;
					currentLevelPairs.Add(mergedFileName);
					currentFileNumber = 0;
				}

				previousFileName = path;
			}

			if (currentFileNumber == 1)
			{
				currentLevelPairs.Add(previousFileName);
			}

			return RecursivePairMerge(currentLevelPairs, currentMergedFileNumber);
		}

		private static string MergeFilesPair(string firstFilePath, string secondFilePath, int tempFileCount)
		{
			var resultFileName = $"temp_{tempFileCount}.txt";
			using var writer = new StreamWriter(resultFileName, true, Encoding.UTF8);

			using var firstFileReader = new StreamReader(firstFilePath, Encoding.UTF8, false, BufferSize);
			using var secondFileReader = new StreamReader(secondFilePath, Encoding.UTF8, false, BufferSize);

			var firstParsedLine = firstFileReader.ReadLine().ToLine();
			var secondParsedLine = secondFileReader.ReadLine().ToLine();

			while (true)
			{
				if (firstParsedLine > secondParsedLine)
				{
					writer.WriteLine(secondParsedLine.OriginalValue);
					secondParsedLine = secondFileReader.ReadLine().ToLine();
				}
				else
				{
					writer.WriteLine(firstParsedLine.OriginalValue);
					firstParsedLine = firstFileReader.ReadLine().ToLine();
				}

				if (firstFileReader.EndOfStream || secondFileReader.EndOfStream)
				{
					break;
				}
			}

			if (!firstFileReader.EndOfStream)
			{
				while (!firstFileReader.EndOfStream)
				{
					writer.WriteLine(firstFileReader.ReadLine());
				}
			}

			if (!secondFileReader.EndOfStream)
			{
				while (!secondFileReader.EndOfStream)
				{
					writer.WriteLine(secondFileReader.ReadLine());
				}
			}

			writer.Flush();

			return resultFileName;
		}
	}
}