using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Schedule_Management.Models;

namespace Schedule_Management.Views
{
    public partial class ManageScheduleEmployeePage : ContentPage
    {
        public ObservableCollection<ScheduleCard> EmployeeSchedules { get; } = new();

        private bool _isEditing = false;
        private ScheduleCard? _editingCard;

        public ManageScheduleEmployeePage()
        {
            InitializeComponent();
            BindingContext = this;

            // build HH:mm list (step 30 min)
            TimeStartPicker.ItemsSource = TimeEndPicker.ItemsSource = GenerateTimeOptions();
        }

        /* ---------- helpers ---------- */

        private static List<string> GenerateTimeOptions()
        {
            var list = new List<string>();
            for (int h = 0; h < 24; h++)
                for (int m = 0; m < 60; m += 30)
                    list.Add($"{h:D2}:{m:D2}");
            return list;
        }

        /* ---------- search ---------- */

        private async void OnSearchSchedulesClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EmployeeEmailEntry.Text))
            {
                await DisplayAlert("Missing", "Enter employee e‑mail first", "OK");
                return;
            }

            try
            {
                EmployeeSchedules.Clear();

                var body = JsonSerializer.Serialize(new { email = EmployeeEmailEntry.Text.Trim() });
                var res  = await new HttpClient()
                           .PostAsync("https://basicapilogin.onrender.com/api/schedule",
                                      new StringContent(body, Encoding.UTF8, "application/json"));

                if (!res.IsSuccessStatusCode)
                {
                    await DisplayAlert("Error", $"API responded {res.StatusCode}", "OK");
                    return;
                }

                var root = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
                if (!root.TryGetProperty("schedules", out var arr)) return;

                foreach (var s in arr.EnumerateArray())
                {
                    EmployeeSchedules.Add(new ScheduleCard
                    {
                        DateText  = DateTime.Parse(s.GetProperty("date").GetString() ?? "")
                                            .ToString("dd MMM yyyy"),
                        TimeRange = s.GetProperty("timeRange").GetString() ?? "",
                        Status    = s.GetProperty("status").GetString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        /* ---------- add / update ---------- */

        private async void OnAddOrUpdateClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EmployeeEmailEntry.Text) ||
                TimeStartPicker.SelectedItem is null ||
                TimeEndPicker.SelectedItem   is null)
            {
                await DisplayAlert("Missing", "Fill all fields", "OK");
                return;
            }

            var payload = new
            {
                email      = EmployeeEmailEntry.Text.Trim(),
                date       = ScheduleDatePicker.Date.ToString("yyyy-MM-dd"),
                timeRange  = $"{TimeStartPicker.SelectedItem} - {TimeEndPicker.SelectedItem}",
                status     = "work",

                // เมื่อเป็นการแก้ไขส่งค่าก่อนแก้ไปด้วย
                oldDate      = _editingCard is null ? null
                              : DateTime.Parse(_editingCard.DateText)
                                        .ToString("yyyy-MM-dd"),
                oldTimeRange = _editingCard?.TimeRange
            };

            var client = new HttpClient();
            var res    = await client.PostAsync(
                            _isEditing
                                ? "https://basicapilogin.onrender.com/api/update-schedule"
                                : "https://basicapilogin.onrender.com/api/add-schedule",
                            new StringContent(JsonSerializer.Serialize(payload),
                                              Encoding.UTF8, "application/json"));

            if (res.IsSuccessStatusCode)
            {
                await DisplayAlert("Success",
                                   _isEditing ? "Schedule updated" : "Schedule added",
                                   "OK");

                // reset edit state & refresh list
                ClearForm();
                await Task.Delay(100);   // give server a moment
                OnSearchSchedulesClicked(sender, e);
            }
            else
            {
                var msg = await res.Content.ReadAsStringAsync();
                await DisplayAlert("Error", msg, "OK");
            }
        }

        /* ---------- swipe  Edit ---------- */

        private void OnEditSchedule(object sender, EventArgs e)
        {
            if (sender is SwipeItem { BindingContext: ScheduleCard card })
            {
                _isEditing  = true;
                _editingCard = card;

                // fill form
                EmployeeEmailEntry.Text     = EmployeeEmailEntry.Text?.Trim(); // keep same e‑mail
                ScheduleDatePicker.Date     = DateTime.Parse(card.DateText);
                var parts                   = card.TimeRange.Split(" - ");
                TimeStartPicker.SelectedItem = parts[0];
                TimeEndPicker.SelectedItem   = parts.Length > 1 ? parts[1] : null;

                AddOrUpdateButton.Text = "UPDATE SCHEDULE";
            }
        }

        /* ---------- util ---------- */
        private void ClearForm()
        {
            _isEditing      = false;
            _editingCard    = null;
            AddOrUpdateButton.Text = "ADD SCHEDULE";
        }
    }
}