-- Admin/workflow extensions migration (idempotent)
-- Target: existing schema selected by current connection.
-- Adds admin workflow indexes and ensures extension tables/default data exist.

CREATE TABLE IF NOT EXISTS `school_settings` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `school_name` longtext NOT NULL,
    `school_code` longtext NOT NULL,
    `school_address` longtext NOT NULL,
    `principal_name` longtext NOT NULL,
    `grading_setup` longtext NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `student_requirements` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `student_id` bigint NOT NULL,
    `requirement_name` longtext NOT NULL,
    `is_submitted` tinyint(1) NOT NULL DEFAULT 0,
    `submitted_at` datetime(6) NULL,
    `notes` longtext NULL,
    `verified_by_user_id` bigint NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_student_requirements_students_student_id`
        FOREIGN KEY (`student_id`) REFERENCES `students` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_student_requirements_users_verified_by_user_id`
        FOREIGN KEY (`verified_by_user_id`) REFERENCES `users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `archive_records` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `entity_type` longtext NOT NULL,
    `original_entity_id` bigint NULL,
    `payload` longtext NOT NULL,
    `deleted_by_user_id` bigint NULL,
    `deleted_at` datetime(6) NOT NULL,
    `is_restored` tinyint(1) NOT NULL DEFAULT 0,
    `restored_by_user_id` bigint NULL,
    `restored_at` datetime(6) NULL,
    `notes` longtext NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_archive_records_users_deleted_by_user_id`
        FOREIGN KEY (`deleted_by_user_id`) REFERENCES `users` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_archive_records_users_restored_by_user_id`
        FOREIGN KEY (`restored_by_user_id`) REFERENCES `users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

DROP PROCEDURE IF EXISTS `ensure_index`;
DELIMITER //
CREATE PROCEDURE `ensure_index`(
    IN p_table_name VARCHAR(128),
    IN p_index_name VARCHAR(128),
    IN p_unique TINYINT,
    IN p_columns TEXT
)
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.statistics
        WHERE table_schema = DATABASE()
          AND table_name = p_table_name
          AND index_name = p_index_name
    ) THEN
        SET @ddl = CONCAT(
            'CREATE ',
            IF(p_unique = 1, 'UNIQUE ', ''),
            'INDEX `', p_index_name, '` ON `', p_table_name, '` (', p_columns, ')'
        );
        PREPARE stmt FROM @ddl;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END //
DELIMITER ;

-- Lookup/performance indexes
CALL ensure_index('users', 'idx_users_username', 0, '`username`(191)');
CALL ensure_index('students', 'idx_students_lrn', 0, '`lrn`(191)');
CALL ensure_index('teachers', 'idx_teachers_employee_no', 0, '`employee_no`(191)');
CALL ensure_index('announcements', 'idx_announcements_audience_posted', 0, '`audience_type`(191), `posted_at`');
CALL ensure_index('archive_records', 'idx_archive_records_entity_deleted', 0, '`entity_type`(191), `deleted_at`');
CALL ensure_index('archive_records', 'idx_archive_records_is_restored', 0, '`is_restored`, `deleted_at`');

-- Workflow integrity indexes
CALL ensure_index('enrollments', 'uq_enrollments_school_year_student', 1, '`school_year_id`, `student_id`');
CALL ensure_index('class_students', 'uq_class_students_offering_student', 1, '`class_offering_id`, `student_id`');
CALL ensure_index('student_grades', 'uq_student_grades_unique', 1, '`class_offering_id`, `grading_period_id`, `student_id`');
CALL ensure_index('attendance_sessions', 'uq_attendance_sessions_offering_date', 1, '`class_offering_id`, `session_date`');
CALL ensure_index('attendance_records', 'uq_attendance_records_session_student', 1, '`attendance_session_id`, `student_id`');
CALL ensure_index('student_requirements', 'uq_student_requirements_student_name', 1, '`student_id`, `requirement_name`(191)');

DROP PROCEDURE IF EXISTS `ensure_index`;

INSERT INTO `school_settings` (`school_name`, `school_code`, `school_address`, `principal_name`, `grading_setup`, `created_at`, `updated_at`)
SELECT
    'School Management System',
    'SMS-001',
    '',
    '',
    'K-12 quarter system (WW 30%, PT 50%, QA 20%)',
    UTC_TIMESTAMP(6),
    UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `school_settings`);
