-- Gap closure patch: school year governance, enrollment workflow, duplicates, settings defaults
-- Safe to run multiple times on MySQL 8+.

DROP PROCEDURE IF EXISTS `ensure_column`;
DELIMITER //
CREATE PROCEDURE `ensure_column`(
    IN p_table_name VARCHAR(128),
    IN p_column_name VARCHAR(128),
    IN p_definition TEXT
)
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = DATABASE()
          AND table_name = p_table_name
          AND column_name = p_column_name
    ) THEN
        SET @ddl = CONCAT(
            'ALTER TABLE `', p_table_name, '` ADD COLUMN `', p_column_name, '` ', p_definition
        );
        PREPARE stmt FROM @ddl;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END //
DELIMITER ;

CALL ensure_column('school_settings', 'default_grade_level_ids', 'longtext NULL');
CALL ensure_column('school_settings', 'enrollment_configuration', 'longtext NULL');
CALL ensure_column('school_settings', 'enrollment_open_date', 'datetime(6) NULL');
CALL ensure_column('school_settings', 'enrollment_close_date', 'datetime(6) NULL');
CALL ensure_column('school_settings', 'print_header_line1', 'longtext NULL');
CALL ensure_column('school_settings', 'print_header_line2', 'longtext NULL');
CALL ensure_column('school_settings', 'student_number_prefix', 'longtext NULL');
CALL ensure_column('school_settings', 'next_student_number', 'int NOT NULL DEFAULT 1');
CALL ensure_column('school_settings', 'default_section_capacity', 'int NOT NULL DEFAULT 45');
CALL ensure_column('school_years', 'enrollment_open_date', 'datetime(6) NULL');
CALL ensure_column('school_years', 'enrollment_close_date', 'datetime(6) NULL');
CALL ensure_column('school_years', 'is_archived', 'tinyint(1) NOT NULL DEFAULT 0');
CALL ensure_column('sections', 'is_archived', 'tinyint(1) NOT NULL DEFAULT 0');
CALL ensure_column('users', 'can_login', 'tinyint(1) NOT NULL DEFAULT 1');
CALL ensure_column('students', 'student_number', 'longtext NOT NULL');
CALL ensure_column('students', 'age', 'int NULL');
CALL ensure_column('students', 'contact_no', 'longtext NULL');
CALL ensure_column('students', 'previous_school', 'longtext NULL');
CALL ensure_column('students', 'preferred_grade_level_id', 'bigint NULL');
CALL ensure_column('students', 'preferred_curriculum_id', 'bigint NULL');
CALL ensure_column('teachers', 'specialization', 'longtext NULL');
CALL ensure_column('teachers', 'advisory_assignment_status', 'longtext NULL');
CALL ensure_column('teachers', 'employment_status', 'longtext NULL');
CALL ensure_column('enrollments', 'enrollment_type', 'varchar(32) NOT NULL DEFAULT ''NEW''');
CALL ensure_column('enrollments', 'notes', 'longtext NULL');
CALL ensure_column('enrollments', 'approval_status', 'varchar(32) NOT NULL DEFAULT ''PENDING''');
CALL ensure_column('enrollments', 'waitlist_position', 'int NULL');
CALL ensure_column('enrollments', 'approved_by_user_id', 'bigint NULL');
CALL ensure_column('enrollments', 'approved_at', 'datetime(6) NULL');

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

CALL ensure_index('sections', 'uq_sections_school_year_grade_name', 1, '`school_year_id`, `grade_level_id`, `name`(191)');
CALL ensure_index('subjects', 'uq_subjects_code', 1, '`code`(191)');
CALL ensure_index('users', 'uq_users_username', 1, '`username`(191)');
CALL ensure_index('students', 'uq_students_lrn', 1, '`lrn`(191)');
CALL ensure_index('students', 'uq_students_student_number', 1, '`student_number`(191)');
CALL ensure_index('teachers', 'uq_teachers_employee_no', 1, '`employee_no`(191)');
CALL ensure_index('curriculum_subjects', 'uq_curriculum_subjects_unique', 1, '`curriculum_id`, `grade_level_id`, `subject_id`');
CALL ensure_index('class_offerings', 'uq_class_offerings_unique', 1, '`school_year_id`, `section_id`, `subject_id`');
CALL ensure_index('enrollments', 'idx_enrollments_waitlist_queue', 0, '`school_year_id`, `section_id`, `status`(32), `waitlist_position`');
CALL ensure_index('enrollments', 'idx_enrollments_approval_status', 0, '`approval_status`, `status`(32)');
CALL ensure_index('school_years', 'idx_school_years_archive_status', 0, '`is_archived`, `status`(32)');
CALL ensure_index('sections', 'idx_sections_archived_school_year', 0, '`is_archived`, `school_year_id`, `grade_level_id`');

DROP PROCEDURE IF EXISTS `ensure_column`;
DROP PROCEDURE IF EXISTS `ensure_index`;
