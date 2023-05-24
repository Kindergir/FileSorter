using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileSorter.Mappers;
using FileSorter.Models;

namespace FileSorter
{
	internal class ParallelFilesSorter
	{
		private readonly ConcurrentDictionary<int, string> _tornLinesAtStart = new ConcurrentDictionary<int, string>();
		private readonly ConcurrentDictionary<int, string> _tornLinesAtEnd = new ConcurrentDictionary<int, string>();

		public async Task SortFiles(List<BatchDataForSort> files, string inputFileNameWithPath)
		{
			Console.WriteLine("Sorting files started");
			var sw = new Stopwatch();
			sw.Start();

			var parallelismDegree = Math.Min(files.Count, Environment.ProcessorCount * 5);
			var semaphore = new SemaphoreSlim(parallelismDegree, parallelismDegree);
			foreach (var file in files)
			{
				await semaphore.WaitAsync();
				InternalSortOneFile(file, inputFileNameWithPath, semaphore);
			}

			for (int i = 0; i < parallelismDegree; i++)
			{
				await semaphore.WaitAsync();
			}

			sw.Stop();
			Console.WriteLine($"Sorting files stopped, it took {sw.ElapsedMilliseconds}");
		}

		public async Task InternalSortOneFile(
			BatchDataForSort batchDataForSort,
			string inputFileNameWithPath,
			SemaphoreSlim semaphore)
		{
			var currentLine = 0;
			//var batchSize = batchDataForSort.InterruptionOffset - batchDataForSort.StartOffset;
			var batch = new List<Line>();
			using (var fileReader = File.OpenRead(inputFileNameWithPath))
			{
				fileReader.Seek(batchDataForSort.StartOffset, SeekOrigin.Begin);
				using (var reader = new StreamReader(fileReader))
				{
					while (!reader.EndOfStream && fileReader.Position < batchDataForSort.InterruptionOffset)
					{
						var line = reader.ReadLine();

						if (currentLine == 0 && !batchDataForSort.IsItFirstFile)
						{
							_tornLinesAtStart.TryAdd(batchDataForSort.InterruptionOffset, line); // possible problem on failed try
                            currentLine++;
							continue;
						}

						if (fileReader.Position >= batchDataForSort.InterruptionOffset && !batchDataForSort.IsItLastFile)
						{
							_tornLinesAtEnd.TryAdd(batchDataForSort.InterruptionOffset, line); // possible problem on failed try
							currentLine++;
							continue;
						}

						batch.Add(line.ToLine());
						currentLine++;
					}
				}
			}

			batch.Sort();
			RewriteTemporaryFile(batch, batchDataForSort.NameWithPath);
			semaphore.Release();
		}

		private static void RewriteTemporaryFile(List<Line> lines, string fileName)
		{
			//File.Delete(fileName);

			using var stream = File.Create(fileName);
			using var writer = new StreamWriter(stream, Encoding.UTF8);
			foreach (var line in lines)
			{
				writer.WriteLine(line.OriginalValue);
			}
			writer.Flush();
		}

		public void SortLastFile(string outputFileName, int batchSize)
		{
			var listOfFullLines = new List<Line>();
			foreach (var line in _tornLinesAtStart)
			{
				if (!_tornLinesAtEnd.ContainsKey(line.Key - batchSize))
				{
					// temporary solution
					continue;
				}
				var half = _tornLinesAtEnd[line.Key - batchSize];
				listOfFullLines.Add($"{half}{line.Value}".ToLine());
			}

			listOfFullLines.Sort();

			using var stream = File.Create(outputFileName);
			using var writer = new StreamWriter(stream, Encoding.UTF8);
			foreach (var line in listOfFullLines)
			{
				writer.WriteLine(line.OriginalValue);
			}
			writer.Flush();
		}
	}
}