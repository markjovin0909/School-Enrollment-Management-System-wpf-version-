using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class SchedulesWindow : Window
    {
        private readonly ClassScheduleService _scheduleService = new();
        private readonly ClassOfferingService _offeringService = new();
        private readonly SchoolYearService _schoolYearService = new();
        private readonly SectionService _sectionService = new();
        private readonly TeacherService _teacherService = new();
        private readonly SubjectService _subjectService = new();
        private readonly RoomService _roomService = new();
        private readonly TimeSlotService _timeSlotService = new();
        private readonly bool _createOnly;

        private DataTable _table = new();
        private long? _selectedId;

        private List<ClassOffering> _offerings = new();
        private List<SchoolYear> _schoolYears = new();
        private List<Section> _sections = new();
        private List<Teacher> _teachers = new();
        private List<Subject> _subjects = new();
        private List<Room> _rooms = new();
        private List<TimeSlot> _timeSlots = new();

        private bool _suppressEvents;

        public SchedulesWindow(bool createOnly = false)
        {
            _createOnly = createOnly;
            InitializeComponent();

            cboFilterSchoolYear.SelectionChanged += (_, _) => { if (!_suppressEvents) LoadData(); };
            cboFilterTeacher.SelectionChanged += (_, _) => { if (!_suppressEvents) LoadData(); };
            cboFilterSection.SelectionChanged += (_, _) => { if (!_suppressEvents) LoadData(); };
            txtSearch.TextChanged += (_, _) => { if (!_suppressEvents) LoadData(); };

            gridSchedules.AutoGeneratingColumn += (_, e) =>
            {
                if (e.PropertyName == "Id" || e.PropertyName == "Time Slot")
                {
                    e.Cancel = true;
                }
            };
            gridSchedules.SelectionChanged += GridSchedules_SelectionChanged;
            btnNew.Click += (_, _) => OpenCreateWindow();
            btnRefresh.Click += (_, _) => LoadData();
            btnAdd.Click += (_, _) =>
            {
                if (_createOnly)
                {
                    AddSchedule();
                }
                else
                {
                    OpenCreateWindow();
                }
            };
            btnSave.Click += (_, _) => SaveSchedule();
            btnDelete.Click += (_, _) => DeleteSchedule();
            btnClear.Click += (_, _) =>
            {
                if (_createOnly)
                {
                    Close();
                }
                else
                {
                    ClearEditor();
                }
            };
            btnExport.Click += (_, _) => CsvExportService.SaveDataTable(_table, "class_schedules.csv");

            cboDay.ItemsSource = BuildDayOptions();
            cboDay.DisplayMemberPath = "Label";
            cboDay.SelectedValuePath = "Value";
            cboDay.SelectedIndex = 0;
            cboOffering.SelectionChanged += (_, _) => UpdateScheduleWarnings();
            cboRoom.SelectionChanged += (_, _) => UpdateScheduleWarnings();
            cboTimeSlot.SelectionChanged += (_, _) => UpdateScheduleWarnings();
            cboDay.SelectionChanged += (_, _) => UpdateScheduleWarnings();
            txtStart.TextChanged += (_, _) => UpdateScheduleWarnings();
            txtEnd.TextChanged += (_, _) => UpdateScheduleWarnings();

            txtStart.Text = "07:00";
            txtEnd.Text = "08:00";

            LoadLookups();
            if (_createOnly)
            {
                ConfigureCreateMode();
            }
            else
            {
                LoadData();
            }
        }

        private void OpenCreateWindow()
        {
            var window = new SchedulesWindow(true) { Owner = this };
            if (window.ShowDialog() == true)
            {
                LoadLookups();
                LoadData();
            }
        }

        private void ConfigureCreateMode()
        {
            Title = "Create Schedule";
            searchPanel.Visibility = Visibility.Collapsed;
            gridSchedules.Visibility = Visibility.Collapsed;
            Grid.SetColumn(editorPanel, 0);
            Grid.SetColumnSpan(editorPanel, 2);
            editorPanel.Margin = new Thickness(0);
            btnAdd.Content = "Create";
            btnSave.Visibility = Visibility.Collapsed;
            btnDelete.Visibility = Visibility.Collapsed;
            btnExport.Visibility = Visibility.Collapsed;
            btnClear.Content = "Cancel";
            Width = 640;
            Height = 600;
            MinWidth = 640;
            MinHeight = 600;
            ClearEditor();
        }

        private void LoadLookups()
        {
            _schoolYears = _schoolYearService.GetAll().Where(x => !x.IsArchived).ToList();
            _sections = _sectionService.GetAll().Where(x => !x.IsArchived).ToList();
            _teachers = _teacherService.GetAll().ToList();
            _subjects = _subjectService.GetAll().ToList();
            _rooms = _roomService.GetAll().ToList();
            _timeSlots = _timeSlotService.GetAll().ToList();
            _offerings = _offeringService.GetAll().Where(x => x.Status != ClassOfferingStatus.ARCHIVED).ToList();

            _suppressEvents = true;

            cboFilterSchoolYear.ItemsSource = BuildSchoolYearFilter();
            cboFilterSchoolYear.DisplayMemberPath = "Name";
            cboFilterSchoolYear.SelectedValuePath = "Id";
            cboFilterSchoolYear.SelectedValue = 0L;

            cboFilterTeacher.ItemsSource = BuildTeacherFilter();
            cboFilterTeacher.DisplayMemberPath = "Name";
            cboFilterTeacher.SelectedValuePath = "Id";
            cboFilterTeacher.SelectedValue = 0L;

            cboFilterSection.ItemsSource = BuildSectionFilter();
            cboFilterSection.DisplayMemberPath = "Name";
            cboFilterSection.SelectedValuePath = "Id";
            cboFilterSection.SelectedValue = 0L;

            cboOffering.ItemsSource = BuildOfferingOptions();
            cboOffering.DisplayMemberPath = "Label";
            cboOffering.SelectedValuePath = "Id";
            cboOffering.SelectedIndex = 0;

            cboRoom.ItemsSource = BuildRoomOptions();
            cboRoom.DisplayMemberPath = "Label";
            cboRoom.SelectedValuePath = "Id";
            cboRoom.SelectedValue = 0L;

            cboTimeSlot.ItemsSource = BuildTimeSlotOptions();
            cboTimeSlot.DisplayMemberPath = "Label";
            cboTimeSlot.SelectedValuePath = "Id";
            cboTimeSlot.SelectedValue = 0L;

            _suppressEvents = false;
        }

        private void LoadData(long? preferredId = null)
        {
            _table = new DataTable();
            _table.Columns.Add("Id", typeof(long));
            _table.Columns.Add("School Year");
            _table.Columns.Add("Section");
            _table.Columns.Add("Subject");
            _table.Columns.Add("Teacher");
            _table.Columns.Add("Day");
            _table.Columns.Add("Start");
            _table.Columns.Add("End");
            _table.Columns.Add("Room");
            _table.Columns.Add("Time Slot");
            _table.Columns.Add("Conflicts");

            var filterSy = cboFilterSchoolYear.SelectedValue is long sy && sy != 0 ? sy : (long?)null;
            var filterTeacher = cboFilterTeacher.SelectedValue is long teacher && teacher != 0 ? teacher : (long?)null;
            var filterSection = cboFilterSection.SelectedValue is long section && section != 0 ? section : (long?)null;
            var search = (txtSearch.Text ?? string.Empty).Trim();

            foreach (var schedule in _scheduleService.GetAll())
            {
                var offering = _offerings.FirstOrDefault(o => o.Id == schedule.ClassOfferingId);
                if (offering == null)
                {
                    continue;
                }

                if (filterSy.HasValue && offering.SchoolYearId != filterSy.Value)
                {
                    continue;
                }

                if (filterTeacher.HasValue && offering.TeacherId != filterTeacher.Value)
                {
                    continue;
                }

                if (filterSection.HasValue && offering.SectionId != filterSection.Value)
                {
                    continue;
                }

                var syName = _schoolYears.FirstOrDefault(x => x.Id == offering.SchoolYearId)?.Name ?? string.Empty;
                var sectionName = _sections.FirstOrDefault(x => x.Id == offering.SectionId)?.Name ?? string.Empty;
                var subjectName = _subjects.FirstOrDefault(x => x.Id == offering.SubjectId)?.Title ?? string.Empty;
                var teacherName = offering.TeacherId.HasValue
                    ? BuildTeacherName(_teachers.FirstOrDefault(x => x.Id == offering.TeacherId.Value))
                    : string.Empty;
                var dayLabel = BuildDayOptions().FirstOrDefault(x => x.Value == schedule.DayOfWeek)?.Label ?? schedule.DayOfWeek.ToString(CultureInfo.InvariantCulture);
                var roomLabel = schedule.RoomId.HasValue ? _rooms.FirstOrDefault(x => x.Id == schedule.RoomId.Value)?.Name ?? string.Empty : string.Empty;
                var timeSlotLabel = schedule.TimeSlotId.HasValue ? _timeSlots.FirstOrDefault(x => x.Id == schedule.TimeSlotId.Value)?.Name ?? string.Empty : string.Empty;

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var match =
                        syName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        sectionName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        subjectName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        teacherName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        roomLabel.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        timeSlotLabel.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        dayLabel.Contains(search, StringComparison.OrdinalIgnoreCase);
                    if (!match)
                    {
                        continue;
                    }
                }

                _table.Rows.Add(
                    schedule.Id,
                    syName,
                    sectionName,
                    subjectName,
                    teacherName,
                    dayLabel,
                    schedule.StartTime.ToString(@"hh\:mm"),
                    schedule.EndTime.ToString(@"hh\:mm"),
                    roomLabel,
                    timeSlotLabel,
                    CountConflicts(schedule, offering));
            }

            gridSchedules.ItemsSource = _table.DefaultView;

            if (preferredId.HasValue && SelectSchedule(preferredId.Value))
            {
                return;
            }

            _selectedId = null;
            gridSchedules.SelectedItem = null;
        }

        private bool SelectSchedule(long id)
        {
            foreach (var item in gridSchedules.Items)
            {
                if (item is DataRowView row && row.Row.Field<long>("Id") == id)
                {
                    gridSchedules.SelectedItem = item;
                    gridSchedules.ScrollIntoView(item);
                    _selectedId = id;
                    return true;
                }
            }

            return false;
        }

        private void GridSchedules_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridSchedules.SelectedItem is not DataRowView row)
            {
                _selectedId = null;
                return;
            }

            _selectedId = row.Row.Field<long>("Id");
            var schedule = _scheduleService.GetById(_selectedId.Value);
            if (schedule == null)
            {
                return;
            }

            cboOffering.SelectedValue = schedule.ClassOfferingId;
            cboRoom.SelectedValue = schedule.RoomId ?? 0L;
            cboTimeSlot.SelectedValue = schedule.TimeSlotId ?? 0L;
            cboDay.SelectedValue = schedule.DayOfWeek;
            txtStart.Text = schedule.StartTime.ToString(@"hh\:mm");
            txtEnd.Text = schedule.EndTime.ToString(@"hh\:mm");
            UpdateScheduleWarnings();
        }

        private void AddSchedule()
        {
            if (!TryReadEditor(out var offeringId, out var roomId, out var timeSlotId, out var day, out var startTime, out var endTime))
            {
                return;
            }

            var now = DateTime.UtcNow;
            var entity = new ClassSchedule
            {
                ClassOfferingId = offeringId,
                RoomId = roomId,
                TimeSlotId = timeSlotId,
                DayOfWeek = day,
                StartTime = startTime,
                EndTime = endTime,
                CreatedAt = now,
                UpdatedAt = now
            };

            _scheduleService.Create(entity);
            AuditTrailService.Log("CREATE", "class_schedules", entity.Id, null, entity);
            if (_createOnly)
            {
                DialogResult = true;
                Close();
                return;
            }

            LoadData(entity.Id);
        }

        private void SaveSchedule()
        {
            if (!_selectedId.HasValue)
            {
                MessageBox.Show("Select a schedule first.", "Schedule", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryReadEditor(out var offeringId, out var roomId, out var timeSlotId, out var day, out var startTime, out var endTime))
            {
                return;
            }

            var entity = _scheduleService.GetById(_selectedId.Value);
            if (entity == null)
            {
                return;
            }

            var oldData = new { entity.ClassOfferingId, entity.RoomId, entity.TimeSlotId, entity.DayOfWeek, entity.StartTime, entity.EndTime };
            entity.ClassOfferingId = offeringId;
            entity.RoomId = roomId;
            entity.TimeSlotId = timeSlotId;
            entity.DayOfWeek = day;
            entity.StartTime = startTime;
            entity.EndTime = endTime;
            entity.UpdatedAt = DateTime.UtcNow;

            _scheduleService.Update(entity);
            AuditTrailService.Log("UPDATE", "class_schedules", entity.Id, oldData, entity);
            LoadData(entity.Id);
        }

        private void DeleteSchedule()
        {
            if (!_selectedId.HasValue)
            {
                MessageBox.Show("Select a schedule first.", "Schedule", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var entity = _scheduleService.GetById(_selectedId.Value);
            if (entity == null)
            {
                return;
            }

            var confirm = MessageBox.Show("Delete selected schedule?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            _scheduleService.Delete(entity.Id);
            AuditTrailService.Log("DELETE", "class_schedules", entity.Id, entity, null);
            LoadData();
            ClearEditor();
        }

        private bool TryReadEditor(out long offeringId, out long? roomId, out long? timeSlotId, out byte day, out TimeSpan startTime, out TimeSpan endTime)
        {
            offeringId = 0;
            roomId = null;
            timeSlotId = null;
            day = 1;
            startTime = default;
            endTime = default;

            if (cboOffering.SelectedValue is not long selectedOfferingId)
            {
                MessageBox.Show("Select a class offering.", "Schedule", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            offeringId = selectedOfferingId;
            if (cboRoom.SelectedValue is long selectedRoomId && selectedRoomId > 0)
            {
                roomId = selectedRoomId;
            }

            if (cboTimeSlot.SelectedValue is long selectedTimeSlotId && selectedTimeSlotId > 0)
            {
                timeSlotId = selectedTimeSlotId;
            }

            if (cboDay.SelectedValue is not byte selectedDay)
            {
                MessageBox.Show("Select a day.", "Schedule", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            day = selectedDay;

            if (!TimeSpan.TryParse(txtStart.Text.Trim(), out startTime))
            {
                MessageBox.Show("Start time must be in HH:mm format.", "Schedule", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!TimeSpan.TryParse(txtEnd.Text.Trim(), out endTime))
            {
                MessageBox.Show("End time must be in HH:mm format.", "Schedule", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (startTime >= endTime)
            {
                MessageBox.Show("Start time must be earlier than end time.", "Schedule", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void ClearEditor()
        {
            _selectedId = null;
            cboOffering.SelectedIndex = cboOffering.Items.Count > 0 ? 0 : -1;
            cboRoom.SelectedValue = 0L;
            cboTimeSlot.SelectedValue = 0L;
            cboDay.SelectedIndex = 0;
            txtStart.Text = "07:00";
            txtEnd.Text = "08:00";
            gridSchedules.SelectedItem = null;
            UpdateScheduleWarnings();
        }

        private void UpdateScheduleWarnings()
        {
            var warnings = BuildScheduleWarnings();
            if (warnings.Count == 0)
            {
                scheduleWarningPanel.Visibility = Visibility.Collapsed;
                txtScheduleWarnings.Text = string.Empty;
                return;
            }

            scheduleWarningPanel.Visibility = Visibility.Visible;
            txtScheduleWarnings.Text = string.Join(Environment.NewLine, warnings.Select(x => $"• {x}"));
        }

        private List<string> BuildScheduleWarnings()
        {
            var warnings = new List<string>();
            if (!TimeSpan.TryParse(txtStart.Text.Trim(), out var startTime) ||
                !TimeSpan.TryParse(txtEnd.Text.Trim(), out var endTime) ||
                startTime >= endTime ||
                cboOffering.SelectedValue is not long offeringId ||
                cboDay.SelectedValue is not byte day)
            {
                return warnings;
            }

            var offering = _offerings.FirstOrDefault(x => x.Id == offeringId);
            if (offering == null)
            {
                return warnings;
            }

            if (!offering.TeacherId.HasValue)
            {
                warnings.Add("Selected offering has no teacher assigned yet.");
            }

            if (!offering.TeacherId.HasValue && cboRoom.SelectedValue is not long roomCheckIdOnly)
            {
                return warnings;
            }

            var overlappingSchedules = _scheduleService.GetAll()
                .Where(x => !_selectedId.HasValue || x.Id != _selectedId.Value)
                .Where(x => x.DayOfWeek == day && TimesOverlap(x.StartTime, x.EndTime, startTime, endTime))
                .ToList();

            foreach (var schedule in overlappingSchedules)
            {
                var otherOffering = _offerings.FirstOrDefault(x => x.Id == schedule.ClassOfferingId);
                if (otherOffering == null)
                {
                    continue;
                }

                if (offering.TeacherId.HasValue &&
                    otherOffering.TeacherId.HasValue &&
                    offering.TeacherId.Value == otherOffering.TeacherId.Value)
                {
                    warnings.Add("Teacher already assigned during the selected time.");
                }

                if (cboRoom.SelectedValue is long selectedRoomId &&
                    selectedRoomId > 0 &&
                    schedule.RoomId.HasValue &&
                    schedule.RoomId.Value == selectedRoomId)
                {
                    warnings.Add("Selected room is already occupied during the selected time.");
                }

                if (otherOffering.SectionId == offering.SectionId)
                {
                    warnings.Add("Selected section already has another class during the selected time.");
                }

                if (cboTimeSlot.SelectedValue is long selectedTimeSlotId &&
                    selectedTimeSlotId > 0 &&
                    schedule.TimeSlotId.HasValue &&
                    schedule.TimeSlotId.Value == selectedTimeSlotId &&
                    (schedule.StartTime != startTime || schedule.EndTime != endTime))
                {
                    warnings.Add("Chosen time slot overlaps with another assignment using the same slot.");
                }
            }

            return warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private int CountConflicts(ClassSchedule schedule, ClassOffering offering)
        {
            var total = 0;
            var overlappingSchedules = _scheduleService.GetAll()
                .Where(x => x.Id != schedule.Id)
                .Where(x => x.DayOfWeek == schedule.DayOfWeek && TimesOverlap(x.StartTime, x.EndTime, schedule.StartTime, schedule.EndTime))
                .ToList();

            foreach (var other in overlappingSchedules)
            {
                var otherOffering = _offerings.FirstOrDefault(x => x.Id == other.ClassOfferingId);
                if (otherOffering == null)
                {
                    continue;
                }

                if (offering.TeacherId.HasValue && otherOffering.TeacherId == offering.TeacherId)
                {
                    total++;
                }

                if (schedule.RoomId.HasValue && other.RoomId == schedule.RoomId)
                {
                    total++;
                }

                if (otherOffering.SectionId == offering.SectionId)
                {
                    total++;
                }
            }

            return total;
        }

        private static bool TimesOverlap(TimeSpan existingStart, TimeSpan existingEnd, TimeSpan candidateStart, TimeSpan candidateEnd)
        {
            return candidateStart < existingEnd && candidateEnd > existingStart;
        }

        private List<FilterItem> BuildSchoolYearFilter()
        {
            var items = new List<FilterItem> { new() { Id = 0, Name = "All School Years" } };
            items.AddRange(_schoolYears.Select(x => new FilterItem { Id = x.Id, Name = x.Name }));
            return items;
        }

        private List<FilterItem> BuildTeacherFilter()
        {
            var items = new List<FilterItem> { new() { Id = 0, Name = "All Teachers" } };
            items.AddRange(_teachers.Select(t => new FilterItem { Id = t.Id, Name = BuildTeacherName(t) }));
            return items;
        }

        private List<FilterItem> BuildSectionFilter()
        {
            var items = new List<FilterItem> { new() { Id = 0, Name = "All Sections" } };
            items.AddRange(_sections.Select(s => new FilterItem { Id = s.Id, Name = s.Name }));
            return items;
        }

        private List<LabelOption> BuildOfferingOptions()
        {
            var list = _offerings
                .OrderBy(o => o.SchoolYearId)
                .ThenBy(o => o.SectionId)
                .ThenBy(o => o.SubjectId)
                .Select(o =>
                {
                    var sy = _schoolYears.FirstOrDefault(x => x.Id == o.SchoolYearId)?.Name ?? $"SY {o.SchoolYearId}";
                    var section = _sections.FirstOrDefault(x => x.Id == o.SectionId)?.Name ?? $"SEC {o.SectionId}";
                    var subject = _subjects.FirstOrDefault(x => x.Id == o.SubjectId)?.Title ?? $"SUB {o.SubjectId}";
                    return new LabelOption { Id = o.Id, Label = $"[{sy}] {section} - {subject}" };
                })
                .ToList();
            return list;
        }

        private List<LabelOption> BuildRoomOptions()
        {
            var list = new List<LabelOption> { new() { Id = 0, Label = "(None)" } };
            list.AddRange(_rooms.OrderBy(x => x.Code).Select(r => new LabelOption
            {
                Id = r.Id,
                Label = string.IsNullOrWhiteSpace(r.Code) ? r.Name : $"{r.Code} - {r.Name}"
            }));
            return list;
        }

        private List<LabelOption> BuildTimeSlotOptions()
        {
            var list = new List<LabelOption> { new() { Id = 0, Label = "(None)" } };
            list.AddRange(_timeSlots.OrderBy(x => x.SortOrder).ThenBy(x => x.StartTime).Select(s => new LabelOption
            {
                Id = s.Id,
                Label = string.IsNullOrWhiteSpace(s.Code) ? s.Name : $"{s.Code} - {s.Name}"
            }));
            return list;
        }

        private static List<DayOption> BuildDayOptions()
        {
            return new List<DayOption>
            {
                new() { Value = 1, Label = "Monday" },
                new() { Value = 2, Label = "Tuesday" },
                new() { Value = 3, Label = "Wednesday" },
                new() { Value = 4, Label = "Thursday" },
                new() { Value = 5, Label = "Friday" },
                new() { Value = 6, Label = "Saturday" },
                new() { Value = 7, Label = "Sunday" }
            };
        }

        private static string BuildTeacherName(Teacher? teacher)
        {
            return teacher == null ? string.Empty : $"{teacher.LastName}, {teacher.FirstName}";
        }

        private sealed class FilterItem
        {
            public long Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private sealed class LabelOption
        {
            public long Id { get; set; }
            public string Label { get; set; } = string.Empty;
        }

        private sealed class DayOption
        {
            public byte Value { get; set; }
            public string Label { get; set; } = string.Empty;
        }
    }
}
