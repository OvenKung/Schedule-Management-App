using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text;              // สำหรับ StringContent
using System.Linq;              // FirstOrDefault
using Schedule_Management.Models;

namespace Schedule_Management.Views;

public partial class UsersListPage : ContentPage
{
    public ObservableCollection<UserInfo> Users { get; } = new();

    public UsersListPage()
    {
        InitializeComponent();
        BindingContext = this;
        _ = LoadUsersAsync();          // fire-and-forget
    }

    /* ────────────────────────────────────────────────────────────
       โหลดผู้ใช้ — แสดงเฉพาะ status = "active"
       ──────────────────────────────────────────────────────────── */
    private async Task LoadUsersAsync()
    {
        try
        {
            using var http = new HttpClient();
            var json = await http.GetStringAsync("https://basicapilogin.onrender.com/api/users");

            var root = JsonDocument.Parse(json).RootElement;
            JsonElement arr =
                root.ValueKind == JsonValueKind.Array
                    ? root
                    : root.GetProperty("users");

            Users.Clear();

            foreach (var el in arr.EnumerateArray())
            {
                var status = el.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "active";

                // ⭐ แสดงเฉพาะ active
                if (!status.Equals("active", StringComparison.OrdinalIgnoreCase))
                    continue;

                Users.Add(new UserInfo
                {
                    Email    = el.GetProperty("email").GetString()    ?? "",
                    Fullname = el.GetProperty("fullname").GetString() ?? "",
                    Role     = el.GetProperty("role").GetString()     ?? "",
                    ImageUrl = el.TryGetProperty("image_url", out var img) ? img.GetString() ?? "" : "",
                    Status   = status        // *ถ้าในโมเดลมีฟิลด์นี้*
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /* ────────────────────────────────────────────────────────────
       EDIT  → ไปหน้า EditUserPage พร้อม email ใน query
       ──────────────────────────────────────────────────────────── */
    private async void OnEditUser(object sender, EventArgs e)
    {
        if (sender is SwipeItem item && item.CommandParameter is string email)
        {
            await Shell.Current.GoToAsync($"EditUserPage?email={Uri.EscapeDataString(email)}");
        }
    }

    /* ────────────────────────────────────────────────────────────
       DELETE  → เปลี่ยน status เป็น "disable" (Soft-Delete)
       ──────────────────────────────────────────────────────────── */
    private async void OnDeleteUser(object sender, EventArgs e)
    {
        if (sender is SwipeItem item && item.CommandParameter is string email)
        {
            var user = Users.FirstOrDefault(u => u.Email == email);
            if (user == null) return;

            bool confirm = await DisplayAlert("Disable user",
                                              $"Disable {user.Fullname} ?",
                                              "Yes", "No");
            if (!confirm) return;

            try
            {
                using var http = new HttpClient();
                
                var payload = JsonSerializer.Serialize(new
                {
                    email,
                    status = "disable"      // เปลี่ยนสถานะ
                });
                var res = await http.PostAsync("https://basicapilogin.onrender.com/api/update-user",
                                               new StringContent(payload, Encoding.UTF8, "application/json"));

                if (res.IsSuccessStatusCode)
                {
                    //Users.Remove(user);     // เอาออกจากลิสต์ทันที
                    await DisplayAlert("Done", $"{user.Fullname} disabled", "OK");
                }
                else
                {
                    await DisplayAlert("Error", $"API replied {res.StatusCode}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}