using System;
using System.Configuration;

namespace School_Management_System.Configuration
{
    /// <summary>
    /// Manages database configuration and connection string selection.
    /// Supports switching between Local, Remote, and Online environments.
    /// 
    /// Usage:
    ///     var connectionString = DatabaseConfig.GetConnectionString();
    ///     var environment = DatabaseConfig.ActiveEnvironment;
    ///     var (isValid, message) = DatabaseConfig.ValidateConfiguration();
    /// </summary>
    public static class DatabaseConfig
    {
        /// <summary>
        /// Supported database environments.
        /// </summary>
        public enum Environment
        {
            /// <summary>Local development database (localhost)</summary>
            Local,

            /// <summary>Remote database on private network</summary>
            Remote,

            /// <summary>Online cloud database</summary>
            Online
        }

        /// <summary>
        /// Gets the currently active environment from App.config.
        /// </summary>
        /// <exception cref="ConfigurationErrorsException">Thrown if environment setting is missing or invalid.</exception>
        public static Environment ActiveEnvironment
        {
            get
            {
                var envSetting = ConfigurationManager.AppSettings["ActiveEnvironment"];

                if (string.IsNullOrWhiteSpace(envSetting))
                {
                    throw new ConfigurationErrorsException(
                        "ActiveEnvironment setting not found in App.config. " +
                        "Please add: <add key=\"ActiveEnvironment\" value=\"Local|Remote|Online\" />");
                }

                if (!Enum.TryParse<Environment>(envSetting, ignoreCase: true, out var result))
                {
                    throw new ConfigurationErrorsException(
                        $"Invalid ActiveEnvironment value: '{envSetting}'. " +
                        $"Allowed values are: Local, Remote, Online");
                }

                return result;
            }
        }

        /// <summary>
        /// Gets the connection string for the active environment.
        /// </summary>
        /// <returns>Database connection string with all server and credential information.</returns>
        /// <exception cref="ConfigurationErrorsException">Thrown if connection string is not found or is empty.</exception>
        public static string GetConnectionString()
        {
            var connectionStringName = GetConnectionStringName(ActiveEnvironment);
            var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName];

            if (connectionString == null)
            {
                throw new ConfigurationErrorsException(
                    $"Connection string '{connectionStringName}' not found in App.config. " +
                    $"Current environment: {ActiveEnvironment}");
            }

            if (string.IsNullOrWhiteSpace(connectionString.ConnectionString))
            {
                throw new ConfigurationErrorsException(
                    $"Connection string '{connectionStringName}' is empty in App.config.");
            }

            return connectionString.ConnectionString;
        }

        /// <summary>
        /// Gets the connection string for a specific environment.
        /// </summary>
        public static string GetConnectionString(Environment environment)
        {
            var connectionStringName = GetConnectionStringName(environment);
            var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName];

            if (connectionString == null || string.IsNullOrWhiteSpace(connectionString.ConnectionString))
            {
                throw new ConfigurationErrorsException(
                    $"Connection string '{connectionStringName}' is missing or empty in App.config.");
            }

            return connectionString.ConnectionString;
        }

        /// <summary>
        /// Gets the provider name for the active environment's connection string.
        /// </summary>
        /// <returns>Provider name (e.g., "MySql.Data.MySqlClient")</returns>
        public static string GetProviderName()
        {
            var connectionStringName = GetConnectionStringName(ActiveEnvironment);
            var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName];

            return connectionString?.ProviderName ?? "MySql.Data.MySqlClient";
        }

        /// <summary>
        /// Maps environment enum to connection string name in App.config.
        /// </summary>
        /// <param name="environment">The environment to map.</param>
        /// <returns>Connection string name from App.config</returns>
        private static string GetConnectionStringName(Environment environment)
        {
            return environment switch
            {
                Environment.Local => "DbLocal",
                Environment.Remote => "DbRemote",
                Environment.Online => "DbOnline",
                _ => throw new ArgumentOutOfRangeException(nameof(environment), environment, "Unknown environment.")
            };
        }

        /// <summary>
        /// Validates the current configuration without throwing exceptions.
        /// Useful for startup diagnostics and health checks.
        /// </summary>
        /// <returns>
        /// Tuple of (IsValid, ErrorMessage).
        /// IsValid is true if configuration can be successfully loaded.
        /// </returns>
        public static (bool IsValid, string ErrorMessage) ValidateConfiguration()
        {
            try
            {
                var env = ActiveEnvironment;
                var connectionString = GetConnectionString();

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return (false, $"Connection string for {env} environment is empty.");
                }

                return (true, $"Configuration valid. Environment: {env}");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Saves the active environment and all connection strings into the application configuration.
        /// </summary>
        /// <exception cref="ConfigurationErrorsException">Thrown when save fails.</exception>
        public static void Save(Environment activeEnvironment, string dbLocal, string dbRemote, string dbOnline)
        {
            if (string.IsNullOrWhiteSpace(dbLocal) || string.IsNullOrWhiteSpace(dbRemote) || string.IsNullOrWhiteSpace(dbOnline))
            {
                throw new ConfigurationErrorsException("All connection string values are required.");
            }

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            if (config.AppSettings.Settings["ActiveEnvironment"] == null)
            {
                config.AppSettings.Settings.Add("ActiveEnvironment", activeEnvironment.ToString());
            }
            else
            {
                config.AppSettings.Settings["ActiveEnvironment"].Value = activeEnvironment.ToString();
            }

            SetConnectionString(config, "DbLocal", dbLocal, "MySql.Data.MySqlClient");
            SetConnectionString(config, "DbRemote", dbRemote, "MySql.Data.MySqlClient");
            SetConnectionString(config, "DbOnline", dbOnline, "MySql.Data.MySqlClient");

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
            ConfigurationManager.RefreshSection("connectionStrings");
        }

        private static void SetConnectionString(System.Configuration.Configuration config, string name, string value, string providerName)
        {
            var setting = config.ConnectionStrings.ConnectionStrings[name];
            if (setting == null)
            {
                config.ConnectionStrings.ConnectionStrings.Add(new ConnectionStringSettings(name, value, providerName));
                return;
            }

            setting.ConnectionString = value;
            setting.ProviderName = providerName;
        }
    }
}
