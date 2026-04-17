using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace School_Management_System.Controls
{
    public partial class SearchToolbar : UserControl
    {
        public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register(
            nameof(SearchText), typeof(string), typeof(SearchToolbar), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty SearchPlaceholderProperty = DependencyProperty.Register(
            nameof(SearchPlaceholder), typeof(string), typeof(SearchToolbar), new PropertyMetadata("Search"));

        public static readonly DependencyProperty PrimaryActionTextProperty = DependencyProperty.Register(
            nameof(PrimaryActionText), typeof(string), typeof(SearchToolbar), new PropertyMetadata("Add"));

        public static readonly DependencyProperty SecondaryActionTextProperty = DependencyProperty.Register(
            nameof(SecondaryActionText), typeof(string), typeof(SearchToolbar), new PropertyMetadata("Refresh"));

        public static readonly DependencyProperty PrimaryCommandProperty = DependencyProperty.Register(
            nameof(PrimaryCommand), typeof(ICommand), typeof(SearchToolbar), new PropertyMetadata(null));

        public static readonly DependencyProperty SecondaryCommandProperty = DependencyProperty.Register(
            nameof(SecondaryCommand), typeof(ICommand), typeof(SearchToolbar), new PropertyMetadata(null));

        public static readonly DependencyProperty ShowPrimaryActionProperty = DependencyProperty.Register(
            nameof(ShowPrimaryAction), typeof(bool), typeof(SearchToolbar), new PropertyMetadata(true));

        public static readonly DependencyProperty ShowSecondaryActionProperty = DependencyProperty.Register(
            nameof(ShowSecondaryAction), typeof(bool), typeof(SearchToolbar), new PropertyMetadata(true));

        public SearchToolbar()
        {
            InitializeComponent();
        }

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public string SearchPlaceholder
        {
            get => (string)GetValue(SearchPlaceholderProperty);
            set => SetValue(SearchPlaceholderProperty, value);
        }

        public string PrimaryActionText
        {
            get => (string)GetValue(PrimaryActionTextProperty);
            set => SetValue(PrimaryActionTextProperty, value);
        }

        public string SecondaryActionText
        {
            get => (string)GetValue(SecondaryActionTextProperty);
            set => SetValue(SecondaryActionTextProperty, value);
        }

        public ICommand? PrimaryCommand
        {
            get => (ICommand?)GetValue(PrimaryCommandProperty);
            set => SetValue(PrimaryCommandProperty, value);
        }

        public ICommand? SecondaryCommand
        {
            get => (ICommand?)GetValue(SecondaryCommandProperty);
            set => SetValue(SecondaryCommandProperty, value);
        }

        public bool ShowPrimaryAction
        {
            get => (bool)GetValue(ShowPrimaryActionProperty);
            set => SetValue(ShowPrimaryActionProperty, value);
        }

        public bool ShowSecondaryAction
        {
            get => (bool)GetValue(ShowSecondaryActionProperty);
            set => SetValue(ShowSecondaryActionProperty, value);
        }
    }
}
