using System;
using System.Collections.Generic;
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
        private readonly AnnouncementService _announcementService = new();

        public NotificationsWindow(User currentUser)
        {
            _currentUser = currentUser;

            InitializeComponent();

            txtSubtitle.Text = $"Review updates for {_currentUser.Username} before entering the dashboard.";

            btnRefresh.Click += (_, _) => LoadNotifications();
            btnMarkAllRead.Click += (_, _) => MarkAllRead();
            btnContinue.Click += (_, _) =>
            {
                DialogResult = true;
                Close();
            };

            LoadNotifications();
        }

        private void LoadNotifications()
        {
            try
            {
                var notifications = _notificationCenterService
                    .GetForUser(_currentUser, includeRead: true, take: 200)
                    .Select(x => new NotificationRow
                    {
                        CreatedAtText = x.CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                        Category = string.IsNullOrWhiteSpace(x.Category) ? "General" : x.Category,
                        Title = x.Title,
                        Message = x.Message
                    })
                    .ToList();

                var announcements = _announcementService
                    .GetAll()
                    .OrderByDescending(x => x.PostedAt)
                    .Take(100)
                    .Select(x => new AnnouncementRow
                    {
                        PostedAtText = x.PostedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                        Title = x.Title,
                        Audience = x.AudienceType.ToString(),
                        Body = x.Body
                    })
                    .ToList();

                gridNotifications.ItemsSource = notifications;
                gridAnnouncements.ItemsSource = announcements;

                txtUnreadCount.Text = _notificationCenterService.GetUnreadCount(_currentUser).ToString();

                if (notifications.Count == 0 && announcements.Count == 0)
                {
                    ShowStatus("No pending updates", "There are no notifications or announcements to review right now.");
                }
                else if (notifications.Count == 0)
                {
                    ShowStatus("Announcements loaded", $"{announcements.Count} announcement(s) available. No direct notifications are pending.");
                }
                else
                {
                    HideStatus();
                }
            }
            catch (Exception ex)
            {
                ShowStatus("Notification load failed", ex.Message);
            }
        }

        private void MarkAllRead()
        {
            try
            {
                _notificationCenterService.MarkAllAsRead(_currentUser);
                LoadNotifications();
                ShowStatus("Notifications updated", "All visible notifications have been marked as read.");
            }
            catch (Exception ex)
            {
                ShowStatus("Mark all read failed", ex.Message);
            }
        }

        private void ShowStatus(string title, string message)
        {
            txtStatusTitle.Text = title;
            txtStatusMessage.Text = message;
            statusBanner.Visibility = Visibility.Visible;
        }

        private void HideStatus()
        {
            statusBanner.Visibility = Visibility.Collapsed;
            txtStatusTitle.Text = string.Empty;
            txtStatusMessage.Text = string.Empty;
        }

        private sealed class NotificationRow
        {
            public string CreatedAtText { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }

        private sealed class AnnouncementRow
        {
            public string PostedAtText { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Audience { get; set; } = string.Empty;
            public string Body { get; set; } = string.Empty;
        }
    }
}
