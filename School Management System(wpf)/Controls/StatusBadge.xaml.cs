using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace School_Management_System.Controls
{
    public partial class StatusBadge : UserControl
    {
        private readonly record struct BadgePalette(string BackgroundHex, string BorderHex, string ForegroundHex);

        private static readonly BadgePalette DefaultPalette = new("#F8FAFC", "#CBD5E1", "#334155");
        private static readonly Dictionary<string, BadgePalette> PaletteByStatus = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ACTIVE"] = new BadgePalette("#ECFDF3", "#86EFAC", "#166534"),
            ["INACTIVE"] = new BadgePalette("#F1F5F9", "#CBD5E1", "#334155"),
            ["LOCKED"] = new BadgePalette("#FEF2F2", "#FCA5A5", "#991B1B"),
            ["PENDING"] = new BadgePalette("#FFF7ED", "#FDBA74", "#9A3412"),
            ["SUBMITTED"] = new BadgePalette("#EFF6FF", "#93C5FD", "#1D4ED8"),
            ["MISSING"] = new BadgePalette("#FFF7ED", "#FDBA74", "#9A3412"),
            ["VERIFIED"] = new BadgePalette("#ECFDF3", "#86EFAC", "#166534"),
            ["EXPIRED"] = new BadgePalette("#FFFBEB", "#FCD34D", "#92400E"),
            ["UNDER_REVIEW"] = new BadgePalette("#EEF2FF", "#A5B4FC", "#3730A3"),
            ["PENDING_REQUIREMENTS"] = new BadgePalette("#FFFBEB", "#FCD34D", "#92400E"),
            ["READY_FOR_APPROVAL"] = new BadgePalette("#ECFEFF", "#67E8F9", "#155E75"),
            ["APPROVED"] = new BadgePalette("#ECFDF3", "#86EFAC", "#166534"),
            ["ENROLLED"] = new BadgePalette("#ECFEFF", "#67E8F9", "#155E75"),
            ["RESERVED"] = new BadgePalette("#EEF2FF", "#A5B4FC", "#3730A3"),
            ["WAITLISTED"] = new BadgePalette("#EEF2FF", "#A5B4FC", "#3730A3"),
            ["RETURNED_FOR_CORRECTION"] = new BadgePalette("#FFF7ED", "#FDBA74", "#9A3412"),
            ["REJECTED"] = new BadgePalette("#FEF2F2", "#FCA5A5", "#991B1B"),
            ["CANCELLED"] = new BadgePalette("#FEF2F2", "#FCA5A5", "#991B1B"),
            ["DROPPED"] = new BadgePalette("#FFF7ED", "#FDBA74", "#9A3412"),
            ["ARCHIVED"] = new BadgePalette("#F1F5F9", "#CBD5E1", "#475569"),
            ["COMPLETED"] = new BadgePalette("#ECFDF3", "#86EFAC", "#166534"),
            ["TRANSFERRED_OUT"] = new BadgePalette("#E0F2FE", "#7DD3FC", "#0C4A6E")
        };

        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
            nameof(Status), typeof(string), typeof(StatusBadge), new PropertyMetadata(string.Empty, OnStatusChanged));

        public static readonly DependencyProperty DisplayTextProperty = DependencyProperty.Register(
            nameof(DisplayText), typeof(string), typeof(StatusBadge), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty BadgeBackgroundProperty = DependencyProperty.Register(
            nameof(BadgeBackground), typeof(Brush), typeof(StatusBadge), new PropertyMetadata(Brushes.Transparent));

        public static readonly DependencyProperty BadgeBorderBrushProperty = DependencyProperty.Register(
            nameof(BadgeBorderBrush), typeof(Brush), typeof(StatusBadge), new PropertyMetadata(Brushes.Transparent));

        public static readonly DependencyProperty BadgeForegroundProperty = DependencyProperty.Register(
            nameof(BadgeForeground), typeof(Brush), typeof(StatusBadge), new PropertyMetadata(Brushes.Black));

        public StatusBadge()
        {
            InitializeComponent();
            UpdateVisualState();
        }

        public string Status
        {
            get => (string)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        public string DisplayText
        {
            get => (string)GetValue(DisplayTextProperty);
            private set => SetValue(DisplayTextProperty, value);
        }

        public Brush BadgeBackground
        {
            get => (Brush)GetValue(BadgeBackgroundProperty);
            private set => SetValue(BadgeBackgroundProperty, value);
        }

        public Brush BadgeBorderBrush
        {
            get => (Brush)GetValue(BadgeBorderBrushProperty);
            private set => SetValue(BadgeBorderBrushProperty, value);
        }

        public Brush BadgeForeground
        {
            get => (Brush)GetValue(BadgeForegroundProperty);
            private set => SetValue(BadgeForegroundProperty, value);
        }

        private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StatusBadge badge)
            {
                badge.UpdateVisualState();
            }
        }

        private void UpdateVisualState()
        {
            var key = NormalizeStatus(Status);
            DisplayText = BuildDisplayText(key);

            var palette = PaletteByStatus.TryGetValue(key, out var specificPalette)
                ? specificPalette
                : DefaultPalette;

            BadgeBackground = BuildBrush(palette.BackgroundHex);
            BadgeBorderBrush = BuildBrush(palette.BorderHex);
            BadgeForeground = BuildBrush(palette.ForegroundHex);
        }

        private static string NormalizeStatus(string? value)
        {
            return (value ?? string.Empty).Trim().Replace(" ", "_").ToUpperInvariant();
        }

        private static string BuildDisplayText(string statusKey)
        {
            if (string.IsNullOrWhiteSpace(statusKey))
            {
                return "UNKNOWN";
            }

            return statusKey.Replace("_", " ");
        }

        private static SolidColorBrush BuildBrush(string hex)
        {
            return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        }
    }
}
