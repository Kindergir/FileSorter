using System;
using System.IO;

namespace FileSorter
{
	internal class LineSelector : IDisposable
	{
		private readonly StreamReader _reader;
		private readonly string _filePath;

		public LineSelector(StreamReader reader, string filePath)
		{
			_reader = reader;
			_filePath = filePath;
			CurrentLine = _reader.ReadLine().ToLine();
		}

		public Line CurrentLine { get; private set; }
		public bool EndOfStream => _reader.EndOfStream;

		public void ReadLine()
		{
			CurrentLine = _reader.ReadLine().ToLine();
		}

		public void Dispose()
		{
			_reader?.Dispose();
			File.Delete(_filePath);
		}
	}
}