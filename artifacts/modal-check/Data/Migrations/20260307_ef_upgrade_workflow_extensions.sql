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

