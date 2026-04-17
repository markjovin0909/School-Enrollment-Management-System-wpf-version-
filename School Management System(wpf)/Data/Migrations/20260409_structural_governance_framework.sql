-- Structural governance framework (SR-01..SR-06)
-- Safe to run multiple times.

CREATE TABLE IF NOT EXISTS `enrollment_state_transitions` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `enrollment_id` BIGINT NOT NULL,
    `previous_status` VARCHAR(64) NULL,
    `new_status` VARCHAR(64) NOT NULL,
    `previous_approval_status` VARCHAR(64) NULL,
    `new_approval_status` VARCHAR(64) NULL,
    `trigger_action` VARCHAR(64) NOT NULL,
    `reason_code` VARCHAR(128) NULL,
    `reason_text` VARCHAR(1024) NULL,
    `performed_by_user_id` BIGINT NULL,
    `correlation_id` VARCHAR(64) NULL,
    `metadata_json` LONGTEXT NULL,
    `created_at` DATETIME(6) NOT NULL,
    PRIMARY KEY (`id`),
    KEY `ix_enrollment_state_transitions_enrollment_created` (`enrollment_id`, `created_at`),
    KEY `ix_enrollment_state_transitions_correlation` (`correlation_id`)
);

CREATE TABLE IF NOT EXISTS `governed_operation_logs` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `correlation_id` VARCHAR(64) NOT NULL,
    `policy_key` VARCHAR(128) NOT NULL,
    `action` VARCHAR(128) NOT NULL,
    `entity` VARCHAR(128) NOT NULL,
    `entity_id` BIGINT NULL,
    `status` VARCHAR(32) NOT NULL,
    `message` VARCHAR(1024) NOT NULL,
    `payload` LONGTEXT NULL,
    `actor_user_id` BIGINT NULL,
    `created_at` DATETIME(6) NOT NULL,
    PRIMARY KEY (`id`),
    KEY `ix_governed_operation_logs_correlation` (`correlation_id`),
    KEY `ix_governed_operation_logs_created` (`created_at`)
);

CREATE TABLE IF NOT EXISTS `exception_queue_items` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `category` VARCHAR(128) NOT NULL,
    `source_module` VARCHAR(256) NOT NULL,
    `entity` VARCHAR(128) NOT NULL,
    `entity_id` BIGINT NULL,
    `severity` VARCHAR(32) NOT NULL,
    `status` VARCHAR(32) NOT NULL,
    `summary` VARCHAR(1024) NOT NULL,
    `details` LONGTEXT NULL,
    `correlation_id` VARCHAR(64) NULL,
    `assignment_status` VARCHAR(64) NOT NULL,
    `assigned_to_user_id` BIGINT NULL,
    `acknowledged_by_user_id` BIGINT NULL,
    `acknowledged_at` DATETIME(6) NULL,
    `resolved_by_user_id` BIGINT NULL,
    `resolved_at` DATETIME(6) NULL,
    `occurrence_count` INT NOT NULL,
    `last_occurred_at` DATETIME(6) NOT NULL,
    `created_by_user_id` BIGINT NULL,
    `created_at` DATETIME(6) NOT NULL,
    `updated_at` DATETIME(6) NOT NULL,
    PRIMARY KEY (`id`),
    KEY `ix_exception_queue_items_status` (`status`, `severity`),
    KEY `ix_exception_queue_items_correlation` (`correlation_id`),
    KEY `ix_exception_queue_items_updated` (`updated_at`)
);
