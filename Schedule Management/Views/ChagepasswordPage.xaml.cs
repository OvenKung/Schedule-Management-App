using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Schedule_Management.Views
{
    public partial class ChagepasswordPage : ContentPage
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public ChagepasswordPage()
        {
            InitializeComponent();
        }

        private async void OnChangePasswordClicked(object sender, EventArgs e)
        {
            var oldPassword = OldPasswordEntry.Text;
            var newPassword = NewPasswordEntry.Text;
            var confirmPassword = ConfirmPasswordEntry.Text;
            var email = Preferences.Get("email", null);

            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageLabel.Text = "All fields are required.";
                MessageLabel.IsVisible = true;
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageLabel.Text = "New passwords do not match.";
                MessageLabel.IsVisible = true;
                return;
            }

            // ตรวจสอบเงื่อนไขรหัสผ่านตาม Guidelines
            string passwordValidationError = ValidatePassword(newPassword);
            if (!string.IsNullOrEmpty(passwordValidationError))
            {
                MessageLabel.Text = passwordValidationError;
                MessageLabel.IsVisible = true;
                return;
            }

            var payload = new
            {
                email = email,
                oldPassword = oldPassword,
                newPassword = newPassword
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("https://basicapilogin.onrender.com/api/change-password", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Success", "Password changed successfully.", "OK");
                    // เคลียร์ Error Message และ Entry หลังจากสำเร็จ
                    MessageLabel.IsVisible = false;
                    OldPasswordEntry.Text = string.Empty;
                    NewPasswordEntry.Text = string.Empty;
                    ConfirmPasswordEntry.Text = string.Empty;
                }
                else
                {
                    MessageLabel.Text = $"Failed to change password: {responseBody}";
                    MessageLabel.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                MessageLabel.Text = $"An error occurred: {ex.Message}";
                MessageLabel.IsVisible = true;
            }
        }

        // เมธอดสำหรับตรวจสอบความถูกต้องของรหัสผ่านตาม Guidelines
        private string ValidatePassword(string password)
        {
            // เช็ครหัสผ่านยาวอย่างน้อย 8 ตัวอักษร
            if (password.Length < 8)
            {
                return "Password must be at least 8 characters long.";
            }

            // เช็คตัวอักษรพิมพ์ใหญ่
            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                return "Password must include at least one uppercase letter.";
            }

            // เช็คตัวอักษรพิมพ์เล็ก
            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                return "Password must include at least one lowercase letter.";
            }

            // เช็คตัวเลข
            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                return "Password must include at least one number.";
            }

            // เช็คอักขระพิเศษ
            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            {
                return "Password must include at least one special character.";
            }

            // ผ่านการตรวจสอบทั้งหมด
            return string.Empty;
        }
    }
}