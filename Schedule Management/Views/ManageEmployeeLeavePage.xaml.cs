using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Schedule_Management.Models;

namespace Schedule_Management.Views;

public partial class ManageEmployeeLeavePage : ContentPage
{
    public ObservableCollection<ScheduleCard> PendingLeaves { get; } = new();

    public ManageEmployeeLeavePage()
    {
        InitializeComponent();
        BindingContext = this;
        _ = LoadPendingLeaveRequests();
    }

    // ------------------------- load -------------------------
    private async Task LoadPendingLeaveRequests()
    {
        // ขอทุก schedule โดยส่ง email="*"
        var payload = new StringContent("{\"email\":\"*\"}", Encoding.UTF8, "application/json");

        try
        {
            using var http = new HttpClient();
            var res  = await http.PostAsync("https://basicapilogin.onrender.com/api/schedule", payload);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                await DisplayAlert("Error", $"API error ({res.StatusCode})", "OK");
                return;
            }

            var root = JsonDocument.Parse(json).RootElement;
            if (!root.TryGetProperty("schedules", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return;

            PendingLeaves.Clear();

            foreach (var el in arr.EnumerateArray())
            {
                if (el.GetProperty("status").GetString() != "pending")
                    continue;

                var card = new ScheduleCard
                {
                    Email     = el.GetProperty("email").GetString() ?? "",
                    DateText  = DateTime.Parse(el.GetProperty("date").GetString() ?? "")
                                       .ToString("dd MMM yyyy"),
                    TimeRange = el.GetProperty("timeRange").GetString() ?? "",
                    Status    = "pending",
                    Reason    = el.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : ""
                };

                // bind commands
                card.ApproveCommand = new Command(async () => await ChangeStatus(card, "leave"));
                card.RejectCommand  = new Command(async () => await ChangeStatus(card, "work"));

                PendingLeaves.Add(card);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    // ------------------------- approve / reject -------------------------
    private async Task ChangeStatus(ScheduleCard card, string newStatus)
    {
        var body = JsonSerializer.Serialize(new
        {
            email  = card.Email,
            date   = DateTime.Parse(card.DateText).ToString("yyyy-MM-dd"),
            status = newStatus
        });

        try
        {
            using var http = new HttpClient();
            var res = await http.PostAsync(
                "https://basicapilogin.onrender.com/api/update-leave-status",
                new StringContent(body, Encoding.UTF8, "application/json"));

            if (res.IsSuccessStatusCode)
            {
                PendingLeaves.Remove(card);   // เอาออกจาก list ทันที
                await DisplayAlert("Done", $"Request {newStatus.ToUpper()} for {card.Email}", "OK");
            }
            else
            {
                var msg = await res.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"Server: {msg}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}