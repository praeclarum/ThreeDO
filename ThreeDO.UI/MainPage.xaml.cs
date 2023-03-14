using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;

using MvvmHelpers;

namespace ThreeDO;

public partial class MainPage : ContentPage
{
	readonly BatchConversion batch = new ();

    public MainPage()
	{
		BindingContext = batch;
		InitializeComponent();
		try
		{
			var initFiles = JsonSerializer.Deserialize<string[]>(Preferences.Get("UserFileReferences", "[]"));
			batch.AddFilePaths(initFiles);
            SetThumbnails();
        }
		catch
		{
		}
	}

	void SaveSettings()
	{
		Preferences.Set("UserFileReferences", JsonSerializer.Serialize(batch.UserFileReferences));
		SetThumbnails();
    }

    async void OnLoadClicked(object sender, EventArgs e)
    {
		var files = await FilePicker.PickMultipleAsync(new PickOptions
		{
			FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
			{
				{ DevicePlatform.MacCatalyst, new [] { "3do", "gob" } },
                { DevicePlatform.WinUI, new [] { ".3do", ".gob" } },
				//{ DevicePlatform.MacCatalyst, new [] { "public.folder", "com.lucasarts.3do", "com.lucasarts.gob" } },
				//{ DevicePlatform.iOS, new [] { "public.folder", "com.lucasarts.3do", "com.lucasarts.gob" } },
			})
		});
		batch.AddFilePaths(files.Select(x => x.FullPath).ToArray());
		SaveSettings();
    }

    void OnRemoveAllFilesClicked(object sender, EventArgs e)
    {
        batch.Files.Clear();
        SaveSettings();
    }

    void OnFileDeleteClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.BindingContext is BatchConversionFile file)
        {
            batch.Files.Remove(file);
            SaveSettings();
        }
    }

    void OnExportGltfClicked(object sender, EventArgs e)
	{
	}

    async void OnExportDaeClicked(object sender, EventArgs e)
    {
		await batch.ExportDaeFilesAsync(progress =>
		{
		});
    }

    async void OnAddDirectoryClicked(object sender, EventArgs e)
	{
        var files = await FilePicker.PickMultipleAsync(new PickOptions
        {
            
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.MacCatalyst, new [] { "public.folder" } },
                { DevicePlatform.WinUI, new [] { ".gob" } },
            })
        });
        batch.AddFilePaths(files.Select(x => x.FullPath).ToArray());
        SaveSettings();
    }

	void OnSearchDirDeleteClicked(object sender, EventArgs e)
	{
		if ((sender as Button)?.BindingContext is string dir)
		{
			batch.SearchDirectories.Directories.Remove(dir);
            SaveSettings();
        }
	}

	void SetThumbnails()
	{
		foreach (var f in batch.Files)
		{
			if (f.ThumbnailDrawable is object)
				continue;
			f.ThumbnailDrawable = new Thumbnail(f);
		}
	}

    class Thumbnail : ObservableObject, IDrawable
    {
		public readonly BatchConversionFile File;

        public Thumbnail(BatchConversionFile file)
        {
            File = file;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
			var obj = File.Object;
			var pixelSize = dirtyRect.Size;
			var objMin = obj.Min;
			var objMax = obj.Max;
			var objCenter = (objMin + objMax) / 2;
			var objSize = objMax - objMin;
			var scale = pixelSize.Width / objSize.X;
			canvas.Translate(pixelSize.Width/2, pixelSize.Height/2);
			canvas.Scale(scale, scale);
			canvas.Translate(-objCenter.X, -objCenter.Y);
			canvas.StrokeSize = 1.0f / scale;
			canvas.StrokeColor = Colors.Green;
			foreach (var o in obj.Objects)
			{
				void DrawLine(int i1, int i2)
				{
					var v1 = o.Vertices[i1];
					var v2 = o.Vertices[i2];
					canvas.DrawLine(v1.X, v1.Y, v2.X, v2.Y);
				}
				foreach (var p in o.Quads)
				{
					DrawLine(p.A, p.B);
					DrawLine(p.B, p.C);
					DrawLine(p.C, p.D);
					DrawLine(p.D, p.A);
				}
			}
        }
    }
}

