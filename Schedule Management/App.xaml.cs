namespace Schedule_Management;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Ensure AppShell is set as the MainPage
        MainPage = new AppShell();
    }
}