using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileSorter.Mappers;

namespace FileSorter
{
	public static class FilesMerger
	{
		private const int BufferSize = 128;

		public static async Task<string> Merge(HashSet<string> filesPaths)
		{
			Console.WriteLine("Merging started");
			var sw = new Stopwatch();
			sw.Start();

			var currentMergedFileNumber = filesPaths.Count;
			while (filesPaths.Count > 1)
			{
				filesPaths = await ParallelPairMerge(filesPaths, currentMergedFileNumber);
				currentMergedFileNumber += filesPaths.Count;
			}
			var result = filesPaths.First();
			
			var currentDirectory = Directory.GetCurrentDirectory();

			var resultFileName = $"Result_of_work_{new Random().Next(0, int.MaxValue)}.txt";
			File.Move(Path.Combine(currentDirectory, result), Path.Combine(currentDirectory, resultFileName));

			sw.Stop();
			Console.WriteLine($"Merging stopped, it took {sw.ElapsedMilliseconds}");
			return resultFileName;
		}

		private static async Task<HashSet<string>> ParallelPairMerge(HashSet<string> filesPaths, int currentMergedFileNumber)
		{
			if (filesPaths.Count == 1)
			{
				return filesPaths;
			}

			var mergedFiles = new HashSet<string>();

			int currentFileNumber = 0;
			string previousFileName = "";

			var parallelismDegree = filesPaths.Count > Environment.ProcessorCount * 5
				? Environment.ProcessorCount * 5
				: filesPaths.Count;
			var semaphore = new SemaphoreSlim(parallelismDegree, parallelismDegree);

			var filesToMerge = new List<(string first, string second)>();
			foreach (var path in filesPaths)
			{
				if (currentFileNumber == 0)
				{
					currentFileNumber = 1;
				}
				else
				{
					filesToMerge.Add((previousFileName, path));
					currentFileNumber = 0;
				}
				previousFileName = path;
			}

			if (currentFileNumber == 1)
			{
				mergedFiles.Add(previousFileName);
			}

			foreach (var filesPair in filesToMerge)
			{
				await semaphore.WaitAsync();

				var mergedFileName = $"temp_{currentMergedFileNumber}.txt";
				MergeOnePair(mergedFileName, filesPair.first, filesPair.second, semaphore);

				++currentMergedFileNumber;
				mergedFiles.Add(mergedFileName);
			}

			for (int i = 0; i < parallelismDegree; i++)
			{
				await semaphore.WaitAsync();
			}

			return mergedFiles;
		}

		private static async Task MergeOnePair(string resultFileName, string firstFilePath, string secondFilePath, SemaphoreSlim semaphore)
		{
			using var writer = new StreamWriter(resultFileName, true, Encoding.UTF8);

			using var firstFileReader = new StreamReader(firstFilePath, Encoding.UTF8, false, BufferSize);
			using var secondFileReader = new StreamReader(secondFilePath, Encoding.UTF8, false, BufferSize);

			var firstParsedLine = (firstFileReader.ReadLine()).ToLine();
			var secondParsedLine = (secondFileReader.ReadLine()).ToLine();

			while (true)
			{
				if (firstParsedLine > secondParsedLine)
				{
					writer.WriteLine(secondParsedLine.OriginalValue);
					secondParsedLine = (secondFileReader.ReadLine()).ToLine();
				}
				else
				{
					writer.WriteLine(firstParsedLine.OriginalValue);
					firstParsedLine = (firstFileReader.ReadLine()).ToLine();
				}

				if (firstFileReader.EndOfStream || secondFileReader.EndOfStream)
				{
					break;
				}
			}

			while (!firstFileReader.EndOfStream)
			{
				writer.WriteLine(firstFileReader.ReadLine());
			}

			while (!secondFileReader.EndOfStream)
			{
				writer.WriteLine(secondFileReader.ReadLine());
			}

			await writer.FlushAsync();

			await writer.DisposeAsync();
			firstFileReader.Dispose();
			secondFileReader.Dispose();

			File.Delete(firstFilePath);
			File.Delete(secondFilePath);

			semaphore.Release();
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

			while (!firstFileReader.EndOfStream)
			{
				writer.WriteLine(firstFileReader.ReadLine());
			}

			while (!secondFileReader.EndOfStream)
			{
				writer.WriteLine(secondFileReader.ReadLine());
			}

			writer.Flush();

			return resultFileName;
		}
	}
}