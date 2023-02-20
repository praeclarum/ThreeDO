namespace ThreeDO;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
	}

    void OnLoadClicked(object sender, EventArgs e)
    {
    }

    void OnExportGltfClicked(object sender, EventArgs e)
	{
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

