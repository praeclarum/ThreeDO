using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Maui.ApplicationModel;

namespace ThreeDO;

public partial class MainPage : ContentPage
{
	int count = 0;
	readonly BatchConversion batch = new ();

    public MainPage()
	{
		BindingContext = batch;
		InitializeComponent();
		try
		{
			var initFiles = JsonSerializer.Deserialize<string[]>(Preferences.Get("UserFileReferences", "[]"));
			batch.AddFilePaths(initFiles);
        }
		catch
		{
		}
	}

	void SaveSettings()
	{
		Preferences.Set("UserFileReferences", JsonSerializer.Serialize(batch.UserFileReferences));
    }

    async void OnLoadClicked(object sender, EventArgs e)
    {
		var files = await FilePicker.PickMultipleAsync(new PickOptions
		{
			FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
			{
				{ DevicePlatform.MacCatalyst, new [] { "public.folder", "com.lucasarts.3do", "com.lucasarts.gob" } },
				{ DevicePlatform.iOS, new [] { "public.folder", "com.lucasarts.3do", "com.lucasarts.gob" } },
			})
		});
		batch.AddFilePaths(files.Select(x => x.FullPath).ToArray());
		SaveSettings();
    }

    async void OnExportGltfClicked(object sender, EventArgs e)
	{
		await batch.ExportDaeFilesAsync(progress =>
		{
		});
	}

    void OnExportDaeClicked(object sender, EventArgs e)
    {
    }

    private void OnCounterClicked(object sender, EventArgs e)
	{
		count++;

		if (count == 1)
			CounterBtn.Text = $"Clicked {count} time";
		else
			CounterBtn.Text = $"Clicked {count} times";

		SemanticScreenReader.Announce(CounterBtn.Text);
	}
}

