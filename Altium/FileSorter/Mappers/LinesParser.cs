using FileSorter.Models;

namespace FileSorter.Mappers
{
	public static class LinesParser
	{
		public static Line ToLine(this string line)
		{
			return new Line(line);
		}
	}
}