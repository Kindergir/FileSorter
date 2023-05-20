using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileSorter
{
	public static class FileSeparator
	{
		// for always using an insertion sort on batch
		private const int MaxBatchSize = 300;
		private const int BufferSize = 128;

		public static List<string> SeparateFile(string fileNameWithPath)
		{
			using var fileStream = File.OpenRead(fileNameWithPath);
			using var streamReader = new StreamReader(fileStream, Encoding.UTF8, false, BufferSize);

			var currentBatchSize = 0;

			var currentTempFileNumber = 0;
			var currentTempFileName = $"temp_{currentTempFileNumber}.txt";
			var tempFilesNames = new List<string>();

			var batch = new Line[MaxBatchSize];
			while (streamReader.ReadLine() is { } line)
			{
				var parsedLine = new Line(line);
				batch[currentBatchSize] = parsedLine;
				++currentBatchSize;

				if (currentBatchSize >= MaxBatchSize)
				{
					InsertionSorter.Sort(batch);

					var fullFileName = Path.Combine(Directory.GetCurrentDirectory(), currentTempFileName);
					FlashTemporaryFile(batch, fullFileName);
					tempFilesNames.Add(fullFileName);

					++currentTempFileNumber;
					currentTempFileName = $"temp_{currentTempFileNumber}.txt";
					currentBatchSize = 0;
				}
			}

			return tempFilesNames;
		}

		private static void FlashTemporaryFile(Line[] lines, string fileName)
		{
			//File.Create(fileName, BufferSize);
			using var writer = new StreamWriter(fileName, true, Encoding.UTF8);
			foreach (var line in lines)
			{
				// TODO encoding
				writer.WriteLine(Encoding.UTF8.GetBytes(line.OriginalValue));
			}
			writer.Flush();
		}
	}
}