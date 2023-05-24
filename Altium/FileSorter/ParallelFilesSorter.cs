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

		public async Task SortFiles(List<FileDataForSort> files)
		{
			Console.WriteLine("Sorting files started");
			var sw = new Stopwatch();
			sw.Start();

			var parallelismDegree = Math.Min(files.Count, 200);
			var semaphore = new SemaphoreSlim(parallelismDegree, parallelismDegree);
			foreach (var file in files)
			{
				await semaphore.WaitAsync();
				InternalSortOneFile(file.NameWithPath, file.IsItFirstFile, file.IsItLastFile, file.InterruptionOffset, semaphore);
			}

			for (int i = 0; i < parallelismDegree; i++)
			{
				await semaphore.WaitAsync();
			}

			sw.Stop();
			Console.WriteLine($"Sorting files stopped, it took {sw.ElapsedMilliseconds}");
		}

		public async Task InternalSortOneFile(
			string inputFileName,
			bool isFirstFile,
			bool isLastFile,
			int endBatchOffset,
			SemaphoreSlim semaphore)
		{
			var lines = await File.ReadAllLinesAsync(inputFileName);
			var batch = new List<Line>();

			for (int i = 0; i < lines.Length; i++)
			{
				if (i == 0 && !isFirstFile)
				{
					_tornLinesAtStart.TryAdd(endBatchOffset, lines.First()); // possible problem on failed try
					continue;
				}

				if (i == lines.Length - 1 && !isLastFile)
				{
					_tornLinesAtEnd.TryAdd(endBatchOffset, lines.Last()); // possible problem on failed try
					continue;
				}
				
				batch.Add(lines[i].ToLine());
			}

			batch.Sort();
			RewriteTemporaryFile(batch, inputFileName);
			semaphore.Release();
		}

		private static void RewriteTemporaryFile(List<Line> lines, string fileName)
		{
			File.Delete(fileName);
			using var stream = File.Create(fileName);
			using var writer = new StreamWriter(stream, Encoding.UTF8);
			foreach (var line in lines)
			{
				// TODO encoding
				writer.WriteLine(line.OriginalValue);
			}
			writer.Flush();
		}

		public void SortLastFile(string outputFileName, int batchSize)
		{
			var listOfFullLines = new List<Line>();
			foreach (var line in _tornLinesAtStart)
			{
				if (!_tornLinesAtEnd.ContainsKey(line.Key))
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