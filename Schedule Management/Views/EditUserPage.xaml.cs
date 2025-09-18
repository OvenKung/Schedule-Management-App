using System.Text;
using System.Text.Json;
using Microsoft.Maui.Controls;

namespace Schedule_Management.Views;

[QueryProperty(nameof(Email), "email")]           // รับอีเมลผ่าน query string
public partial class EditUserPage : ContentPage
{
    public string Email { get; set; } = "";

    public EditUserPage() => InitializeComponent();

    // ───────────────────────────────────────── OnAppearing
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (string.IsNullOrWhiteSpace(Email))
            return;                                // ไม่มีอีเมล = กลับเลย

        try
        {
            using var http = new HttpClient();
            var url  = $"https://basicapilogin.onrender.com/api/users?email={Uri.EscapeDataString(Email)}";
            var json = await http.GetStringAsync(url);

            // ---- แปลงผลลัพธ์ให้เป็น List<JsonElement> เสมอ ----
            var root = JsonDocument.Parse(json).RootElement;
            IEnumerable<JsonElement> list = root.ValueKind switch
            {
                JsonValueKind.Array                        => root.EnumerateArray(),
                _ when root.TryGetProperty("users", out var arr)   => arr.EnumerateArray(),
                _ when root.TryGetProperty("user",  out var one)   => new[] { one },
                _                                                => new[] { root }
            };

            // ---- หา record ที่ email ตรง (ไม่สนตัวพิมพ์) ----
            var userEl = list.FirstOrDefault(e =>
                e.TryGetProperty("email", out var em) &&
                string.Equals(em.GetString(), Email, StringComparison.OrdinalIgnoreCase));

            if (userEl.ValueKind == JsonValueKind.Undefined)
            {
                await DisplayAlert("Not found", $"Cannot find user {Email}", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            // helper
            static string GetSafe(JsonElement el, string name1, string? name2 = null)
                => el.TryGetProperty(name1, out var p) ? p.GetString() ?? ""
                   : name2 is not null && el.TryGetProperty(name2, out var p2) ? p2.GetString() ?? ""
                   : "";

            // ---- เติม UI ----
            EmailEntry.Text    = GetSafe(userEl, "email");
            FullnameEntry.Text = GetSafe(userEl, "fullname");
            ImageUrlEntry.Text = GetSafe(userEl, "image_url", "imageUrl");

            var role = GetSafe(userEl, "role");
            RolePicker.SelectedItem = RolePicker.ItemsSource?.Contains(role) == true ? role
                                  : RolePicker.ItemsSource?[0];
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    // ───────────────────────────────────────── SAVE
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        if (PasswordEntry.Text != ConfirmPasswordEntry.Text)
        {
            ErrorLabel.Text = "Password and confirmation do not match.";
            ErrorLabel.IsVisible = true;
            return;
        }

        var payload = new
        {
            email    = EmailEntry.Text?.Trim(),
            fullname = FullnameEntry.Text?.Trim(),
            role     = RolePicker.SelectedItem?.ToString(),
            imageUrl = ImageUrlEntry.Text?.Trim(),
            password = string.IsNullOrWhiteSpace(PasswordEntry.Text) ? null : PasswordEntry.Text
        };

        try
        {
            using var http = new HttpClient();
            var content = new StringContent(JsonSerializer.Serialize(payload),
                                            Encoding.UTF8, "application/json");

            var res = await http.PostAsync("https://basicapilogin.onrender.com/api/update-user", content);

            if (res.IsSuccessStatusCode)
            {
                await DisplayAlert("Success", "User updated.", "OK");
                await Shell.Current.GoToAsync("..");   // Back to UsersListPage
            }
            else
            {
                var msg = await res.Content.ReadAsStringAsync();
                ErrorLabel.Text = $"API error: {res.StatusCode}\n{msg}";
                ErrorLabel.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
        }
    }
}