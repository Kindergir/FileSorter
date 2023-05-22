using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileSorter
{
	internal class ParallelBatchesWriter
	{
		private volatile int _finishedTasksCount;
		private int _tasksCount;

		public ParallelBatchesWriter()
		{
			_tasksCount = -1;
		}

		public async Task AddTask(List<Line> content, string fileName)
		{
			WriteOneBatch(content, fileName).Start();
		}

		public void SetTasksCount(int tasksCount)
		{
			_tasksCount = tasksCount;
		}

		public bool AllTasksCompleted()
		{
			return _tasksCount != -1 && _tasksCount == _finishedTasksCount;
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