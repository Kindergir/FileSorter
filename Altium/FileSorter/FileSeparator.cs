using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace FileSorter
{
	public static class FileSeparator
	{
		// for always using an insertion sort on batch
		private const int MaxBatchSize = 5000;

		// TODO move to common
		private const int BufferSize = 128;

		public static HashSet<string> SeparateFile(string fileNameWithPath)
		{
			int batchLength = 1000000; // 1000 kilobytes
			long batchesCount = 0;
			using (var fileStream = File.OpenRead(fileNameWithPath))
			{
				batchesCount = fileStream.Length / batchLength;
				if (fileStream.Length % batchLength != 0)
				{
					batchesCount += 1;
				}
			}
			//using var streamReader = new StreamReader(fileStream, Encoding.UTF8, false, BufferSize);

			var currentTempFileNumber = 0;
			var currentTempFileName = $"temp_{currentTempFileNumber}.txt";
			var tempFilesNames = new HashSet<string>();

			// var batchesWriter = new ParallelBatchesWriter();
			//
			// var batch = new List<Line>();
			// var previousLine = new Line();


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

				fifFileSorter.SortOneFile(fullFileName);

				// var currentTempFile = MemoryMappedFile.CreateFromFile(fullFileName, batchLength, MemoryMappedFileAccess.ReadWrite);
				var currentTempFile = MemoryMappedFile.CreateFromFile(
					fullFileName,
					FileMode.OpenOrCreate,
					$"bla{currentTempFileNumber}",
					batchLength,
					MemoryMappedFileAccess.ReadWrite);
				
				currentTempFile.CreateViewAccessor().WriteArray(0, currentBatch, 0, batchLength);

				++currentTempFileNumber;
				currentPosition += batchLength;
			}

			SpinWait.SpinUntil(() => fifFileSorter.AllTasksCompleted(), TimeSpan.FromMilliseconds(15));

			/*while (streamReader.ReadLine() is { } line)
			{
				var parsedLine = new Line(line);
				batch.Add(parsedLine);

				if (batch.Count >= MaxBatchSize)
				{
					if (previousLine > parsedLine)
					{
						var fullFileName = Path.Combine(Directory.GetCurrentDirectory(), currentTempFileName);
						batchesWriter.AddTask(batch, fullFileName);

						tempFilesNames.Add(fullFileName);

						++currentTempFileNumber;
						currentTempFileName = $"temp_{currentTempFileNumber}.txt";
						batch = new List<Line>();
					}
				}

				previousLine = parsedLine;
			}

			batchesWriter.SetTasksCount(tempFilesNames.Count);
			SpinWait.SpinUntil(() => batchesWriter.AllTasksCompleted(), TimeSpan.FromMilliseconds(15));*/
			return tempFilesNames;
		}
	}
}