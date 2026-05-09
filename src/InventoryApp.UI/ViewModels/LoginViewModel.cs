using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace InventoryApp.UI.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly Action _onLoginSuccess;

    public LoginViewModel(Action onLoginSuccess)
    {
        _onLoginSuccess = onLoginSuccess;
    }

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _message = "Đăng nhập để vào hệ thống.";

    [RelayCommand]
    private void Login()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            Message = "Vui lòng nhập đầy đủ tài khoản và mật khẩu.";
            return;
        }

        Message = "Đăng nhập thành công (bản thô).";
        _onLoginSuccess();
    }
}
