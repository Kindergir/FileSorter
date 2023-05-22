using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileSorter
{
	internal class ParallelBatchesWriter
	{
		// for always using an insertion sort on batch
		private const int MaxBatchSize = 5000;

		private List<Line> currentLines = new List<Line>(MaxBatchSize);
		private volatile int _finishedTasksCount;
		private int _tasksCount;
		private int currentTempFileNumber;
		private string currentTempFileName;

		public HashSet<string> TempFilesNames { get; private set; }

		public ParallelBatchesWriter()
		{
			currentTempFileNumber = 0;
			currentTempFileName = $"temp_{currentTempFileNumber}.txt";
			TempFilesNames = new HashSet<string>();
			_tasksCount = 0;
		}

		public async Task AddLine(Line line)
		{
			currentLines.Add(line);

			if (currentLines.Count > MaxBatchSize)
			{
				var fullFileName = Path.Combine(Directory.GetCurrentDirectory(), currentTempFileName);
				TempFilesNames.Add(fullFileName);

				var linesCopy = currentLines;
				WriteOneBatch(linesCopy, fullFileName).Start();

				currentLines = new List<Line>(MaxBatchSize);
				++currentTempFileNumber;
				++_tasksCount;
				currentTempFileName = $"temp_{currentTempFileNumber}.txt";
			}
		}

		public bool AllTasksCompleted()
		{
			return _tasksCount != 0 && _tasksCount == _finishedTasksCount;
		}

		private async Task WriteOneBatch(List<Line> content, string fileName)
		{
			InsertionSorter.Sort(content);
			var fullFileName = Path.Combine(Directory.GetCurrentDirectory(), fileName);
			FlashTemporaryFile(content, fullFileName);

			Interlocked.Increment(ref _finishedTasksCount);
		}

		private static void FlashTemporaryFile(List<Line> lines, string fileName)
		{
			using var writer = new StreamWriter(fileName, true, Encoding.UTF8);
			foreach (var line in lines)
			{
				// TODO encoding
				writer.WriteLine(line.OriginalValue);
			}
			writer.Flush();
		}
	}
}