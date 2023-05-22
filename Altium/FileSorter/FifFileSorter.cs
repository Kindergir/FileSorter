using System;
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
		private readonly ConcurrentDictionary<int, (string originalLine, bool wasAtTheEnd)> _tornLines
			= new ConcurrentDictionary<int, (string, bool)>();

		public FifFileSorter(long tasksCount)
		{
			_tasksCount = tasksCount;
		}

		public async Task SortOneFile(
			string inputFileName,
			bool firstFile,
			bool lastFile,
			int startBatchOffset,
			int endBatchOffset)
		{
			var batch = new List<Line>();
			using (var fileStream = File.OpenRead(inputFileName))
			{
				using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, false, 128))
				{
					int i = 0;
					while (await streamReader.ReadLineAsync() is { } line)
					{
						if (!firstFile && i == 0)
						{
							_tornLines.TryAdd(startBatchOffset, (line, false)); // possible problem on failed try
							i = 1;
							continue;
						}

						++i;

						try
						{
							batch.Add(line.ToLine());
						}
						catch
						{
							_tornLines.TryAdd(endBatchOffset, (line, true)); // possible problem on failed try
						}
					}
				}
			}

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

		public void SortLastFile(string outputFileName)
		{
			var listOfFullLines = new List<string>();
			foreach (var line in _tornLines)
			{
				if (!line.Value.wasAtTheEnd)
				{
					continue;
				}

				var half = _tornLines[line.Key + 1];
				listOfFullLines.Add($"{line.Value.originalLine}{half.originalLine}");
			}

			using var stream = File.Create(outputFileName);
			using var writer = new StreamWriter(stream, Encoding.UTF8);
			foreach (var line in listOfFullLines)
			{
				writer.WriteLine(line);
			}
			writer.Flush();
		}
	}
}