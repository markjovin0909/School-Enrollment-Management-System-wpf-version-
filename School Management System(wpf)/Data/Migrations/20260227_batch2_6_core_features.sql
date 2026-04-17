-- Batches 2-6 core schema updates (legacy)
-- Run against the currently selected database/connection.

CREATE TABLE IF NOT EXISTS rooms (
  id BIGINT AUTO_INCREMENT PRIMARY KEY,
  code VARCHAR(50) NOT NULL,
  name VARCHAR(120) NOT NULL,
  capacity INT NULL,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  created_at DATETIME NOT NULL,
  updated_at DATETIME NOT NULL,
  UNIQUE KEY uq_rooms_code (code)
);

CREATE TABLE IF NOT EXISTS time_slots (
  id BIGINT AUTO_INCREMENT PRIMARY KEY,
  code VARCHAR(50) NOT NULL,
  name VARCHAR(120) NOT NULL,
  start_time TIME NOT NULL,
  end_time TIME NOT NULL,
  is_bell_period TINYINT(1) NOT NULL DEFAULT 1,
  sort_order INT NOT NULL DEFAULT 0,
  created_at DATETIME NOT NULL,
  updated_at DATETIME NOT NULL,
  UNIQUE KEY uq_time_slots_code (code),
  KEY idx_time_slots_sort (sort_order, start_time)
);

ALTER TABLE class_schedules
  ADD COLUMN IF NOT EXISTS room_id BIGINT NULL,
  ADD COLUMN IF NOT EXISTS time_slot_id BIGINT NULL;

ALTER TABLE attendance_records
  ADD COLUMN IF NOT EXISTS marked_by_user_id BIGINT NULL,
  ADD COLUMN IF NOT EXISTS reason VARCHAR(255) NULL;

CREATE TABLE IF NOT EXISTS announcements (
  id BIGINT AUTO_INCREMENT PRIMARY KEY,
  posted_by_user_id BIGINT NOT NULL,
  title VARCHAR(180) NOT NULL,
  body TEXT NOT NULL,
  audience_type VARCHAR(20) NOT NULL,
  section_id BIGINT NULL,
  class_offering_id BIGINT NULL,
  posted_at DATETIME NOT NULL,
  created_at DATETIME NOT NULL,
  updated_at DATETIME NOT NULL,
  KEY idx_announcements_audience (audience_type, section_id, class_offering_id),
  KEY idx_announcements_posted_at (posted_at)
);

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

CALL ensure_index('class_schedules', 'idx_class_schedules_room_day_time', 0, '`room_id`, `day_of_week`, `start_time`, `end_time`');
CALL ensure_index('class_schedules', 'idx_class_schedules_offering_day_time', 0, '`class_offering_id`, `day_of_week`, `start_time`, `end_time`');
CALL ensure_index('class_schedules', 'idx_class_schedules_slot', 0, '`time_slot_id`');
CALL ensure_index('attendance_records', 'idx_attendance_records_session_student', 0, '`attendance_session_id`, `student_id`');
CALL ensure_index('attendance_records', 'idx_attendance_records_student_status', 0, '`student_id`, `status`');
CALL ensure_index('attendance_sessions', 'idx_attendance_sessions_offering_date', 0, '`class_offering_id`, `session_date`');
CALL ensure_index('enrollments', 'idx_enrollments_schoolyear_section_status', 0, '`school_year_id`, `section_id`, `status`');
CALL ensure_index('student_grades', 'idx_student_grades_offering_period_student', 0, '`class_offering_id`, `grading_period_id`, `student_id`');
CALL ensure_index('class_students', 'idx_class_students_offering_status', 0, '`class_offering_id`, `status`');

DROP PROCEDURE IF EXISTS `ensure_index`;

-- Backup/restore operational guidance (run from shell, not SQL):
-- Backup: mysqldump -u root -proot school_sms > school_sms_backup.sql
-- Restore: mysql -u root -proot school_sms < school_sms_backup.sql
