﻿using System;
using System.Windows;
using System.Windows.Input;
using ABCat.Shared.Messages;
using ABCat.Shared.ViewModels;
using Caliburn.Micro;

namespace ABCat.UI.WPF.Models
{
    public sealed class StatusBarStateViewModel : ViewModelBase, IHandle<DBOperationMessage>
    {
        private bool _isOnDbOperation;
        private string _message;
        private int _progressBarSmallMaximum;
        private string _progressBarSmallMessage;
        private int _progressBarSmallMinimum;
        private int _progressBarSmallValue = -1;
        private int _progressBarTotalMaximum;
        private string _progressBarTotalMessage;
        private int _progressBarTotalMinimum;
        private int _progressBarTotalValue = -1;

        public StatusBarStateViewModel(Func<bool> isCanCancelAsyncOperation,
            Action cancelAsyncOperation)
        {
            IsCanCancelAsyncOperation = isCanCancelAsyncOperation;
            CancelAsyncOperation = cancelAsyncOperation;
            Context.I.EventAggregator.Subscribe(this);
        }

        public Action CancelAsyncOperation { get; set; }

        public ICommand CancelAsyncOperationCommand => CommandFactory.Get(CancelAsyncOperation,
            () => IsCanCancelAsyncOperation());

        public Visibility CancelAsyncOperationCommandVisibibility =>
            _progressBarSmallValue >= 0 || _progressBarTotalValue >= 0
                ? Visibility.Visible
                : Visibility.Collapsed;

        public Func<bool> IsCanCancelAsyncOperation { get; set; }

        public string Mesage
        {
            get => _message;
            set
            {
                if (value == _message) return;
                _message = value;
                OnPropertyChanged();
            }
        }

        public int ProgressBarSmallMaximum
        {
            get => _progressBarSmallMaximum;
            set
            {
                if (Equals(_progressBarSmallMaximum, value)) return;
                _progressBarSmallMaximum = value;
                OnPropertyChanged();
            }
        }

        public string ProgressBarSmallMessage
        {
            get => _progressBarSmallMessage;
            set
            {
                if (Equals(_progressBarSmallMessage, value)) return;
                _progressBarSmallMessage = value;
                OnPropertyChanged();
            }
        }

        public int ProgressBarSmallMinimum
        {
            get => _progressBarSmallMinimum;
            set
            {
                if (Equals(_progressBarSmallMinimum, value)) return;
                _progressBarSmallMinimum = value;
                OnPropertyChanged();
            }
        }

        public int ProgressBarSmallValue
        {
            get => _progressBarSmallValue;
            set
            {
                if (Equals(_progressBarSmallValue, value)) return;
                _progressBarSmallValue = value;
                OnPropertyChanged(nameof(ProgressBarSmallVisibility));
                OnPropertyChanged(nameof(CancelAsyncOperationCommandVisibibility));
                OnPropertyChanged();
            }
        }

        public Visibility ProgressBarSmallVisibility =>
            ProgressBarSmallValue >= 0 ? Visibility.Visible : Visibility.Collapsed;

        public int ProgressBarTotalMaximum
        {
            get => _progressBarTotalMaximum;
            set
            {
                if (Equals(_progressBarTotalMaximum, value)) return;
                _progressBarTotalMaximum = value;
                OnPropertyChanged();
            }
        }

        public string ProgressBarTotalMessage
        {
            get => _progressBarTotalMessage;
            set
            {
                if (Equals(_progressBarTotalMessage, value)) return;
                _progressBarTotalMessage = value;
                OnPropertyChanged();
            }
        }

        public int ProgressBarTotalMinimum
        {
            get => _progressBarTotalMinimum;
            set
            {
                if (Equals(_progressBarTotalMinimum, value)) return;
                _progressBarTotalMinimum = value;
                OnPropertyChanged();
            }
        }

        public int ProgressBarTotalValue
        {
            get => _progressBarTotalValue;
            set
            {
                if (Equals(_progressBarTotalValue, value)) return;
                _progressBarTotalValue = value;
                OnPropertyChanged(nameof(ProgressBarTotalVisibility));
                OnPropertyChanged(nameof(CancelAsyncOperationCommandVisibibility));
                OnPropertyChanged();
            }
        }

        public Visibility ProgressBarTotalVisibility =>
            ProgressBarTotalValue >= 0 ? Visibility.Visible : Visibility.Collapsed;

        public bool IsOnDbOperation
        {
            get => _isOnDbOperation;
            private set
            {
                if (value == _isOnDbOperation) return;
                _isOnDbOperation = value;
                OnPropertyChanged();
            }
        }

        public void Handle(DBOperationMessage message)
        {
            if (message.OperationState == DBOperationMessage.OperationStates.Started &&
                message.Elapsed.TotalMilliseconds > 1000)
                return;

            IsOnDbOperation = message.OperationState == DBOperationMessage.OperationStates.Started;
        }
    }
}