using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
			using var fileStream = File.OpenRead(fileNameWithPath);
			using var streamReader = new StreamReader(fileStream, Encoding.UTF8, false, BufferSize);

			var currentTempFileNumber = 0;
			var currentTempFileName = $"temp_{currentTempFileNumber}.txt";
			var tempFilesNames = new HashSet<string>();

			var batchesWriter = new ParallelBatchesWriter();

			var batch = new List<Line>();
			var previousLine = new Line();
			while (streamReader.ReadLine() is { } line)
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
			SpinWait.SpinUntil(() => batchesWriter.AllTasksCompleted(), TimeSpan.FromMilliseconds(15));
			return tempFilesNames;
		}
	}
}