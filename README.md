# FileSorter

გამარჯობა :)

Solution contains 2 programs: file generator and file sorter.

## How to use the sorter

Open archive from repository and run FileSorter.exe. Also you can build and run project FileSorter, but remember, that it will be slowly in Debug mode.

You should enter the input file name. Note, that if you enter name without full path, we will try to use work directory.
If you enter path to non-existent file, program will end.

## How to use the generator

Open archive from repository and run FileGenerator.exe. Also you can build and run project FileGenerator, but remember, that it will be slowly in Debug mode.
You should enter the expected file size in kilobutes and wait.

Also you can use ready files from repository, see "Ready test files" title.

## Where is the result?

The result (file) will be in the application work directory.
You will see the name of result file when work is finished.

## Input file format

Sorter can work with text file, which contains lines like "Number. String".
All incorrect lines will be skipped. If file contains only incorrect lines, result file will be empty.

### Example
234. Op
2. Bla bla bla

### Ready test files

You can use ready files from archive "test files.7z". It contains correct (with different sizes) and incorrect files.

## Pay attention

FileSorter uses temporary files. It always keeps they in work directory.
If at least one temporary file is interrupted, process will be interrupted at all. Just think that it's our issue in backlog :)

And also, if the process was interrupted, you should clean all temporary files by yourself.