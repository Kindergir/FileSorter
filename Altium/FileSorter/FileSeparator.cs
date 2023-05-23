using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;

namespace FileSorter
{
	struct FileDataForSort
	{
		public FileDataForSort(bool isItFirstFile, bool isItLastFile, string nameWithPath, int interruptionOffset)
		{
			IsItFirstFile = isItFirstFile;
			IsItLastFile = isItLastFile;
			NameWithPath = nameWithPath;
			InterruptionOffset = interruptionOffset;
		}

		public bool IsItFirstFile { get; }
		public bool IsItLastFile { get; }
		public string NameWithPath { get; }
		public int InterruptionOffset { get; }
	}
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

			var fifFileSorter = new FifFileSorter(batchesCount);
			var filesToSort = new List<FileDataForSort>();

			Console.WriteLine("Separation file started");
			var sw = new Stopwatch();
			sw.Start();

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

				filesToSort.Add(new FileDataForSort(
					i == 0,
					i == batchesCount - 1,
					fullFileName,
					currentPosition + batchLength));

				++currentTempFileNumber;
				currentPosition += currentBatchLength;
			}

			await fifFileSorter.SortFiles(filesToSort);

			if (batchesCount > 1)
			{
				currentTempFileName = $"temp_{currentTempFileNumber}.txt";
				var lastFullFileName = Path.Combine(Directory.GetCurrentDirectory(), currentTempFileName);
				tempFilesNames.Add(lastFullFileName);
				fifFileSorter.SortLastFile(lastFullFileName, batchLength);
			}

			sw.Stop();
			Console.WriteLine($"Separation file stopped, it took {sw.ElapsedMilliseconds}");

			return tempFilesNames;
		}
	}
}