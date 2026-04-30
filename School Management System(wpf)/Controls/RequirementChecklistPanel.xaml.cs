using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using School_Management_System.Models;

namespace School_Management_System.Controls
{
    public partial class RequirementChecklistPanel : UserControl
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(RequirementChecklistPanel), new PropertyMetadata("Requirement Checklist"));

        public static readonly DependencyProperty SummaryTextProperty = DependencyProperty.Register(
            nameof(SummaryText), typeof(string), typeof(RequirementChecklistPanel), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty HintTextProperty = DependencyProperty.Register(
            nameof(HintText), typeof(string), typeof(RequirementChecklistPanel), new PropertyMetadata("Final approval requires all required documents to be submitted and validated."));

        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
            nameof(Items), typeof(IEnumerable<RequirementChecklistItem>), typeof(RequirementChecklistPanel), new PropertyMetadata(null));

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem), typeof(RequirementChecklistItem), typeof(RequirementChecklistPanel), new PropertyMetadata(null));

        public RequirementChecklistPanel()
        {
            InitializeComponent();
        }

        public event SelectionChangedEventHandler? SelectionChanged;

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string SummaryText
        {
            get => (string)GetValue(SummaryTextProperty);
            set => SetValue(SummaryTextProperty, value);
        }

        public string HintText
        {
            get => (string)GetValue(HintTextProperty);
            set => SetValue(HintTextProperty, value);
        }

        public IEnumerable<RequirementChecklistItem>? Items
        {
            get => (IEnumerable<RequirementChecklistItem>?)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public RequirementChecklistItem? SelectedItem
        {
            get => (RequirementChecklistItem?)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        private void GridChecklist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }
    }
}
