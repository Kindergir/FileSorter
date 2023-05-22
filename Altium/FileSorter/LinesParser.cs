using System;

namespace FileSorter
{
	public static class LinesParser
	{
		public static Line ToLine(this string line)
		{
			return new Line(line);
		}

		public static bool IsLine(this string line, out Line result)
		{
			result = new Line(); //TODO
			if (line.Length < 4)
			{
				return false;
			}

			var dotPosition = line.IndexOf('.', StringComparison.Ordinal);
			if (dotPosition == -1)
			{
				return false;
			}

			var numberParsingIsSuccess = int.TryParse(line.AsSpan(0, dotPosition), out var number);
			if (!numberParsingIsSuccess)
			{
				return false;
			}

			if (!line.EndsWith(Environment.NewLine))
			{
				return false;
			}

			result = new Line(number, line.AsSpan(dotPosition + 2).ToString(), line);
			return true;
		}
	}
}