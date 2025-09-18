using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Schedule_Management.Views;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        string email = EmailEntry.Text;
        string password = PasswordEntry.Text;
        string confirmPassword = ConfirmPasswordEntry.Text;
        string fullname = FullnameEntry.Text;
        string imageUrl = ImageUrlEntry.Text;

        // Validate email format
        if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
        {
            await DisplayAlert("Input Error", "Please enter a valid email address.", "OK");
            return;
        }

        // Validate password and confirm password
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            await DisplayAlert("Input Error", "Please enter both password and confirm password.", "OK");
            return;
        }

        if (password != confirmPassword)
        {
            await DisplayAlert("Input Error", "Passwords do not match. Please try again.", "OK");
            return;
        }

        // ตรวจสอบความถูกต้องของรหัสผ่านตาม Guidelines
        string passwordValidationError = ValidatePassword(password);
        if (!string.IsNullOrEmpty(passwordValidationError))
        {
            await DisplayAlert("Password Error", passwordValidationError, "OK");
            return;
        }

        if (string.IsNullOrEmpty(fullname))
        {
            await DisplayAlert("Input Error", "Please enter your full name.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(imageUrl) || !Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
        {
            await DisplayAlert("Input Error", "Please enter a valid image URL.", "OK");
            return;
        }

        var registerData = new { email, password, fullname, imageUrl, role = "user",status = "active"};
        var json = JsonSerializer.Serialize(registerData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await new HttpClient().PostAsync("https://basicapilogin.onrender.com/api/register", content);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Success", "Registration successful! Redirecting to Login...", "OK");

                // Wait for 2 seconds and navigate back to Login
                await Task.Delay(2000);
                await Shell.Current.GoToAsync("//LoginPage");
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Registration Failed", $"Error: {errorResponse}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }

    private bool IsValidEmail(string email)
    {
        // Regular expression for validating email format
        var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, emailRegex);
    }

    // เพิ่มเมธอดตรวจสอบรหัสผ่านตาม Guidelines
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