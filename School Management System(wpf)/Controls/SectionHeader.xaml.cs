using System.Windows;
using System.Windows.Controls;

namespace School_Management_System.Controls
{
    public partial class SectionHeader : UserControl
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(SectionHeader), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
            nameof(Subtitle), typeof(string), typeof(SectionHeader), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ActionsContentProperty = DependencyProperty.Register(
            nameof(ActionsContent), typeof(object), typeof(SectionHeader), new PropertyMetadata(null));

        public SectionHeader()
        {
            InitializeComponent();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        public object? ActionsContent
        {
            get => GetValue(ActionsContentProperty);
            set => SetValue(ActionsContentProperty, value);
        }
    }
}
