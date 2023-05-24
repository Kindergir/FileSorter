namespace FileSorter.Models
{
	internal struct BatchDataForSort
	{
		public BatchDataForSort(bool isItFirstFile, bool isItLastFile, string nameWithPath, int startOffset, int interruptionOffset)
		{
			IsItFirstFile = isItFirstFile;
			IsItLastFile = isItLastFile;
			NameWithPath = nameWithPath;
			InterruptionOffset = interruptionOffset;
			StartOffset = startOffset;
		}

		public bool IsItFirstFile { get; }
		public bool IsItLastFile { get; }
		public string NameWithPath { get; }
		public int StartOffset { get; }
		public int InterruptionOffset { get; }
	}
}