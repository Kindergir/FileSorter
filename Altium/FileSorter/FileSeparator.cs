using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace FileSorter
{
	public static class FileSeparator
	{
		// TODO move to common
		private const int BufferSize = 128;

		public static HashSet<string> SeparateFile(string fileNameWithPath)
		{
			using var fileStream = File.OpenRead(fileNameWithPath);
			using var streamReader = new StreamReader(fileStream, Encoding.UTF8, false, BufferSize);

			var batchesWriter = new ParallelBatchesWriter();

			while (streamReader.ReadLine() is { } line)
			{
				var parsedLine = new Line(line);
				batchesWriter.AddLine(parsedLine);
			}

			SpinWait.SpinUntil(() => batchesWriter.AllTasksCompleted(), TimeSpan.FromMilliseconds(15));
			return batchesWriter.TempFilesNames;
		}
	}
}