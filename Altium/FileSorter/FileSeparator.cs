using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;

namespace FileSorter
{
	public static class FileSeparator
	{
		public static HashSet<string> SeparateFile(string fileNameWithPath)
		{
			int batchLength = 1000000; // 1000 kilobytes
			long batchesCount = 0;
			long fileSize = 0;
			using (var fileStream = File.OpenRead(fileNameWithPath))
			{
				fileSize = fileStream.Length;
			}

			batchesCount = fileSize / batchLength;
			if (fileSize % batchLength != 0)
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
				var currentBatch = new byte [batchLength];
				mmf.CreateViewAccessor().ReadArray(currentPosition, currentBatch, 0, batchLength);

				currentTempFileName = $"temp_{currentTempFileNumber}.txt";
				var fullFileName = Path.Combine(Directory.GetCurrentDirectory(), currentTempFileName);
				tempFilesNames.Add(fullFileName);

				var currentTempFile = MemoryMappedFile.CreateFromFile(
					fullFileName,
					FileMode.OpenOrCreate,
					$"bla{currentTempFileNumber}",
					batchLength,
					MemoryMappedFileAccess.ReadWrite);

				var accessor = currentTempFile.CreateViewAccessor();
				accessor.WriteArray(0, currentBatch, 0, batchLength);
				accessor.Dispose();
				currentTempFile.Dispose();

				fifFileSorter.SortOneFile(fullFileName,
					i == 0,
					i == batchesCount - 1,
					currentPosition,
					currentPosition + batchLength);

				++currentTempFileNumber;
				currentPosition += batchLength;
			}

			SpinWait.SpinUntil(() => fifFileSorter.AllTasksCompleted(), TimeSpan.FromMilliseconds(15));

			//TODO that must be returned
			//currentTempFileName = $"temp_{currentTempFileNumber}.txt";
			//var lastFullFileName = Path.Combine(Directory.GetCurrentDirectory(), currentTempFileName);
			//tempFilesNames.Add(lastFullFileName);
			//fifFileSorter.SortLastFile(lastFullFileName);

			return tempFilesNames;
		}
	}
}