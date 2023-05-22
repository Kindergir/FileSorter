using System;

namespace FileSorter
{
	public struct Line
	{
		public int Number { get; }
		public string Content { get; }
		public string OriginalValue { get; }

		public Line(int number, string content, string originalValue)
		{
			Number = number;
			Content = content;
			OriginalValue = originalValue;
		}

		public Line(string line)
		{
			var dotPosition = line.IndexOf('.', StringComparison.Ordinal);
			Number = int.Parse(line.AsSpan(0, dotPosition));
			Content = line.AsSpan(dotPosition + 2).ToString();
			OriginalValue = line;
		}

		public static bool operator > (Line first, Line second)
		{
			var comparingResult = string.Compare(first.Content, second.Content, StringComparison.Ordinal);
			return comparingResult == 0
				? first.Number > second.Number
				: comparingResult > 0;
		}

		public static bool operator < (Line first, Line second)
		{
			var comparingResult = string.Compare(first.Content, second.Content, StringComparison.Ordinal);
			return comparingResult == 0
				? first.Number < second.Number
				: comparingResult < 0;
		}

		public static bool operator >= (Line first, Line second)
		{
			var comparingResult = string.Compare(first.Content, second.Content, StringComparison.Ordinal);
			return comparingResult == 0
				? first.Number >= second.Number
				: comparingResult >= 0;
		}

		public static bool operator <= (Line first, Line second)
		{
			var comparingResult = string.Compare(first.Content, second.Content, StringComparison.Ordinal);
			return comparingResult == 0
				? first.Number <= second.Number
				: comparingResult <= 0;
		}
	}
}