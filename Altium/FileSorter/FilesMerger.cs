using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FileSorter.Mappers;

namespace FileSorter
{
	public class FilesMerger
	{
		private volatile int _alreadyFinishedMerges = 0;
		private ConcurrentBag<string> _readyFiles = new ConcurrentBag<string>();

		public async Task<string> Merge(HashSet<string> filesPaths)
		{
			Console.WriteLine("Merging started");
			var sw = new Stopwatch();
			sw.Start();

			var currentMergedFileNumber = filesPaths.Count;

			var result = await TestPairMerge(filesPaths, currentMergedFileNumber);
			var currentDirectory = Directory.GetCurrentDirectory();

			var resultFileName = $"Result_of_work_{new Random().Next(0, int.MaxValue)}.txt";
			File.Move(Path.Combine(currentDirectory, result), Path.Combine(currentDirectory, resultFileName));

			sw.Stop();
			Console.WriteLine($"Merging stopped, it took {sw.ElapsedMilliseconds}");
			return resultFileName;
		}

		private async Task<string> TestPairMerge(
			HashSet<string> filesPaths,
			int currentMergedFileNumber)
		{
			var channel = Channel.CreateUnbounded<(string firstFileName, string secondFileName, string outputFileName)>();

			int currentFileNumber = 0;
			string previousFileName = "";
			foreach (var filePath in filesPaths)
			{
				if (currentFileNumber == 0)
				{
					currentFileNumber = 1;
				}
				else
				{
					_readyFiles.Add(previousFileName);
					_readyFiles.Add(filePath);

					await channel.Writer.WriteAsync((
						previousFileName,
						filePath,
						$"temp_{currentMergedFileNumber++}.txt"));

					currentFileNumber = 0;
				}
				previousFileName = filePath;
			}

			Console.WriteLine("Consume enter");

			var mergesCount = filesPaths.Count - (1 + filesPaths.Count % 2);

			ConsumeWithAwaitForeachAsync(
				channel.Reader,
				channel.Writer,
				currentMergedFileNumber,
				mergesCount);

			Console.WriteLine($"EXPECTED IS {mergesCount}");
			var waiting = SpinWait.SpinUntil(() => _alreadyFinishedMerges == mergesCount, TimeSpan.FromHours(2));

			var resultFileName = $"temp_{filesPaths.Count + mergesCount - 1}.txt";
			if (currentFileNumber == 1)
			{
				var newResultFileName = $"temp_{filesPaths.Count + mergesCount}.txt";
				SyncMergeOnePair(newResultFileName, previousFileName, resultFileName);
				resultFileName = newResultFileName;
			}

			Console.WriteLine("Consume exit");
			Console.WriteLine("Consume complete");
			return resultFileName;
		}

		private async void ConsumeWithAwaitForeachAsync(
			ChannelReader<(string firstFileName, string secondFileName, string outputFileName)> reader,
			ChannelWriter<(string firstFileName, string secondFileName, string outputFileName)> writer,
			int currentMergedFileNumber,
			int mergesCount)
		{
			int currentFileNumber = 0;
			string previousFileName = "";

			//var tasks = new List<Task>(Environment.ProcessorCount);
			await foreach (var files in reader.ReadAllAsync())
			{
				//tasks.Add(MergeOnePair(files.outputFileName, files.firstFileName, files.secondFileName));
				Task.Run(() => MergeOnePair(files.outputFileName, files.firstFileName, files.secondFileName));
				if (currentFileNumber == 1)
				{
					await writer.WriteAsync((
						previousFileName,
						files.outputFileName,
						$"temp_{currentMergedFileNumber++}.txt"));

					currentFileNumber = 0;
				}
				else
				{
					previousFileName = files.outputFileName;
					currentFileNumber = 1;
				}
			}
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

			var filesToMerge = new List<(string first, string second)>(filesPaths.Count / 2);
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

			var parallelismDegree = Math.Min(filesPaths.Count, Environment.ProcessorCount);
			var semaphore = new SemaphoreSlim(0, parallelismDegree);

			var tasks = new List<Task>(filesToMerge.Count);
			foreach (var filesPair in filesToMerge)
			{
				async Task Action()
				{
					var mergedFileName = $"temp_{currentMergedFileNumber++}.txt";
					await MergeOnePair(mergedFileName, filesPair.first, filesPair.second, semaphore);
					mergedFiles.Add(mergedFileName);
				}

				tasks.Add(Action());
			}

			semaphore.Release(parallelismDegree);
			await Task.WhenAll(tasks);

			for (int i = 0; i < parallelismDegree; i++)
			{
				await semaphore.WaitAsync();
			}

			return mergedFiles;
		}

