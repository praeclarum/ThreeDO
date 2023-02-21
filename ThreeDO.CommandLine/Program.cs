using ThreeDO;

Console.WriteLine("3DO Tool by Frank A. Krueger");

var batch = new BatchConversion();

for (var i = 0; i < args.Length;) {
    var arg = args[i];
    if (Path.GetExtension(arg).ToLowerInvariant() == ".3do")
    {
        batch.AddFiles(new[] { arg });
        i++;
    }
    else if (arg == "-i")
    {
        if (i + 1 < args.Length)
        {
            batch.SearchDirectories.Directories.Add(args[i + 1]);
        }
        i += 2;
    }
    else
    {
        Console.WriteLine($"Unrecognized arg \"{arg}\"");
    }
}

if (batch.Files.Count == 0)
{
    Console.WriteLine("Usage: 3dotool <3do files> -i <search directories>");
    return 1;
}

try
{
    await batch.ExportDaeFilesAsync(p => Console.WriteLine("{0:0.0}%", p * 100));
}
catch (Exception ex)
{
    Console.WriteLine($"{ex}");
    return 2;
}

return 0;
