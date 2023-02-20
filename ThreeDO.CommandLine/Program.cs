using ThreeDO;

Console.WriteLine("3DO Tools by Frank A. Krueger");

string[] inputFiles =
    Environment.GetCommandLineArgs()
    .Where(x =>
        Path.GetExtension(x).Equals(".3do", StringComparison.InvariantCultureIgnoreCase))
    .ToArray();

if (inputFiles.Length == 0)
{
    Console.WriteLine("Usage: 3dotools <input files>");
    return 1;
}

var batch = new BatchConversion();

batch.AddFiles(inputFiles);

batch.OutputDirectory = Path.Combine(
    Path.GetDirectoryName(inputFiles[0]) ?? string.Empty,
    "output");

await batch.ExportDaeFilesAsync(p => Console.WriteLine(p));


return 0;
