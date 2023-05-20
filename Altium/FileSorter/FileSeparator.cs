using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileSorter
{
	public class FileSeparator
	{
		// for always using an insertion sort on batch
		private const int MaxBatchSize = 300;
		private const int BufferSize = 128;

		public IList<string> SeparateFile(string fileNameWithPath)
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
					FlashTemporaryFile(batch, currentTempFileName);

					tempFilesNames.Add(currentTempFileName);

					++currentTempFileNumber;
					currentTempFileName = $"temp_{currentTempFileNumber}.txt";
					currentBatchSize = 0;
				}
			}

			return tempFilesNames;
		}

		private void FlashTemporaryFile(Line[] lines, string fileName)
		{
			File.Create(Path.Combine(Directory.GetCurrentDirectory(), fileName), BufferSize);
			using var writer = new StreamWriter(fileName, true);
			foreach (var line in lines)
			{
				// TODO encoding
				writer.WriteLine(Encoding.UTF8.GetBytes(line.OriginalValue));
			}
			writer.Flush();
		}
	}
}