using Microsoft.Maui.Storage;

namespace Schedule_Management.Views;

public partial class ProfilePage : ContentPage
{
	public ProfilePage()
	{
		InitializeComponent();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		var fullname = Preferences.Get("fullname", "Unknown");
		var role = Preferences.Get("role", "Unknown");
		var imageUrl = Preferences.Get("imageUrl", string.Empty);
		if (!string.IsNullOrWhiteSpace(imageUrl) && Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
		{
			// show the real profile image
			AvatarImage.Source = ImageSource.FromUri(new Uri(imageUrl));
		}
		else
		{
			// fallback placeholder (ensure a placeholder exists in Resources/Images)
			AvatarImage.Source = "placeholder_profile.png";
		}
		FullNameLabel.Text = $"Welcome : {fullname}";
		RoleLabel.Text = $"Role: {role}";
		if (role == "admin")
		{
			AdminPanel.IsVisible = true;
		}
		else
		{
			AdminPanel.IsVisible = false;
		}

	}

	private async void OnMyScheduleClicked(object sender, EventArgs e)
	{
		try
		{
			await Shell.Current.GoToAsync("MySchedulePage");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Navigation error error: {ex.Message}");
		}
	}

	private async void OnChangePasswordClicked(object sender, EventArgs e)
	{
		// Navigate to a ChangePasswordPage (you need to create this page)
		await Shell.Current.GoToAsync("ChangePasswordPage");
	}

	private async void OnLogoutClicked(object sender, EventArgs e)
	{
		// Clear stored preferences
		Preferences.Remove("fullname");
		Preferences.Remove("role");

		// Navigate back to Login page
		await Shell.Current.GoToAsync("//LoginPage");
	}

	private async void OnManageEmployeeScheduleClicked(object sender, EventArgs e)
	{
		try
		{
			await Shell.Current.GoToAsync("ManageScheduleEmployeePage");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Navigation error: {ex.Message}");
		}
	}

	private async void OnManageEmployeeLeaveClicked(object sender, EventArgs e)
	{
		try
		{
			await Shell.Current.GoToAsync("ManageEmployeeLeavePage");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Navigation error: {ex.Message}");
		}
	}
	private async void OnCheckInEmployeeClicked(object sender, EventArgs e)
	{
		try
		{
			await Shell.Current.GoToAsync("ScanQrPage");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Navigation error: {ex.Message}");
		}
	}
	private async void OnTodayAttendanceClicked(object sender, EventArgs e)
	{
		try
		{
			await Shell.Current.GoToAsync("TodayAttendancePage");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Navigation error: {ex.Message}");
		}
	}
	private async void OnAllUsersClicked(object sender, EventArgs e)
	{
		try
		{
			await Shell.Current.GoToAsync("UsersListPage");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Navigation error: {ex.Message}");
		}
	}
}