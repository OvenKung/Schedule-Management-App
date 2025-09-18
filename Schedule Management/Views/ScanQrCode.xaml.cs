using ZXing.Net.Maui;
using System.Text.Json;

namespace Schedule_Management.Views;

public partial class ScanQrPage : ContentPage
{
    private bool _handled = false;

    public ScanQrPage()
    {
        InitializeComponent();
    }

    private async void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (_handled || e.Results.Count() == 0)
            return;

        _handled = true;

        try
        {
            var raw = e.Results[0].Value;
            await MainThread.InvokeOnMainThreadAsync(() => ResultLabel.Text = $"Scanned: {raw}");

            if (raw.StartsWith("CHECKIN|"))
            {
                var parts = raw.Split('|');
                if (parts.Length >= 4)
                {
                    var email   = parts[1].Trim();
                    var dateStr = parts[2].Trim();
                    var timeStr = parts[3].Trim();

                    // 1) ตรวจสอบรูปแบบ date + time
                    if (!DateTime.TryParse($"{dateStr} {timeStr}", out DateTime checkInTime))
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                            DisplayAlert("Error", "รูปแบบเวลาไม่ถูกต้องใน QR", "OK"));
                        return;
                    }

                    //----------------------------------------------------------
                    // 2) ดึงตารางเวลาทำงานของพนักงานในวันนั้นจาก API
                    //----------------------------------------------------------
                    using var client = new HttpClient();
                    var apiUrl = $"https://basicapilogin.onrender.com/api/schedule?email={email}&date={dateStr}";
                    var scheduleRes = await client.GetAsync(apiUrl);

                    if (!scheduleRes.IsSuccessStatusCode)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                            DisplayAlert("Error", "ไม่พบข้อมูลเวลาทำงาน", "OK"));
                        return;
                    }

                    //----------------------------------------------------------
                    // 3) แปลง JSON → ดึง startTime / endTime ให้ได้เสมอ (array หรือ object)
                    //----------------------------------------------------------
                    string scheduleJson = await scheduleRes.Content.ReadAsStringAsync();
                    JsonDocument doc    = JsonDocument.Parse(scheduleJson);

                    JsonElement schedEl;
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        schedEl = doc.RootElement[0];
                    }
                    else if (doc.RootElement.TryGetProperty("schedules", out var arrEl) && arrEl.GetArrayLength() > 0)
                    {
                        schedEl = arrEl[0];
                    }
                    else
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                            DisplayAlert("Error", "รูปแบบข้อมูลตารางไม่ถูกต้อง", "OK"));
                        return;
                    }

                    string startStr = schedEl.GetProperty("startTime").GetString() ?? "00:00";
                    string endStr   = schedEl.GetProperty("endTime").GetString()   ?? "23:59";

                    if (!TimeSpan.TryParse(startStr, out TimeSpan startTime) ||
                        !TimeSpan.TryParse(endStr,   out TimeSpan endTime))
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                            DisplayAlert("Error", "เวลาเริ่ม/สิ้นสุดงานไม่ถูกต้อง", "OK"));
                        return;
                    }

                    //----------------------------------------------------------
                    // 4) คำนวณสถานะ (on_time | late | absent)
                    //----------------------------------------------------------
                    var workDate       = DateTime.Parse(dateStr);
                    var startDateTime  = workDate.Date + startTime;
                    var endDateTime    = workDate.Date + endTime;

                    string status = checkInTime > endDateTime    ? "absent"
                                 : checkInTime > startDateTime   ? "late"
                                 : "on_time";

                    //----------------------------------------------------------
                    // 5) ส่งผลเช็กอินไปยัง API
                    //----------------------------------------------------------
                    var payload = new { email, date = dateStr, status };
                    var    json = JsonSerializer.Serialize(payload);
                    var    res  = await client.PostAsync(
                            "https://basicapilogin.onrender.com/api/checkin",
                            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

                    //----------------------------------------------------------
                    // 6) แจ้งผลลัพธ์
                    //----------------------------------------------------------
                    if (res.IsSuccessStatusCode)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                            DisplayAlert("Check-In Successful",
                                         $"{email}\nTime  : {checkInTime:HH:mm:ss}\nStatus: {status}",
                                         "OK"));
                    }
                    else if (res.StatusCode == System.Net.HttpStatusCode.Conflict)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                            DisplayAlert("Already Checked-In", "You have already checked in today.", "OK"));
                    }
                    else if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                            DisplayAlert("Not Found", "No schedule found for this date.", "OK"));
                    }
                    else
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                            DisplayAlert("Failed", $"API returned {res.StatusCode}", "OK"));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
                DisplayAlert("Exception", ex.Message, "OK"));
        }

        await Task.Delay(3000); // ปล่อยให้สแกนใหม่หลัง 3 วินาที
        _handled = false;
    }

    private void OnToggleFlashClicked(object sender, EventArgs e)
    {
        cameraView.IsTorchOn = !cameraView.IsTorchOn;
    }
}

// คลาสช่วยให้ deserialize JSON จาก schedule API
public class ScheduleModel
{
    public string email { get; set; }
    public string date { get; set; }
    public string status { get; set; }
    public string startTime { get; set; }
    public string endTime { get; set; }
}