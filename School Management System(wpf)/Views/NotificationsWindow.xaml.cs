using System;
using System.Linq;
using System.Windows;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class NotificationsWindow : Window
    {
        private readonly User _currentUser;
        private readonly NotificationCenterService _notificationCenterService = new();

        public NotificationsWindow(User currentUser)
        {
            _currentUser = currentUser;

            InitializeComponent();

            btnDismiss.Click += (_, _) => Close();
            btnMarkAllRead.Click += (_, _) => MarkAllRead();

            LoadNotifications();
        }

        private void LoadNotifications()
        {
            try
            {
                var notifications = _notificationCenterService
                    .GetForUser(_currentUser, includeRead: false, take: 50)
                    .Select(x => new NotificationRow
                    {
                        CreatedAtText = x.CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                        Category = string.IsNullOrWhiteSpace(x.Category) ? "General" : x.Category,
                        Title = x.Title,
                        Message = x.Message
                    })
                    .ToList();

                listNotifications.ItemsSource = notifications;
                txtUnreadCount.Text = notifications.Count.ToString();

                if (notifications.Count == 0)
                {
                    txtSubtitle.Text = "You are all caught up.";
                    txtEmptyState.Visibility = Visibility.Visible;
                    listNotifications.Visibility = Visibility.Collapsed;
                    btnMarkAllRead.IsEnabled = false;
                }
                else
                {
                    txtSubtitle.Text = notifications.Count == 1
                        ? "You have 1 new notification."
                        : $"You have {notifications.Count} new notifications.";
                    txtEmptyState.Visibility = Visibility.Collapsed;
                    listNotifications.Visibility = Visibility.Visible;
                    btnMarkAllRead.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                txtSubtitle.Text = "Could not load notifications.";
                txtEmptyState.Text = ex.Message;
                txtEmptyState.Visibility = Visibility.Visible;
                listNotifications.Visibility = Visibility.Collapsed;
                txtUnreadCount.Text = "—";
                btnMarkAllRead.IsEnabled = false;
            }
        }

        private void MarkAllRead()
        {
            try
            {
                _notificationCenterService.MarkAllAsRead(_currentUser);
                LoadNotifications();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Unable to mark notifications as read.{Environment.NewLine}{ex.Message}",
                    "Notifications",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private sealed class NotificationRow
        {
            public string CreatedAtText { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }
    }
}
