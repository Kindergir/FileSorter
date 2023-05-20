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
			// TODO
			Number = 9;
			Content = "Bla";
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