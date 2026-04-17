using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace School_Management_System.Views
{
    public partial class ReasonPromptWindow : Window
    {
        private readonly bool _detailsRequiredForOther;

        public ReasonPromptWindow(
            string title,
            string prompt,
            IEnumerable<ReasonOption> options,
            bool detailsRequiredForOther = true)
        {
            InitializeComponent();

            _detailsRequiredForOther = detailsRequiredForOther;
            txtTitle.Text = string.IsNullOrWhiteSpace(title) ? "Reason Required" : title.Trim();
            txtPrompt.Text = string.IsNullOrWhiteSpace(prompt) ? "Please provide a reason before continuing." : prompt.Trim();

            var resolvedOptions = (options ?? Enumerable.Empty<ReasonOption>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Code))
                .Select(x => new ReasonOption(x.Code.Trim().ToUpperInvariant(), string.IsNullOrWhiteSpace(x.Label) ? x.Code.Trim().ToUpperInvariant() : x.Label.Trim()))
                .GroupBy(x => x.Code)
                .Select(x => x.First())
                .ToList();

            if (resolvedOptions.Count == 0)
            {
                resolvedOptions.Add(new ReasonOption("ADMIN_REVIEW", "Administrative review"));
                resolvedOptions.Add(new ReasonOption("DATA_CORRECTION", "Data correction"));
                resolvedOptions.Add(new ReasonOption("POLICY_DECISION", "Policy decision"));
                resolvedOptions.Add(new ReasonOption("OTHER", "Other (details required)"));
            }

            cboReasonCode.ItemsSource = resolvedOptions;
            cboReasonCode.SelectedIndex = 0;

            btnConfirm.Click += (_, _) => Confirm();
            btnCancel.Click += (_, _) => Cancel();
            cboReasonCode.SelectionChanged += (_, _) => HideValidation();
            txtReasonDetail.TextChanged += (_, _) => HideValidation();
        }

        public string SelectedReasonCode =>
            (cboReasonCode.SelectedValue as string ?? string.Empty).Trim().ToUpperInvariant();

        public string ReasonDetail => (txtReasonDetail.Text ?? string.Empty).Trim();

        private void Confirm()
        {
            var code = SelectedReasonCode;
            var detail = ReasonDetail;

            if (string.IsNullOrWhiteSpace(code))
            {
                ShowValidation("Select a reason code.");
                return;
            }

            if (_detailsRequiredForOther && code == "OTHER" && string.IsNullOrWhiteSpace(detail))
            {
                ShowValidation("Details are required when reason code is OTHER.");
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel()
        {
            DialogResult = false;
            Close();
        }

        private void ShowValidation(string message)
        {
            txtValidation.Text = message;
            validationBanner.Visibility = Visibility.Visible;
        }

        private void HideValidation()
        {
            validationBanner.Visibility = Visibility.Collapsed;
            txtValidation.Text = string.Empty;
        }

        public sealed class ReasonOption
        {
            public ReasonOption(string code, string label)
            {
                Code = code;
                Label = label;
            }

            public string Code { get; }
            public string Label { get; }
        }
    }
}
