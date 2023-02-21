using System.Collections.ObjectModel;

using MvvmHelpers;

namespace ThreeDO
{
    public class BatchConversion : ObservableObject
    {
        public ObservableCollection<BatchConversionFile> Files { get; } = new();

        public SearchDirectories SearchDirectories { get; } = new();

        private string _outputDirectory = "";
        public string OutputDirectory { get => _outputDirectory; set => SetProperty(ref _outputDirectory, value); }
        public IEnumerable<string> UserFileReferences
        {
            get
            {
                foreach (var f in Files)
                    yield return f.FilePath;
                foreach (var g in SearchDirectories.GobPaths)
                    yield return g;
                foreach (var d in SearchDirectories.Directories)
                    yield return d;
            }
        }

        public void AddFiles(string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                Files.Add(new BatchConversionFile(filePath));
            }
            OnPropertyChanged(nameof(UserFileReferences));
        }

        void Warn(string message)
        {
            Console.WriteLine($"WARNING: {message}");
        }

        public async Task ExportDaeFilesAsync(Action<double>? progress)
        {
            if (Files.Count < 1)
                return;
            var p = 0.0;
            var dp = 1.0 / Files.Count;
            var fileTasks = Files.Select(async file =>
            {
                var fileDir = Path.GetDirectoryName(file.FilePath) ?? "";
                var obj = await file.GetObjectAsync();
                var outPath =
                    string.IsNullOrEmpty(_outputDirectory) ?
                    Path.ChangeExtension(file.FilePath, ".dae") :
                    Path.Join(_outputDirectory, Path.GetFileNameWithoutExtension(file.FilePath) + ".dae");
                await Task.Run(() =>
                {
                    using var ow = new StreamWriter(outPath);
                    obj.ExportDae(ow);
                    var palData = SearchDirectories.ReadFile(obj.Palette, fileDir);
                    if (palData == null)
                        Warn($"Failed to find palette: {obj.Palette}");
                    var pal = palData is null ? Palette.Default : new Palette(palData);
                    foreach (var texName in obj.Textures)
                    {
                        if (SearchDirectories.ReadFile(texName, fileDir) is byte[] bmData)
                        {
                            var bm = Bitmap.FromData(texName, bmData);
                            bm.SavePngInDir(Path.GetDirectoryName(outPath) ?? _outputDirectory, pal);
                        }
                        else
                        {
                            Warn($"Failed to find texture: {texName}");
                        }
                    }
                });
                p += dp;
                progress?.Invoke(p);
            }).ToArray();
            await Task.WhenAll(fileTasks);
        }

        public void AddFilePaths(string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                AddFilePath(filePath);
            }
        }

        private void AddFilePath(string filePath)
        {
            if (Path.GetExtension(filePath).ToLowerInvariant() == ".3do")
            {
                AddFiles(new[] { filePath });
            }
            else if (Path.GetExtension(filePath).ToLowerInvariant() == ".gob")
            {
                SearchDirectories.GobPaths.Add(filePath);
                OnPropertyChanged(nameof(UserFileReferences));
            }
            else if (Directory.Exists(filePath))
            {
                SearchDirectories.Directories.Add(filePath);
                OnPropertyChanged(nameof(UserFileReferences));
            }
        }
    }

    public class BatchConversionFile : ObservableObject
    {
        public string FilePath { get; }

        readonly Task<ThreeDObject> objTask;
        public Task<ThreeDObject> GetObjectAsync() => objTask;

        public BatchConversionFile(string filePath)
        {
            FilePath = filePath;
            objTask = Task.Run(() => ThreeDObject.LoadFromFile(filePath));
        }
    }
}

