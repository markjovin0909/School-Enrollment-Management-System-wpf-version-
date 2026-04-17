-- Batch 1: RBAC hardening support script
-- Safe/idempotent indexing and data normalization only.
-- Run against the currently selected database/connection.

-- Normalize any null/empty user statuses to ACTIVE.
UPDATE users
SET status = 'ACTIVE'
WHERE status IS NULL OR TRIM(status) = '';

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

-- Indexes that help authentication and RBAC filtering.
CALL ensure_index('users', 'idx_users_username', 0, '`username`');
CALL ensure_index('users', 'idx_users_role_status', 0, '`role`, `status`');

DROP PROCEDURE IF EXISTS `ensure_index`;
