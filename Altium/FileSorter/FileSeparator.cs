using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FileSorter.Models;

namespace FileSorter
{
	public static class FileSeparator
	{
		public static async Task<HashSet<string>> SeparateFile(string fileNameWithPath)
		{
			int batchLength = 1000000; // 1000 kilobytes
			long batchesCount = 0;
			long fileSize = 0;
			using (var fileStream = File.OpenRead(fileNameWithPath))
			{
				fileSize = fileStream.Length;
			}

			batchesCount = fileSize > batchLength ? fileSize / batchLength : 1;
			int lastBatchLength = (int)(fileSize % batchLength);
			if (lastBatchLength != 0 && fileSize > batchLength)
			{
				batchesCount += 1;
			}

			var currentTempFileNumber = 0;
			string currentTempFileName;
			var tempFilesNames = new HashSet<string>();

			var filesSorter = new ParallelFilesSorter();
			var filePertsToSort = new List<BatchDataForSort>();

			Console.WriteLine("Separation file started");
			var sw = new Stopwatch();
			sw.Start();

			var currentPosition = 0;
			for (int i = 0; i < batchesCount; i++)
			{
				int currentBatchLength = i == batchesCount - 1 ? lastBatchLength : batchLength;
				currentTempFileName = $"temp_{currentTempFileNumber}.txt";
				var fullFileName = Path.Combine(Directory.GetCurrentDirectory(), currentTempFileName);
				tempFilesNames.Add(fullFileName);

				filePertsToSort.Add(new BatchDataForSort(
					i == 0,
					i == batchesCount - 1,
					fullFileName,
					currentPosition,
					currentPosition + batchLength));

				++currentTempFileNumber;
				currentPosition += currentBatchLength;
			}

			await filesSorter.SortFiles(filePertsToSort, fileNameWithPath);

			if (batchesCount > 1)
			{
				currentTempFileName = $"temp_{currentTempFileNumber}.txt";
				var lastFullFileName = Path.Combine(Directory.GetCurrentDirectory(), currentTempFileName);
				tempFilesNames.Add(lastFullFileName);
				filesSorter.SortLastFile(lastFullFileName, batchLength);
			}

			sw.Stop();
			Console.WriteLine($"Separation file stopped, it took {sw.ElapsedMilliseconds}");

			return tempFilesNames;
		}
	}
}