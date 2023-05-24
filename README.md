# FileSorter

გამარჯობა!

This solution contains 2 programs: file generator and file sorter.

## How to use the generator

Build and run project FileGenerator.
You should enter the expected file size in kilobutes and wait.

Also you can use ready file from repository: "small" is a test file.

## How to use the sorter

Build and run project FileSorter.
You should enter the input file name. Note, that if you enter name without full path, we will try to use work directory.

## Where is the result?

The result (file) will be in the application work directory.
You will see the name of result file when work is finished.

## Pay attention

FileSorter uses temporary files. It always keeps they in work directory.
If at least one temporary file is interrupted, process will be interrupted at all. Just think that it's our issue in backlog :)

And also, if the process was interrupted, you should clean all temporary files by yourself.