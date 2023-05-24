using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;
using FileSorter.Models;

namespace FileSorter
{
	public static class FileSeparator
	{
		public static async Task<HashSet<string>> SeparateFile(string fileNameWithPath)
		{
			int batchLength = 10000000; // 10000 kilobytes
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

			var filesSorter = new ParallelFilesSorter(batchesCount);
			//var filesToSort = new List<FileDataForSort>();

			Console.WriteLine("Separation file started");
			var sw = new Stopwatch();
			sw.Start();

			var currentPosition = 0;
			var currentBatch = new byte [batchLength];
			using var mmf = MemoryMappedFile.CreateFromFile(fileNameWithPath, FileMode.Open, "ImgA");
			for (int i = 0; i < batchesCount; i++)
			{
				int currentBatchLength = i == batchesCount - 1 ? lastBatchLength : batchLength;
				mmf.CreateViewAccessor().ReadArray(currentPosition, currentBatch, 0, currentBatchLength);

				currentTempFileName = $"temp_{currentTempFileNumber}.txt";
				var fullFileName = Path.Combine(Directory.GetCurrentDirectory(), currentTempFileName);
				tempFilesNames.Add(fullFileName);

				var currentTempFile = MemoryMappedFile.CreateFromFile(
					fullFileName,
					FileMode.OpenOrCreate,
					$"bla{currentTempFileNumber}",
					currentBatchLength,
					MemoryMappedFileAccess.ReadWrite);

				var accessor = currentTempFile.CreateViewAccessor();
				accessor.WriteArray(0, currentBatch, 0, currentBatchLength);
				accessor.Dispose();
				currentTempFile.Dispose();

				filesSorter.SortOneFile(fullFileName, i == 0, i == batchesCount -1, currentPosition + batchLength);
				// filesToSort.Add(new FileDataForSort(
				// 	i == 0,
				// 	i == batchesCount - 1,
				// 	fullFileName,
				// 	currentPosition + batchLength));

				++currentTempFileNumber;
				currentPosition += currentBatchLength;
			}

			//await filesSorter.SortFiles(filesToSort);

			if (batchesCount > 1)
			{
				currentTempFileName = $"temp_{currentTempFileNumber}.txt";
				var lastFullFileName = Path.Combine(Directory.GetCurrentDirectory(), currentTempFileName);
				tempFilesNames.Add(lastFullFileName);
				filesSorter.SortLastFile(lastFullFileName, batchLength);
			}

			SpinWait.SpinUntil(() => filesSorter.AllTasksCompleted(), TimeSpan.FromMilliseconds(15));
			sw.Stop();
			Console.WriteLine($"Separation file stopped, it took {sw.ElapsedMilliseconds}");

			return tempFilesNames;
		}
	}
}