using System;
using FileSorter.Models;

namespace FileSorter.Mappers
{
	public static class LinesParser
	{
		public static bool ToLine(this string line, out Line result)
		{
			result = new Line();

			var dotPosition = line.IndexOf('.', StringComparison.Ordinal);
			if (dotPosition == -1)
			{
				return false;
			}

			if (!long.TryParse(line.AsSpan(0, dotPosition), out var number))
			{
				return false;
			}

			if (line.Length - dotPosition < 2)
			{
				return false;
			}

			var content = line.AsSpan(dotPosition + 2).ToString();
			if (string.IsNullOrWhiteSpace(content))
			{
				return false;
			}

			result = new Line(number, content, line);
			return true;
		}
	}
}