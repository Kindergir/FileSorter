using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileSorter.Mappers;
using FileSorter.Models;

namespace FileSorter
{
	internal class FilesSorter
	{
		private readonly ConcurrentDictionary<long, string> _tornLinesAtStart = new ConcurrentDictionary<long, string>();
		private readonly ConcurrentDictionary<long, string> _tornLinesAtEnd = new ConcurrentDictionary<long, string>();

		public async Task SortFiles(List<BatchDataForSort> files, string inputFileNameWithPath)
		{
			Console.WriteLine("Sorting files started");
			var sw = new Stopwatch();
			sw.Start();

			var parallelismDegree = Math.Min(files.Count, Environment.ProcessorCount);
			var semaphore = new SemaphoreSlim(0, parallelismDegree);

			var tasks = new List<Task>(files.Count);
			foreach (var file in files)
			{
				tasks.Add(InternalSortOneFile(file, inputFileNameWithPath, semaphore));
			}

			semaphore.Release(parallelismDegree);
			await Task.WhenAll(tasks);

			for (int i = 0; i < parallelismDegree; i++)
			{
				await semaphore.WaitAsync();
			}

			sw.Stop();
			Console.WriteLine($"Sorting files stopped, it took {sw.ElapsedMilliseconds}");
		}

		private async Task InternalSortOneFile(
			BatchDataForSort batchDataForSort,
			string inputFileNameWithPath,
			SemaphoreSlim semaphore)
		{
			var currentLine = 0;
			var batch = new List<Line>(4000000);
			Console.WriteLine($"ENTER {batchDataForSort.StartOffset}");
			await semaphore.WaitAsync();
			Console.WriteLine($"START {batchDataForSort.StartOffset}");
			using (var fileReader = File.OpenRead(inputFileNameWithPath))
			{
				fileReader.Seek(batchDataForSort.StartOffset, SeekOrigin.Begin);
				using (var reader = new StreamReader(fileReader, Encoding.UTF8, false, Consts.Megabyte))
				{
					while (!reader.EndOfStream && fileReader.Position < batchDataForSort.InterruptionOffset)
					{
						var line = reader.ReadLine();

						if (currentLine == 0 && !batchDataForSort.IsItFirstFile)
						{
							if (!string.IsNullOrWhiteSpace(line))
							{
								_tornLinesAtStart.TryAdd(batchDataForSort.InterruptionOffset, line); // possible problem on failed try
								currentLine++;
							}
							continue;
						}

						if (fileReader.Position >= batchDataForSort.InterruptionOffset && !batchDataForSort.IsItLastFile)
						{
							if (!string.IsNullOrWhiteSpace(line))
							{
								_tornLinesAtEnd.TryAdd(batchDataForSort.InterruptionOffset, line); // possible problem on failed try
								currentLine++;
							}
							continue;
						}

						if (!line.ToLine(out var parsedLine))
						{
							currentLine++;
							continue;
						}

						batch.Add(parsedLine);
					}
				}
			}

			batch.Sort();
			semaphore.Release();
			Console.WriteLine($"RELEASE {batchDataForSort.StartOffset}");
			RewriteTemporaryFile(batch, batchDataForSort.NameWithPath);
			Console.WriteLine($"EXIT {batchDataForSort.StartOffset}");
		}

		private static void RewriteTemporaryFile(List<Line> lines, string fileName)
		{
			using var stream = File.Create(fileName);
			using var writer = new StreamWriter(stream, Encoding.UTF8, Consts.Megabyte);
			foreach (var line in lines)
			{
				writer.WriteLine(line.OriginalValue);
			}
			writer.Flush();
		}

		public void SortLastFile(string outputFileName, long batchSize)
		{
			var listOfFullLines = new List<Line>(_tornLinesAtStart.Count + 5);
			foreach (var line in _tornLinesAtStart)
			{
				if (!_tornLinesAtEnd.ContainsKey(line.Key - batchSize))
				{
					// temporary solution
					continue;
				}
				var half = _tornLinesAtEnd[line.Key - batchSize];
				$"{half}{line.Value}".ToLine(out var parsedLine);
				listOfFullLines.Add(parsedLine);
			}

			listOfFullLines.Sort();

			using var stream = File.Create(outputFileName);
			using var writer = new StreamWriter(stream, Encoding.UTF8, Consts.Megabyte);
			foreach (var line in listOfFullLines)
			{
				writer.WriteLine(line.OriginalValue);
			}
			writer.Flush();
		}
	}
}