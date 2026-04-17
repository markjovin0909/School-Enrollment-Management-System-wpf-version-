CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
);

START TRANSACTION;

CREATE TABLE `curricula` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `name` longtext NOT NULL,
    `description` longtext NULL,
    `is_active` tinyint(1) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE `grade_components` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `name` longtext NOT NULL,
    `weight` decimal(6,4) NOT NULL,
    `is_active` tinyint(1) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE `grade_levels` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `code` longtext NOT NULL,
    `name` longtext NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE `rooms` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `code` longtext NOT NULL,
    `name` longtext NOT NULL,
    `capacity` int NULL,
    `is_active` tinyint(1) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE `school_years` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `name` longtext NOT NULL,
    `start_date` datetime(6) NULL,
    `end_date` datetime(6) NULL,
    `status` longtext NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE `time_slots` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `code` longtext NOT NULL,
    `name` longtext NOT NULL,
    `start_time` time(6) NOT NULL,
    `end_time` time(6) NOT NULL,
    `is_bell_period` tinyint(1) NOT NULL,
    `sort_order` int NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE `users` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `username` longtext NOT NULL,
    `password_hash` longtext NOT NULL,
    `role` longtext NOT NULL,
    `status` longtext NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE `subjects` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `code` longtext NOT NULL,
    `title` longtext NOT NULL,
    `description` longtext NULL,
    `grade_level_id` bigint NULL,
    `is_active` tinyint(1) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_subjects_grade_levels_grade_level_id` FOREIGN KEY (`grade_level_id`) REFERENCES `grade_levels` (`Id`)
);

CREATE TABLE `grading_periods` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `school_year_id` bigint NOT NULL,
    `name` longtext NOT NULL,
    `start_date` datetime(6) NULL,
    `end_date` datetime(6) NULL,
    `status` longtext NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_grading_periods_school_years_school_year_id` FOREIGN KEY (`school_year_id`) REFERENCES `school_years` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `audit_logs` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `user_id` bigint NOT NULL,
    `action` longtext NOT NULL,
    `entity` longtext NOT NULL,
    `entity_id` bigint NULL,
    `payload` longtext NULL,
    `created_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_audit_logs_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `students` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `user_id` bigint NOT NULL,
    `profile_image_url` longtext NULL,
    `lrn` longtext NOT NULL,
    `first_name` longtext NOT NULL,
    `last_name` longtext NOT NULL,
    `middle_name` longtext NULL,
    `birthdate` datetime(6) NULL,
    `sex` longtext NULL,
    `address` longtext NULL,
    `guardian_name` longtext NULL,
    `guardian_contact` longtext NULL,
    `status` longtext NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_students_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `teachers` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `user_id` bigint NOT NULL,
    `profile_image_url` longtext NULL,
    `employee_no` longtext NULL,
    `first_name` longtext NOT NULL,
    `last_name` longtext NOT NULL,
    `middle_name` longtext NULL,
    `email` longtext NULL,
    `contact_no` longtext NULL,
    `hire_date` datetime(6) NULL,
    `status` longtext NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_teachers_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `curriculum_subjects` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `curriculum_id` bigint NOT NULL,
    `grade_level_id` bigint NOT NULL,
    `subject_id` bigint NOT NULL,
    `semester` tinyint unsigned NULL,
    `is_required` tinyint(1) NOT NULL,
    `sort_order` int NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_curriculum_subjects_curricula_curriculum_id` FOREIGN KEY (`curriculum_id`) REFERENCES `curricula` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_curriculum_subjects_grade_levels_grade_level_id` FOREIGN KEY (`grade_level_id`) REFERENCES `grade_levels` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_curriculum_subjects_subjects_subject_id` FOREIGN KEY (`subject_id`) REFERENCES `subjects` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `sections` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `school_year_id` bigint NOT NULL,
    `grade_level_id` bigint NOT NULL,
    `name` longtext NOT NULL,
    `capacity` int NULL,
    `adviser_teacher_id` bigint NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_sections_grade_levels_grade_level_id` FOREIGN KEY (`grade_level_id`) REFERENCES `grade_levels` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_sections_school_years_school_year_id` FOREIGN KEY (`school_year_id`) REFERENCES `school_years` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_sections_teachers_adviser_teacher_id` FOREIGN KEY (`adviser_teacher_id`) REFERENCES `teachers` (`Id`)
);

CREATE TABLE `class_offerings` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `school_year_id` bigint NOT NULL,
    `section_id` bigint NOT NULL,
    `subject_id` bigint NOT NULL,
    `teacher_id` bigint NULL,
    `curriculum_id` bigint NULL,
    `status` longtext NOT NULL,
    `room` longtext NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_class_offerings_curricula_curriculum_id` FOREIGN KEY (`curriculum_id`) REFERENCES `curricula` (`Id`),
    CONSTRAINT `FK_class_offerings_school_years_school_year_id` FOREIGN KEY (`school_year_id`) REFERENCES `school_years` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_class_offerings_sections_section_id` FOREIGN KEY (`section_id`) REFERENCES `sections` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_class_offerings_subjects_subject_id` FOREIGN KEY (`subject_id`) REFERENCES `subjects` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_class_offerings_teachers_teacher_id` FOREIGN KEY (`teacher_id`) REFERENCES `teachers` (`Id`)
);

CREATE TABLE `enrollments` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `school_year_id` bigint NOT NULL,
    `student_id` bigint NOT NULL,
    `grade_level_id` bigint NOT NULL,
    `section_id` bigint NOT NULL,
    `curriculum_id` bigint NOT NULL,
    `status` longtext NOT NULL,
    `enrolled_at` datetime(6) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_enrollments_curricula_curriculum_id` FOREIGN KEY (`curriculum_id`) REFERENCES `curricula` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_enrollments_grade_levels_grade_level_id` FOREIGN KEY (`grade_level_id`) REFERENCES `grade_levels` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_enrollments_school_years_school_year_id` FOREIGN KEY (`school_year_id`) REFERENCES `school_years` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_enrollments_sections_section_id` FOREIGN KEY (`section_id`) REFERENCES `sections` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_enrollments_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `students` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `announcements` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `posted_by_user_id` bigint NOT NULL,
    `title` longtext NOT NULL,
    `body` longtext NOT NULL,
    `audience_type` longtext NOT NULL,
    `section_id` bigint NULL,
    `class_offering_id` bigint NULL,
    `posted_at` datetime(6) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_announcements_class_offerings_class_offering_id` FOREIGN KEY (`class_offering_id`) REFERENCES `class_offerings` (`Id`),
    CONSTRAINT `FK_announcements_sections_section_id` FOREIGN KEY (`section_id`) REFERENCES `sections` (`Id`),
    CONSTRAINT `FK_announcements_users_posted_by_user_id` FOREIGN KEY (`posted_by_user_id`) REFERENCES `users` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `assessments` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `class_offering_id` bigint NOT NULL,
    `grading_period_id` bigint NOT NULL,
    `component_id` bigint NOT NULL,
    `title` longtext NOT NULL,
    `max_score` decimal(8,2) NOT NULL,
    `date_given` datetime(6) NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_assessments_class_offerings_class_offering_id` FOREIGN KEY (`class_offering_id`) REFERENCES `class_offerings` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_assessments_grade_components_component_id` FOREIGN KEY (`component_id`) REFERENCES `grade_components` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_assessments_grading_periods_grading_period_id` FOREIGN KEY (`grading_period_id`) REFERENCES `grading_periods` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `attendance_sessions` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `class_offering_id` bigint NOT NULL,
    `session_date` datetime(6) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_attendance_sessions_class_offerings_class_offering_id` FOREIGN KEY (`class_offering_id`) REFERENCES `class_offerings` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `class_schedules` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `class_offering_id` bigint NOT NULL,
    `room_id` bigint NULL,
    `time_slot_id` bigint NULL,
    `day_of_week` tinyint unsigned NOT NULL,
    `start_time` time(6) NOT NULL,
    `end_time` time(6) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_class_schedules_class_offerings_class_offering_id` FOREIGN KEY (`class_offering_id`) REFERENCES `class_offerings` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_class_schedules_rooms_room_id` FOREIGN KEY (`room_id`) REFERENCES `rooms` (`Id`),
    CONSTRAINT `FK_class_schedules_time_slots_time_slot_id` FOREIGN KEY (`time_slot_id`) REFERENCES `time_slots` (`Id`)
);

