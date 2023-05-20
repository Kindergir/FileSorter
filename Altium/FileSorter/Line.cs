using System;

namespace FileSorter
{
	public struct Line
	{
		public int Number { get; }
		public string Content { get; }
		public string OriginalValue { get; }

		public Line(string line)
		{
			var dotPosition = line.IndexOf('.', StringComparison.Ordinal);
			Number = int.Parse(line.AsSpan(0, dotPosition));
			Content = line.AsSpan(dotPosition + 2).ToString();
			OriginalValue = line;
		}

		public static bool operator > (Line first, Line second)
		{
			if (first.Number == second.Number)
			{
				return string.Compare(first.Content, second.Content, StringComparison.Ordinal) > 0;
			}
			return first.Number > second.Number;
		}

		public static bool operator < (Line first, Line second)
		{
			if (first.Number == second.Number)
			{
				return string.Compare(first.Content, second.Content, StringComparison.Ordinal) < 0;
			}
			return first.Number < second.Number;
		}

		public static bool operator >= (Line first, Line second)
		{
			if (first.Number == second.Number)
			{
				return string.Compare(first.Content, second.Content, StringComparison.Ordinal) >= 0;
			}
			return first.Number >= second.Number;
		}

		public static bool operator <= (Line first, Line second)
		{
			if (first.Number == second.Number)
			{
				return string.Compare(first.Content, second.Content, StringComparison.Ordinal) <= 0;
			}
			return first.Number <= second.Number;
		}
	}
}