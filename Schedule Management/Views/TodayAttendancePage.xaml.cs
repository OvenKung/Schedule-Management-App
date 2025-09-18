using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using Schedule_Management.Models;

namespace Schedule_Management.Views;

/// <summary>
/// “วันนี้ใครมาทำงาน?”
/// แสดงรายการเข้า‑งานกลุ่มตาม Status และนับจำนวนในกลุ่มให้พร้อมใช้ที่ XAML
/// </summary>
public partial class TodayAttendancePage : ContentPage
{
    // ─────────────────────────────────────────────────────────────
    // UI‑binding : คอลเลกชันของกลุ่ม (Key, CountWithLabel, Items…)
    // ─────────────────────────────────────────────────────────────
    public ObservableCollection<AttendanceGroup> GroupedRecords { get; } = new();

    public TodayAttendancePage()
    {
        InitializeComponent();
        BindingContext = this;

        _ = LoadTodayAttendanceAsync();
    }

    private async Task LoadTodayAttendanceAsync()
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        try
        {
            using var http = new HttpClient();
            var url  = $"https://basicapilogin.onrender.com/api/schedule?date={today}&email=*";
            var json = await http.GetStringAsync(url);

            var root = JsonDocument.Parse(json).RootElement;

            // 1) รองรับทั้ง {schedules:[…]}  และ array ตรง ๆ
            JsonElement arr =
                root.ValueKind == JsonValueKind.Array
                    ? root
                    : root.GetProperty("schedules");

            // 2) map → AttendanceRecord
            var list = new List<AttendanceRecord>();

            foreach (var el in arr.EnumerateArray())
            {
                string timeRange = el.TryGetProperty("timeRange", out var trElt)
                                     ? trElt.GetString() ?? ""
                                     : $"{el.GetProperty("startTime").GetString()} - {el.GetProperty("endTime").GetString()}";

                list.Add(new AttendanceRecord
                {
                    Email     = el.GetProperty("email").GetString()  ?? "",
                    Status    = el.GetProperty("status").GetString() ?? "",
                    TimeRange = timeRange,
                    ImageUrl  = el.TryGetProperty("imageUrl", out var img) ? img.GetString() ?? "" : ""
                });
            }

            // 3) Group + เติมลง ObservableCollection
            var grouped = list
                .GroupBy(r => r.Status switch
                {
                    "on_time" => "✓ On time",
                    "late"    => "⏰ Late",
                    "absent"  => "✖ Absent",
                    "pending" => "⌛ Pending",
                    "leave"   => "🏖 Leave",
                    _         => "Other"
                })
                .OrderBy(g => g.Key);

            GroupedRecords.Clear();
            foreach (var g in grouped)
                GroupedRecords.Add(new AttendanceGroup(g.Key, g));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}

/// <summary>
/// กลุ่มที่ CollectionView ใช้ bind (แถมหัวข้อ + จำนวนสำเร็จรูป)
/// </summary>
public class AttendanceGroup : ObservableCollection<AttendanceRecord>
{
    public string Key { get; }
    public string CountWithLabel => Count.ToString();   // ใช้โชว์บน badge

    public AttendanceGroup(string key, IEnumerable<AttendanceRecord> items) : base(items)
        => Key = key;
}