		private static async Task MergeOnePair(string resultFileName, string firstFilePath, string secondFilePath, SemaphoreSlim semaphore)
		{
			Console.WriteLine($"ENTER {firstFilePath} + {secondFilePath} = {resultFileName}");
			await semaphore.WaitAsync();

			Console.WriteLine($"START {firstFilePath} + {secondFilePath} = {resultFileName}");
			using var writer = new StreamWriter(resultFileName, true, Encoding.UTF8, Consts.Megabytes50);

			using var firstFileReader = new StreamReader(firstFilePath, Encoding.UTF8, false, Consts.Megabytes50);
			using var secondFileReader = new StreamReader(secondFilePath, Encoding.UTF8, false, Consts.Megabytes50);

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
			Console.WriteLine($"EXIT {firstFilePath} + {secondFilePath} = {resultFileName}");
		}

		private async Task MergeOnePair(string resultFileName, string firstFilePath, string secondFilePath)
		{
			SpinWait.SpinUntil(() =>
			{
				return _readyFiles.Contains(firstFilePath) && _readyFiles.Contains(secondFilePath);
			}, TimeSpan.FromHours(2));

			Console.WriteLine($"START {firstFilePath} + {secondFilePath} = {resultFileName}");
			using var writer = new StreamWriter(resultFileName, true, Encoding.UTF8, Consts.Megabytes50);

			using var firstFileReader = new StreamReader(firstFilePath, Encoding.UTF8, false, Consts.Megabytes50);
			using var secondFileReader = new StreamReader(secondFilePath, Encoding.UTF8, false, Consts.Megabytes50);

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

			writer.Flush();
			writer.Dispose();

			firstFileReader.Dispose();
			secondFileReader.Dispose();

			File.Delete(firstFilePath);
			File.Delete(secondFilePath);

			Console.WriteLine($"EXIT {firstFilePath} + {secondFilePath} = {resultFileName}");

			_readyFiles.Add(resultFileName);
			Interlocked.Increment(ref _alreadyFinishedMerges);
			Console.WriteLine($"CURRENT IS {_alreadyFinishedMerges}");
		}

		private void SyncMergeOnePair(string resultFileName, string firstFilePath, string secondFilePath)
		{
			using var writer = new StreamWriter(resultFileName, true, Encoding.UTF8, Consts.Megabytes50);

			using var firstFileReader = new StreamReader(firstFilePath, Encoding.UTF8, false, Consts.Megabytes50);
			using var secondFileReader = new StreamReader(secondFilePath, Encoding.UTF8, false, Consts.Megabytes50);

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

			writer.Flush();
			writer.Dispose();

			firstFileReader.Dispose();
			secondFileReader.Dispose();

			File.Delete(firstFilePath);
			File.Delete(secondFilePath);
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
			using var writer = new StreamWriter(resultFileName, true, Encoding.UTF8, Consts.Megabytes50);

			using var firstFileReader = new StreamReader(firstFilePath, Encoding.UTF8, false, Consts.Megabytes50);
			using var secondFileReader = new StreamReader(secondFilePath, Encoding.UTF8, false, Consts.Megabytes50);

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