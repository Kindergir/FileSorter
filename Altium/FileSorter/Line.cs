using System;

namespace FileSorter
{
	public struct Line : IComparable<Line>, IEquatable<Line>
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

		public int CompareTo(Line other)
		{
			var comparingResult = string.Compare(Content, other.Content, StringComparison.Ordinal);
			if (comparingResult != 0)
			{
				return comparingResult;
			}

			if (Number == other.Number)
			{
				return 0;
			}

			return Number > other.Number
				? 1
				: -1;
		}

		public bool Equals(Line other)
		{
			return Number == other.Number
			       && Content == other.Content
			       && OriginalValue == other.OriginalValue;
		}

		public override bool Equals(object obj)
		{
			return obj is Line other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Number, Content, OriginalValue);
		}
	}
}