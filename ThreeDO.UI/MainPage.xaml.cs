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
	}

    void OnLoadClicked(object sender, EventArgs e)
    {
    }

    async Task OnExportGltfClicked(object sender, EventArgs e)
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

