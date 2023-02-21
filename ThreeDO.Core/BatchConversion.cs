using System.Collections.ObjectModel;

using MvvmHelpers;

using ThreeDO.Core;

namespace ThreeDO
{
    public class BatchConversion : ObservableObject
    {
        public ObservableCollection<BatchConversionFile> Files { get; } = new();

        public SearchDirectories SearchDirectories { get; } = new();

        public BatchConversion()
        {
            Files.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CanExport));
            };
            SearchDirectories.GobPaths.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CanExport));
            };
        }

        bool isExporting;
        public bool IsExporting
        {
            get => isExporting; set
            {
                SetProperty(ref isExporting, value);
                OnPropertyChanged(nameof(CanExport));
            }
        }
        public bool CanExport => !IsExporting && Files.Count > 0;

        double exportProgress;
        public double ExportProgress { get => exportProgress; set => SetProperty(ref exportProgress, value); }

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
        public IEnumerable<string> ObjectFileReferences
        {
            get
            {
                foreach (var f in Files)
                    yield return f.FilePath;
                foreach (var gp in SearchDirectories.GobPaths)
                {
                    Gob g;
                    try
                    {
                        g = Gob.GetGob(gp);
                    }
                    catch
                    {
                        g = new Gob();
                    }
                    foreach (var (e, d) in g.Entries)
                    {
                        if (e.EndsWith(".3do"))
                            yield return e;
                    }
                }
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
            ExportProgress = 0;
            IsExporting = true;
            var p = 0.0;
            var dp = 1.0 / Files.Count;
            var fileTasks = Files.Select(async file =>
            {
                var fileDir = Path.GetDirectoryName(file.FilePath) ?? "";
                var obj = file.Object;
                var outPath =
                    string.IsNullOrEmpty(_outputDirectory) ?
                    Path.ChangeExtension(file.FilePath, ".dae") :
                    Path.Join(_outputDirectory, Path.GetFileNameWithoutExtension(file.FilePath) + ".dae");
                await Task.Run(() =>
                {
                    file.ExportStatus = ExportStatus.Exporting;
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
                    file.ExportStatus = ExportStatus.Exported;
                });
                p += dp;
                ExportProgress = p;
                progress?.Invoke(p);
            }).ToArray();
            await Task.WhenAll(fileTasks);
            IsExporting = false;
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
        public string FileName => Path.GetFileName(FilePath);

        ExportStatus exportStatus = ExportStatus.Queued;
        private object? _thumbnailDrawable;

        public ExportStatus ExportStatus { get => exportStatus; set => SetProperty(ref exportStatus, value); }

        readonly Lazy<ThreeDObject> obj;
        public ThreeDObject Object => obj.Value;

        public object? ThumbnailDrawable { get => _thumbnailDrawable; set => SetProperty(ref _thumbnailDrawable, value); }

        public BatchConversionFile(string filePath)
        {
            FilePath = filePath;
            obj = new Lazy<ThreeDObject>(() =>
            {
                try
                {
                    return ThreeDObject.LoadFromFile(filePath);
                }
                catch
                {
                    return new ThreeDObject();
                }
            });
        }
    }

    public enum ExportStatus {
        Queued,
        Exporting,
        Exported,
    }
}

