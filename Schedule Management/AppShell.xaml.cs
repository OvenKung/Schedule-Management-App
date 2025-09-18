namespace Schedule_Management;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("RegisterPage", typeof(Views.RegisterPage));
		Routing.RegisterRoute("ProfilePage", typeof(Views.ProfilePage));
		Routing.RegisterRoute("MySchedulePage", typeof(Views.MySchedulePage));
		Routing.RegisterRoute("ChangePasswordPage", typeof(Views.ChagepasswordPage));
		Routing.RegisterRoute("ManageScheduleEmployeePage", typeof(Views.ManageScheduleEmployeePage));
		Routing.RegisterRoute("ManageEmployeeLeavePage", typeof(Views.ManageEmployeeLeavePage));
		Routing.RegisterRoute("CheckInQrPage", typeof(Views.CheckInQrPage));
		Routing.RegisterRoute("ScanQrPage", typeof(Views.ScanQrPage));
		Routing.RegisterRoute("TodayAttendancePage", typeof(Views.TodayAttendancePage));
		Routing.RegisterRoute("UsersListPage", typeof(Views.UsersListPage));
		Routing.RegisterRoute("EditUserPage", typeof(Views.EditUserPage));
	}
}
