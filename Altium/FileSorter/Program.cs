using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FileSorter
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Hello! Just enter the file name. If you enter name without full path, we will try to use work directory.");

			var enteredFileName = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(enteredFileName))
			{
				Console.WriteLine("Incorrect input: file name cannot be empty.");
				return;
			}

			var fullFileName = GetFullFileName(enteredFileName);
			if (fullFileName == null)
			{
				Console.WriteLine("Incorrect input: file should be exist.");
				return;
			}

			var sw = new Stopwatch();
			sw.Start();

			var tempFilesNames = FileSeparator.SeparateFile(fullFileName).GetAwaiter().GetResult();

			string resultFileName;
			if (tempFilesNames.Count > 1)
			{
				resultFileName = (FilesMerger.Merge(tempFilesNames));
				TempFilesCleaner.CleanAllFiles(tempFilesNames);
			}
			else
			{
				resultFileName = tempFilesNames.First();
			}

			sw.Stop();

			Console.WriteLine($"Great! Program used {sw.ElapsedMilliseconds} ms. Count of temporary files is {tempFilesNames.Count}. Result file name is {resultFileName}.");
		}

		private static string GetFullFileName(string enteredFileName)
		{
			var probableFileName = Path.Combine(Directory.GetCurrentDirectory(), enteredFileName);
			if (File.Exists(enteredFileName))
			{
				return enteredFileName;
			}

			return File.Exists(probableFileName) ? probableFileName : null;
		}
	}
}