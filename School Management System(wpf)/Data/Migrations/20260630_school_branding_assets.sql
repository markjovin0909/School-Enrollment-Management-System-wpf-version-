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

CALL ensure_column('school_settings', 'school_logo_file_key', 'longtext NULL');

DROP PROCEDURE IF EXISTS `ensure_column`;