CREATE TABLE `student_grades` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `class_offering_id` bigint NOT NULL,
    `grading_period_id` bigint NOT NULL,
    `student_id` bigint NOT NULL,
    `written_works` decimal(5,2) NULL,
    `performance_tasks` decimal(5,2) NULL,
    `quarterly_assessment` decimal(5,2) NULL,
    `quarter_grade` decimal(5,2) NULL,
    `submitted_at` datetime(6) NULL,
    `locked_at` datetime(6) NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_student_grades_class_offerings_class_offering_id` FOREIGN KEY (`class_offering_id`) REFERENCES `class_offerings` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_student_grades_grading_periods_grading_period_id` FOREIGN KEY (`grading_period_id`) REFERENCES `grading_periods` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_student_grades_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `students` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `class_students` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `class_offering_id` bigint NOT NULL,
    `student_id` bigint NOT NULL,
    `enrollment_id` bigint NOT NULL,
    `status` longtext NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_class_students_class_offerings_class_offering_id` FOREIGN KEY (`class_offering_id`) REFERENCES `class_offerings` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_class_students_enrollments_enrollment_id` FOREIGN KEY (`enrollment_id`) REFERENCES `enrollments` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_class_students_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `students` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `assessment_scores` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `assessment_id` bigint NOT NULL,
    `student_id` bigint NOT NULL,
    `score` decimal(8,2) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_assessment_scores_assessments_assessment_id` FOREIGN KEY (`assessment_id`) REFERENCES `assessments` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_assessment_scores_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `students` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `attendance_records` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `attendance_session_id` bigint NOT NULL,
    `student_id` bigint NOT NULL,
    `marked_by_user_id` bigint NULL,
    `status` longtext NOT NULL,
    `reason` longtext NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_attendance_records_attendance_sessions_attendance_session_id` FOREIGN KEY (`attendance_session_id`) REFERENCES `attendance_sessions` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_attendance_records_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `students` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_attendance_records_users_marked_by_user_id` FOREIGN KEY (`marked_by_user_id`) REFERENCES `users` (`Id`)
);

CREATE INDEX `IX_announcements_class_offering_id` ON `announcements` (`class_offering_id`);

CREATE INDEX `IX_announcements_posted_by_user_id` ON `announcements` (`posted_by_user_id`);

CREATE INDEX `IX_announcements_section_id` ON `announcements` (`section_id`);

CREATE INDEX `IX_assessment_scores_assessment_id` ON `assessment_scores` (`assessment_id`);

CREATE INDEX `IX_assessment_scores_student_id` ON `assessment_scores` (`student_id`);

CREATE INDEX `IX_assessments_class_offering_id` ON `assessments` (`class_offering_id`);

CREATE INDEX `IX_assessments_component_id` ON `assessments` (`component_id`);

CREATE INDEX `IX_assessments_grading_period_id` ON `assessments` (`grading_period_id`);

CREATE INDEX `IX_attendance_records_attendance_session_id` ON `attendance_records` (`attendance_session_id`);

CREATE INDEX `IX_attendance_records_marked_by_user_id` ON `attendance_records` (`marked_by_user_id`);

CREATE INDEX `IX_attendance_records_student_id` ON `attendance_records` (`student_id`);

CREATE INDEX `IX_attendance_sessions_class_offering_id` ON `attendance_sessions` (`class_offering_id`);

CREATE INDEX `IX_audit_logs_user_id` ON `audit_logs` (`user_id`);

CREATE INDEX `IX_class_offerings_curriculum_id` ON `class_offerings` (`curriculum_id`);

CREATE INDEX `IX_class_offerings_school_year_id` ON `class_offerings` (`school_year_id`);

CREATE INDEX `IX_class_offerings_section_id` ON `class_offerings` (`section_id`);

CREATE INDEX `IX_class_offerings_subject_id` ON `class_offerings` (`subject_id`);

CREATE INDEX `IX_class_offerings_teacher_id` ON `class_offerings` (`teacher_id`);

CREATE INDEX `IX_class_schedules_class_offering_id` ON `class_schedules` (`class_offering_id`);

CREATE INDEX `IX_class_schedules_room_id` ON `class_schedules` (`room_id`);

CREATE INDEX `IX_class_schedules_time_slot_id` ON `class_schedules` (`time_slot_id`);

CREATE INDEX `IX_class_students_class_offering_id` ON `class_students` (`class_offering_id`);

CREATE INDEX `IX_class_students_enrollment_id` ON `class_students` (`enrollment_id`);

CREATE INDEX `IX_class_students_student_id` ON `class_students` (`student_id`);

CREATE INDEX `IX_curriculum_subjects_curriculum_id` ON `curriculum_subjects` (`curriculum_id`);

CREATE INDEX `IX_curriculum_subjects_grade_level_id` ON `curriculum_subjects` (`grade_level_id`);

CREATE INDEX `IX_curriculum_subjects_subject_id` ON `curriculum_subjects` (`subject_id`);

CREATE INDEX `IX_enrollments_curriculum_id` ON `enrollments` (`curriculum_id`);

CREATE INDEX `IX_enrollments_grade_level_id` ON `enrollments` (`grade_level_id`);

CREATE INDEX `IX_enrollments_school_year_id` ON `enrollments` (`school_year_id`);

CREATE INDEX `IX_enrollments_section_id` ON `enrollments` (`section_id`);

CREATE INDEX `IX_enrollments_student_id` ON `enrollments` (`student_id`);

CREATE INDEX `IX_grading_periods_school_year_id` ON `grading_periods` (`school_year_id`);

CREATE INDEX `IX_sections_adviser_teacher_id` ON `sections` (`adviser_teacher_id`);

CREATE INDEX `IX_sections_grade_level_id` ON `sections` (`grade_level_id`);

CREATE INDEX `IX_sections_school_year_id` ON `sections` (`school_year_id`);

CREATE INDEX `IX_student_grades_class_offering_id` ON `student_grades` (`class_offering_id`);

CREATE INDEX `IX_student_grades_grading_period_id` ON `student_grades` (`grading_period_id`);

CREATE INDEX `IX_student_grades_student_id` ON `student_grades` (`student_id`);

CREATE UNIQUE INDEX `IX_students_user_id` ON `students` (`user_id`);

CREATE INDEX `IX_subjects_grade_level_id` ON `subjects` (`grade_level_id`);

CREATE UNIQUE INDEX `IX_teachers_user_id` ON `teachers` (`user_id`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260305154000_20260305_ModelBaseline', '8.0.8');

COMMIT;

START TRANSACTION;

CREATE TABLE `archive_records` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `entity_type` longtext NOT NULL,
    `original_entity_id` bigint NULL,
    `payload` longtext NOT NULL,
    `deleted_by_user_id` bigint NULL,
    `deleted_at` datetime(6) NOT NULL,
    `is_restored` tinyint(1) NOT NULL,
    `restored_by_user_id` bigint NULL,
    `restored_at` datetime(6) NULL,
    `notes` longtext NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_archive_records_users_deleted_by_user_id` FOREIGN KEY (`deleted_by_user_id`) REFERENCES `users` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_archive_records_users_restored_by_user_id` FOREIGN KEY (`restored_by_user_id`) REFERENCES `users` (`Id`) ON DELETE SET NULL
);

CREATE TABLE `school_settings` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `school_name` longtext NOT NULL,
    `school_code` longtext NOT NULL,
    `school_address` longtext NOT NULL,
    `principal_name` longtext NOT NULL,
    `grading_setup` longtext NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE `student_requirements` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `student_id` bigint NOT NULL,
    `requirement_name` longtext NOT NULL,
    `is_submitted` tinyint(1) NOT NULL,
    `submitted_at` datetime(6) NULL,
    `notes` longtext NULL,
    `verified_by_user_id` bigint NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_student_requirements_students_student_id` FOREIGN KEY (`student_id`) REFERENCES `students` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_student_requirements_users_verified_by_user_id` FOREIGN KEY (`verified_by_user_id`) REFERENCES `users` (`Id`) ON DELETE SET NULL
);

CREATE INDEX `IX_archive_records_deleted_by_user_id` ON `archive_records` (`deleted_by_user_id`);

CREATE INDEX `IX_archive_records_restored_by_user_id` ON `archive_records` (`restored_by_user_id`);

CREATE INDEX `IX_student_requirements_student_id` ON `student_requirements` (`student_id`);

CREATE INDEX `IX_student_requirements_verified_by_user_id` ON `student_requirements` (`verified_by_user_id`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260307072736_20260307_WorkflowExtensions', '8.0.8');

COMMIT;

