using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Graphics;

namespace Schedule_Management.Models;

public class ScheduleCard : INotifyPropertyChanged
{
    private string _status = "work";   // work | pending | leave
    private Color _statusColor = Colors.Green;

    public string DateText { get; set; } = "";
    public string TimeRange { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Email { get; set; } = ""; // Added Email property here

    public ICommand? ApproveCommand { get; set; }
    public ICommand? RejectCommand { get; set; }

    public string Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            _status = value;
            StatusColor = value switch
            {
                "work"    => Colors.Green,
                "pending" => Colors.Gold,
                "leave"   => Colors.IndianRed,
                _         => Colors.LightGray
            };
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(IsWork));
            OnPropertyChanged(nameof(IsPending));
            OnPropertyChanged(nameof(IsLeave));
        }
    }

    public Color StatusColor
    {
        get => _statusColor;
        private set { _statusColor = value; OnPropertyChanged(); }
    }

    // Helper‑bool properties for binding in XAML
    public bool IsWork => Status == "work";
    public bool IsPending => Status == "pending";
    public bool IsLeave => Status == "leave";

    // ICommand properties to be set later from the page
    public ICommand? CheckInCommand { get; set; }
    public ICommand? RequestLeaveCommand { get; set; }
    public ICommand? CancelLeaveCommand { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}