namespace FileSorter
{
	public static class InsertionSorter
	{
		public static void Sort(Line[] input)
		{
			for (var i = 1; i < input.Length; ++i)
			{
				var tempValue = input[i];
				var j = i;

				while (j > 0 && input[j - 1] > tempValue)
				{
					input[j] = input[j - 1];
					j = j - 1;
				}

				input[j] = tempValue;
			}
		}
	}
}