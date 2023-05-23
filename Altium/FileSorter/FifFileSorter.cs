using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileSorter
{
	internal class FifFileSorter
	{
		private volatile int _finishedTasksCount;
		private readonly long _tasksCount;

		private readonly ConcurrentDictionary<int, string> _tornLinesAtStart = new ConcurrentDictionary<int, string>();
		private readonly ConcurrentDictionary<int, string> _tornLinesAtEnd = new ConcurrentDictionary<int, string>();

		public FifFileSorter(long tasksCount)
		{
			_tasksCount = tasksCount;
			_finishedTasksCount = 0;
		}

		public async Task SortOneFile(
			string inputFileName,
			bool firstFile,
			bool lastFile,
			int startBatchOffset,
			int endBatchOffset)
		{
			var lines = (await File.ReadAllLinesAsync(inputFileName));
			var batch = new List<Line>();

			for (int i = 0; i < lines.Length; i++)
			{
				if (i == 0 && !firstFile)
				{
					_tornLinesAtStart.TryAdd(endBatchOffset, lines.First()); // possible problem on failed try
					continue;
				}

				if (i == lines.Length - 1 && !lastFile)
				{
					_tornLinesAtEnd.TryAdd(endBatchOffset, lines.Last()); // possible problem on failed try
					continue;
				}
				
				batch.Add(lines[i].ToLine());
			}

			batch.Sort();
			RewriteTemporaryFile(batch, inputFileName);
			Interlocked.Increment(ref _finishedTasksCount);
		}

		private static void RewriteTemporaryFile(List<Line> lines, string fileName)
		{
			File.Delete(fileName);
			using var stream = File.Create(fileName);
			using var writer = new StreamWriter(stream, Encoding.UTF8);
			foreach (var line in lines)
			{
				// TODO encoding
				writer.WriteLine(line.OriginalValue);
			}
			writer.Flush();
		}

		public bool AllTasksCompleted()
		{
			return _tasksCount != -1 && _tasksCount == _finishedTasksCount;
		}

		public void SortLastFile(string outputFileName, int batchSize)
		{
			var listOfFullLines = new List<Line>();
			foreach (var line in _tornLinesAtStart)
			{
				var half = _tornLinesAtEnd[line.Key - batchSize];
				listOfFullLines.Add($"{half}{line.Value}".ToLine());
			}

			listOfFullLines.Sort();

			using var stream = File.Create(outputFileName);
			using var writer = new StreamWriter(stream, Encoding.UTF8);
			foreach (var line in listOfFullLines)
			{
				writer.WriteLine(line.OriginalValue);
			}
			writer.Flush();
		}
	}
}