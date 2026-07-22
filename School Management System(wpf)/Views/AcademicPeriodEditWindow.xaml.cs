using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class AcademicPeriodEditWindow : Window
    {
        private readonly SchoolYearService _schoolYearService = new();
        private readonly GradingPeriodService _gradingPeriodService = new();
        private List<SchoolYear> _schoolYears = new();
        private List<GradingPeriod> _gradingPeriods = new();
        private bool _suppressEvents;

        public long? SavedSchoolYearId { get; private set; }
        public long? SavedGradingPeriodId { get; private set; }

        public AcademicPeriodEditWindow()
        {
            InitializeComponent();

            cboSchoolYear.SelectionChanged += (_, _) =>
            {
                if (_suppressEvents) return;
                LoadSchoolYearFields();
                BindGradingPeriods();
            };
            cboGradingPeriod.SelectionChanged += (_, _) =>
            {
                if (_suppressEvents) return;
                LoadGradingPeriodFields();
            };

            btnSave.Click += (_, _) => SaveChanges();
            btnCancel.Click += (_, _) =>
            {
                DialogResult = false;
                Close();
            };

            LoadData();
        }

        private void LoadData()
        {
            _schoolYears = _schoolYearService.GetAll().Where(x => !x.IsArchived).ToList();
            _gradingPeriods = _gradingPeriodService.GetAll().ToList();

            _suppressEvents = true;
            cboSchoolYear.ItemsSource = _schoolYears;
            cboSchoolYear.DisplayMemberPath = nameof(SchoolYear.Name);

            var active = SchoolYearSelectionHelper.ResolveActive(_schoolYears, _schoolYearService)
                ?? _schoolYears.FirstOrDefault();
            cboSchoolYear.SelectedItem = active;
            _suppressEvents = false;

            LoadSchoolYearFields();
            BindGradingPeriods();
        }

        private void LoadSchoolYearFields()
        {
            if (cboSchoolYear.SelectedItem is not SchoolYear year)
            {
                dpStart.SelectedDate = null;
                dpEnd.SelectedDate = null;
                dpEnrollmentOpen.SelectedDate = null;
                dpEnrollmentClose.SelectedDate = null;
                return;
            }

            dpStart.SelectedDate = year.StartDate?.Date;
            dpEnd.SelectedDate = year.EndDate?.Date;
            dpEnrollmentOpen.SelectedDate = year.EnrollmentOpenDate?.Date;
            dpEnrollmentClose.SelectedDate = year.EnrollmentCloseDate?.Date;
        }

        private void BindGradingPeriods()
        {
            _suppressEvents = true;
            var schoolYearId = (cboSchoolYear.SelectedItem as SchoolYear)?.Id;
            var items = _gradingPeriods
                .Where(x => schoolYearId.HasValue && x.SchoolYearId == schoolYearId.Value)
                .OrderBy(x => x.StartDate ?? DateTime.MaxValue)
                .ThenBy(x => x.Name)
                .Select(x => new GradingPeriodChoice(x))
                .ToList();

            cboGradingPeriod.ItemsSource = items;
            cboGradingPeriod.DisplayMemberPath = nameof(GradingPeriodChoice.Label);

            var current = GetPreferredCurrentPeriod(items.Select(x => x.Period).ToList());
            cboGradingPeriod.SelectedItem = items.FirstOrDefault(x => current != null && x.Period.Id == current.Id)
                ?? items.FirstOrDefault();
            _suppressEvents = false;

            LoadGradingPeriodFields();
        }

        private static GradingPeriod? GetPreferredCurrentPeriod(List<GradingPeriod> periods)
        {
            if (periods.Count == 0) return null;

            var today = DateTime.Today;
            return periods
                .Where(x => x.Status == GradingPeriodStatus.OPEN)
                .OrderBy(x => x.StartDate ?? DateTime.MinValue)
                .LastOrDefault()
                ?? periods
                    .Where(x => x.StartDate.HasValue && x.EndDate.HasValue
                        && x.StartDate.Value.Date <= today
                        && x.EndDate.Value.Date >= today)
                    .OrderBy(x => x.StartDate ?? DateTime.MinValue)
                    .LastOrDefault()
                ?? periods
                    .Where(x => x.Status == GradingPeriodStatus.UPCOMING)
                    .OrderBy(x => x.StartDate ?? DateTime.MaxValue)
                    .FirstOrDefault()
                ?? periods.OrderByDescending(x => x.EndDate ?? DateTime.MinValue).FirstOrDefault();
        }

        private void LoadGradingPeriodFields()
        {
            if (cboGradingPeriod.SelectedItem is not GradingPeriodChoice choice)
            {
                dpGradingStart.SelectedDate = null;
                dpGradingEnd.SelectedDate = null;
                return;
            }

            dpGradingStart.SelectedDate = choice.Period.StartDate?.Date;
            dpGradingEnd.SelectedDate = choice.Period.EndDate?.Date;
        }

        private void SaveChanges()
        {
            HideValidation();

            if (cboSchoolYear.SelectedItem is not SchoolYear selectedYear)
            {
                ShowValidation("Select a school year.");
                return;
            }

            if (dpStart.SelectedDate.HasValue && dpEnd.SelectedDate.HasValue
                && dpStart.SelectedDate.Value.Date > dpEnd.SelectedDate.Value.Date)
            {
                ShowValidation("School year start date cannot be later than end date.");
                return;
            }

            if (dpEnrollmentOpen.SelectedDate.HasValue && dpEnrollmentClose.SelectedDate.HasValue
                && dpEnrollmentOpen.SelectedDate.Value.Date > dpEnrollmentClose.SelectedDate.Value.Date)
            {
                ShowValidation("Enrollment open date cannot be later than enrollment close date.");
                return;
            }

            if (dpGradingStart.SelectedDate.HasValue && dpGradingEnd.SelectedDate.HasValue
                && dpGradingStart.SelectedDate.Value.Date > dpGradingEnd.SelectedDate.Value.Date)
            {
                ShowValidation("Grading period start date cannot be later than end date.");
                return;
            }

            try
            {
                var year = _schoolYearService.GetById(selectedYear.Id);
                if (year == null)
                {
                    ShowValidation("School year was not found.");
                    return;
                }

                var previousActive = _schoolYearService.GetActiveSchoolYear();
                if (previousActive != null && previousActive.Id != year.Id)
                {
                    var confirm = MessageBox.Show(
                        $"'{previousActive.Name}' is currently ACTIVE. Saving will set it to CLOSED and activate '{year.Name}'. Continue?",
                        "Set Active School Year",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question,
                        MessageBoxResult.No);
                    if (confirm != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                var oldYear = new
                {
                    year.Name,
                    year.StartDate,
                    year.EndDate,
                    year.EnrollmentOpenDate,
                    year.EnrollmentCloseDate,
                    year.Status
                };

                year.StartDate = dpStart.SelectedDate?.Date;
                year.EndDate = dpEnd.SelectedDate?.Date;
                year.EnrollmentOpenDate = dpEnrollmentOpen.SelectedDate?.Date;
                year.EnrollmentCloseDate = dpEnrollmentClose.SelectedDate?.Date;
                year.Status = SchoolYearStatus.ACTIVE;
                year.IsArchived = false;
                year.UpdatedAt = DateTime.UtcNow;
                _schoolYearService.Update(year);

                AuditTrailService.Log("UPDATE", "school_years", year.Id, oldYear, new
                {
                    year.Name,
                    year.StartDate,
                    year.EndDate,
                    year.EnrollmentOpenDate,
                    year.EnrollmentCloseDate,
                    year.Status
                });

                long? gradingPeriodId = null;
                if (cboGradingPeriod.SelectedItem is GradingPeriodChoice choice)
                {
                    var period = _gradingPeriodService.GetById(choice.Period.Id);
                    if (period != null)
                    {
                        var oldPeriod = new
                        {
                            period.Name,
                            period.StartDate,
                            period.EndDate,
                            period.Status
                        };

                        period.StartDate = dpGradingStart.SelectedDate?.Date;
                        period.EndDate = dpGradingEnd.SelectedDate?.Date;
                        period.UpdatedAt = DateTime.UtcNow;
                        _gradingPeriodService.Update(period);
                        _gradingPeriodService.SetCurrentOpenPeriod(year.Id, period.Id);

                        var refreshed = _gradingPeriodService.GetById(period.Id);
                        AuditTrailService.Log("UPDATE", "grading_periods", period.Id, oldPeriod, new
                        {
                            refreshed?.Name,
                            refreshed?.StartDate,
                            refreshed?.EndDate,
                            Status = GradingPeriodStatus.OPEN
                        });

                        gradingPeriodId = period.Id;
                    }
                }

                SavedSchoolYearId = year.Id;
                SavedGradingPeriodId = gradingPeriodId;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                AppFeedbackService.ShowError("Failed to save academic period.", ex, "Edit Academic Period", this);
            }
        }

        private void ShowValidation(string message)
        {
            validationSummaryHost.Visibility = Visibility.Visible;
            txtValidationSummary.Text = message;
        }

        private void HideValidation()
        {
            validationSummaryHost.Visibility = Visibility.Collapsed;
            txtValidationSummary.Text = string.Empty;
        }

        private sealed class GradingPeriodChoice
        {
            public GradingPeriodChoice(GradingPeriod period)
            {
                Period = period;
            }

            public GradingPeriod Period { get; }
            public string Label => $"{Period.Name} ({Period.Status})"
                + (Period.StartDate.HasValue && Period.EndDate.HasValue
                    ? $" · {Period.StartDate:MMM dd}–{Period.EndDate:MMM dd}"
                    : string.Empty);
        }
    }
}
