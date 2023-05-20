using System.Collections.Generic;
using System.IO;

namespace FileSorter
{
	internal static class TempFilesCleaner
	{
		public static void CleanAllFiles(HashSet<string> filesPaths)
		{
			foreach (var filesPath in filesPaths)
			{
				File.Delete(filesPath);
			}
		}
	}
}