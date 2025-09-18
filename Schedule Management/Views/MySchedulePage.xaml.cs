using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;
using Schedule_Management.Models;

namespace Schedule_Management.Views;

public partial class MySchedulePage : ContentPage
{
    public ObservableCollection<ScheduleCard> ScheduleCards { get; } = new();

    public MySchedulePage()
    {
        InitializeComponent();
        BindingContext = this;
        _ = LoadScheduleFromApi();
    }

    /* ─────────────────────  load schedules  ───────────────────── */
    private async Task LoadScheduleFromApi()
    {
        var email = Preferences.Get("email", "");
        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlert("Error", "E-mail not found", "OK");
            return;
        }

        var payload = JsonSerializer.Serialize(new { email });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        try
        {
            using var http = new HttpClient();
            var res  = await http.PostAsync("https://basicapilogin.onrender.com/api/schedule", content);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                await DisplayAlert("Error", $"API error: {res.StatusCode}", "OK");
                return;
            }

            var root = JsonDocument.Parse(json).RootElement;
            if (!root.TryGetProperty("schedules", out var arr) || arr.ValueKind != JsonValueKind.Array)
            {
                await DisplayAlert("Error", "Invalid data", "OK");
                return;
            }

            ScheduleCards.Clear();
            foreach (var el in arr.EnumerateArray())
            {
                /* show only from today onwards */
                if (DateTime.TryParse(el.GetProperty("date").GetString(), out var d) &&
                    d.Date < DateTime.Today) continue;

                var status = el.GetProperty("status").GetString() ?? "";
                if (status is not ("work" or "leave" or "pending")) continue;

                ScheduleCards.Add(ParseSchedule(el));
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /* ─────────────────────  helpers  ───────────────────── */
    private static async Task<bool> PostAsync(string ep, object payload)
    {
        try
        {
            using var http = new HttpClient();
            var res = await http.PostAsync(
                $"https://basicapilogin.onrender.com/api/{ep}",
                new StringContent(JsonSerializer.Serialize(payload),
                                  Encoding.UTF8, "application/json"));
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private ScheduleCard ParseSchedule(JsonElement el)
    {
        var card = new ScheduleCard
        {
            DateText = DateTime.Parse(el.GetProperty("date").GetString() ?? "")
                                 .ToString("dd MMM yyyy"),
            TimeRange = el.GetProperty("timeRange").GetString() ?? "",
            Status    = el.GetProperty("status").GetString() ?? "work",
            Reason    = el.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : ""
        };

        /* -------- Check-In -------- */
        card.CheckInCommand = new Command(async () =>
        {
            var email = Preferences.Get("email", "");
            var date  = DateTime.Parse(card.DateText).ToString("yyyy-MM-dd");
            var data  = $"CHECKIN|{email}|{date}|{DateTime.Now:HH:mm:ss}";
            await Shell.Current.GoToAsync($"CheckInQrPage?data={Uri.EscapeDataString(data)}");
        });

        /* -------- Request leave -------- */
        card.RequestLeaveCommand = new Command(async () =>
        {
            var reason = await DisplayPromptAsync("Request Leave",
                                                  "Enter reason:",
                                                  "Submit", "Cancel",
                                                  "Reason", -1, Keyboard.Text);

            if (string.IsNullOrWhiteSpace(reason)) return;

            card.Status = "pending";           // optimistic
            card.Reason = reason;

            var ok = await PostAsync("request-leave", new
            {
                email  = Preferences.Get("email", ""),
                date   = DateTime.Parse(card.DateText).ToString("yyyy-MM-dd"),
                reason = reason
            });

            if (!ok)
            {
                card.Status = "work";
                card.Reason = "";
                await DisplayAlert("Error", "Request failed.", "OK");
            }
        });

        /* -------- Cancel leave -------- */
        card.CancelLeaveCommand = new Command(async () =>
        {
            var prevReason = card.Reason;
            card.Status = "work";
            card.Reason = "";                  // hide immediately

            var ok = await PostAsync("cancel-leave", new
            {
                email = Preferences.Get("email", ""),
                date  = DateTime.Parse(card.DateText).ToString("yyyy-MM-dd")
            });

            if (!ok)
            {
                card.Status = "pending";
                card.Reason = prevReason;
                await DisplayAlert("Error", "Cancel failed.", "OK");
            }
        });

        return card;
    }
}