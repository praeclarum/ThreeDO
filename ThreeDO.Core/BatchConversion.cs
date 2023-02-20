using System.Collections.ObjectModel;

using MvvmHelpers;

namespace ThreeDO
{
    public class BatchConversion : ObservableObject
    {
        public ObservableCollection<BatchConversionFile> Files { get; } = new();

        private string _outputDirectory = "";
        public string OutputDirectory { get => _outputDirectory; set => SetProperty(ref _outputDirectory, value); }

        public void AddFiles(string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                Files.Add(new BatchConversionFile(filePath));
            }
        }

        public async Task ExportDaeFilesAsync(Action<double>? progress)
        {
            if (Files.Count < 1)
                return;
            var p = 0.0;
            var dp = 1.0 / Files.Count;
            var fileTasks = Files.Select(async file =>
            {
                var obj = await file.GetObjectAsync();
                var outPath = Path.Join(_outputDirectory, Path.GetFileNameWithoutExtension(file.FilePath) + ".dae");
                await Task.Run(() =>
                {
                    using var ow = new StreamWriter(outPath);
                    obj.ExportDae(ow);
                });
                p += dp;
                progress?.Invoke(p);
            }).ToArray();
            await Task.WhenAll(fileTasks);
        }
    }

    public class BatchConversionFile : ObservableObject
    {
        public string FilePath { get; }

        readonly Task<ThreeDObject> objTask;
        public Task<ThreeDObject> GetObjectAsync() => objTask;
        public ThreeDObject Object => objTask.Result;

        public BatchConversionFile(string filePath)
        {
            FilePath = filePath;
            objTask = Task.Run(() => ThreeDObject.LoadFromFile(filePath));
        }
    }
}

