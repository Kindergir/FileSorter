using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace FileSorter
{
	public static class FileSeparator
	{
		public static HashSet<string> SeparateFile(string fileNameWithPath)
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

			var fifFileSorter = new FifFileSorter(batchesCount);

			var currentPosition = 0;
			using var mmf = MemoryMappedFile.CreateFromFile(fileNameWithPath, FileMode.Open, "ImgA");
			for (int i = 0; i < batchesCount; i++)
			{
				int currentBatchLength = i == batchesCount - 1 ? lastBatchLength : batchLength;
				var currentBatch = new byte [currentBatchLength];
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

				fifFileSorter.SortOneFile(fullFileName,
					i == 0,
					i == batchesCount - 1,
					currentPosition,
					currentPosition + batchLength);

				++currentTempFileNumber;
				currentPosition += currentBatchLength;
			}

			SpinWait.SpinUntil(() => fifFileSorter.AllTasksCompleted(), TimeSpan.FromMilliseconds(15));

			if (batchesCount > 1)
			{
				currentTempFileName = $"temp_{currentTempFileNumber}.txt";
				var lastFullFileName = Path.Combine(Directory.GetCurrentDirectory(), currentTempFileName);
				tempFilesNames.Add(lastFullFileName);
				fifFileSorter.SortLastFile(lastFullFileName, batchLength);
			}

			return tempFilesNames;
		}
	}
}