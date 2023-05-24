namespace FileSorter.Models
{
	internal struct FileDataForSort
	{
		public FileDataForSort(bool isItFirstFile, bool isItLastFile, string nameWithPath, int interruptionOffset)
		{
			IsItFirstFile = isItFirstFile;
			IsItLastFile = isItLastFile;
			NameWithPath = nameWithPath;
			InterruptionOffset = interruptionOffset;
		}

		public bool IsItFirstFile { get; }
		public bool IsItLastFile { get; }
		public string NameWithPath { get; }
		public int InterruptionOffset { get; }
	}
}