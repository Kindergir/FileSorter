using System;
using System.Diagnostics;

namespace FileGenerator
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Hi! I'll generate file for you. Just say, how many kilobytes do you need.");
			Console.WriteLine("Please note, that I can generate a file that is at least 1 kilobyte in size.");

			var inputSize = Console.ReadLine();
			var parseResult = int.TryParse(inputSize, out var size);

			if (!parseResult)
			{
				Console.WriteLine("Incorrect input: file size must be an integer.");
			}

			if (size <= 0)
			{
				Console.WriteLine("Incorrect input: file size must be greater then zero.");
			}

			var sw = new Stopwatch();
			sw.Start();

			var generator = new FileGenerator();
			var resultFileName = generator.Generate(size);

			sw.Stop();

			Console.WriteLine($"Result file name is {resultFileName}.");
			Console.WriteLine($"Generation took {sw.ElapsedMilliseconds} ms.");
		}
	}
}