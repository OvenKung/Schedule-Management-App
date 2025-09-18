using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace Schedule_Management.Views;

public partial class LoginPage : ContentPage
{
    private readonly HttpClient _httpClient = new();

    public LoginPage()
    {
        InitializeComponent();
    }

    // ───────────────────────── helpers ─────────────────────────
    private void ClearFields()
    {
        EmailEntry.Text    = string.Empty;
        PasswordEntry.Text = string.Empty;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ClearFields();
    }

    // ───────────────────────── LOGIN ─────────────────────────
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var email    = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Input Error", "Please enter both email and password.", "OK");
            return;
        }

        var payload = JsonSerializer.Serialize(new { email, password });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        try
        {
            var res  = await _httpClient.PostAsync("https://basicapilogin.onrender.com/api/login", content);
            var body = await res.Content.ReadAsStringAsync();

            Console.WriteLine($"[LOGIN] ⇢ {payload}");
            Console.WriteLine($"[LOGIN] ⇠ {body}");

            if (!res.IsSuccessStatusCode)
            {
                await DisplayAlert("Login Failed", "Invalid email or password.", "OK");
                return;
            }

            var root      = JsonDocument.Parse(body).RootElement;
            var role      = root.GetProperty("role").GetString()      ?? "";
            var fullname  = root.GetProperty("fullname").GetString()  ?? "";
            var imageUrl  = root.TryGetProperty("imageUrl", out var img) ? img.GetString() ?? "" : "";
            var statusStr = root.TryGetProperty("status",  out var st)  ? st.GetString()  ?? "" : "active";

            // ---------- CHECK STATUS ----------
            if (statusStr.Equals("disabled", StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert("Account Disabled",
                                   "Your account has been disabled. Please contact administrator.",
                                   "OK");
                return;                               // หยุดการเข้าสู่ระบบ
            }

            // ---------- SAVE PREFS ----------
            Preferences.Set("email",     email);
            Preferences.Set("fullname",  fullname);
            Preferences.Set("role",      role);
            Preferences.Set("imageUrl",  imageUrl);
            Preferences.Set("status",    statusStr);

            await DisplayAlert("Success", $"Welcome {fullname} ({role})!", "OK");
            await Shell.Current.GoToAsync("ProfilePage");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    // ───────────────────────── NAVIGATE TO REGISTER ─────────────────────────
    private async void OnRegisterButtonClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("RegisterPage");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Navigation Error", ex.Message, "OK");
        }
    }
}