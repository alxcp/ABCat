using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace ABCat.Shared
{
    public sealed class LoginInfo : ICloneable, INotifyPropertyChanged
    {
        private readonly string _cryptPassword = Extensions.GenerateRandomString(16, 300);

        private string _login;
        private string _password;

        public bool IsCancelled { get; set; }

        public string Login
        {
            get => _login;
            set
            {
                if (value == _login) return;
                _login = value;
                OnPropertyChanged();
            }
        }

        public object Clone()
        {
            var clone = new LoginInfo();
            clone.Login = Login;
            clone.SetPassword(GetPassword());
            return clone;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool Check()
        {
            return !string.IsNullOrEmpty(Login) && !string.IsNullOrEmpty(_password);
        }

        public string GetPassword()
        {
            return string.IsNullOrEmpty(_password) ? null : _password.Decrypt(_cryptPassword);
        }

        public void SetPassword(string password)
        {
            _password = string.IsNullOrEmpty(password) ? null : password.Encrypt(_cryptPassword);
            OnPropertyChanged("Password");
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}