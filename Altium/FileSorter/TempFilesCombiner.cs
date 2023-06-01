using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileSorter
{
	internal static class TempFilesCombiner
	{
		public static string GetResultFileName(HashSet<string> tempFilesNames)
		{
			var merger = new FilesMerger();
			string tempResultFileName;
			if (tempFilesNames.Count > 1)
			{
				tempResultFileName = merger.Merge(tempFilesNames).GetAwaiter().GetResult();
				TempFilesCleaner.CleanAllFiles(tempFilesNames);
			}
			else
			{
				tempResultFileName = tempFilesNames.First();
			}

			var currentDirectory = Directory.GetCurrentDirectory();

			var resultFileName = $"Result_of_work_{new Random().Next(0, int.MaxValue)}.txt";
			File.Move(Path.Combine(currentDirectory, tempResultFileName), Path.Combine(currentDirectory, resultFileName));
			return resultFileName;
		}
	}
}