using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;

namespace School_Management_System.Services
{
    internal sealed class GovernanceReadinessService
    {
        private static readonly string[] RequiredStructuralTables =
        {
            "enrollment_state_transitions",
            "governed_operation_logs",
            "exception_queue_items"
        };

        public GovernanceReadinessReport Evaluate()
        {
            var checks = new List<GovernanceReadinessCheck>();

            try
            {
                using var db = new AppDbContext();

                var canConnect = db.Database.CanConnect();
                checks.Add(canConnect
                    ? GovernanceReadinessCheck.Pass("Database connectivity is healthy.")
                    : GovernanceReadinessCheck.Fail("Database connectivity failed."));
                if (!canConnect)
                {
                    return GovernanceReadinessReport.FromChecks(checks);
                }

                StructuralSchemaService.EnsureApplied(db);
                checks.Add(GovernanceReadinessCheck.Pass("Structural schema bootstrap applied."));

                if (db.Database.IsRelational())
                {
                    var missing = FindMissingStructuralTables(db);
                    if (missing.Count == 0)
                    {
                        checks.Add(GovernanceReadinessCheck.Pass("Required structural tables are present."));
                    }
                    else
                    {
                        checks.Add(GovernanceReadinessCheck.Fail(
                            $"Missing structural tables: {string.Join(", ", missing)}."));
                    }
                }
                else
                {
                    checks.Add(GovernanceReadinessCheck.Warn("Relational table verification skipped for non-relational provider."));
                }

                var policyCount = Enum.GetValues<PolicyActionKey>().Length;
                checks.Add(policyCount > 0
                    ? GovernanceReadinessCheck.Pass($"Permission policy keys available: {policyCount}.")
                    : GovernanceReadinessCheck.Fail("No permission policy keys were discovered."));

                var preflight = new PreflightPipelineService().Evaluate("GovernanceReadiness", new Func<PreflightCheckResult>[]
                {
                    () => PreflightCheckResult.Pass("PREFLIGHT_ENGINE", "Shared preflight pipeline is operational.")
                });
                checks.Add(preflight.Success
                    ? GovernanceReadinessCheck.Pass("Shared preflight pipeline check passed.")
                    : GovernanceReadinessCheck.Fail("Shared preflight pipeline check failed."));
            }
            catch (Exception ex)
            {
                checks.Add(GovernanceReadinessCheck.Fail($"Governance readiness check failed: {ex.Message}"));
            }

            return GovernanceReadinessReport.FromChecks(checks);
        }

        private static List<string> FindMissingStructuralTables(AppDbContext db)
        {
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var databaseName = db.Database.GetDbConnection().Database;

            using var connection = db.Database.GetDbConnection();
            EnsureOpen(connection);
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT table_name
FROM information_schema.tables
WHERE table_schema = @db
  AND table_name IN ('enrollment_state_transitions','governed_operation_logs','exception_queue_items');";
            var dbParameter = command.CreateParameter();
            dbParameter.ParameterName = "@db";
            dbParameter.Value = databaseName;
            command.Parameters.Add(dbParameter);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (!reader.IsDBNull(0))
                {
                    found.Add(reader.GetString(0));
                }
            }

            return RequiredStructuralTables
                .Where(x => !found.Contains(x))
                .ToList();
        }

        private static void EnsureOpen(DbConnection connection)
        {
            if (connection.State == System.Data.ConnectionState.Open)
            {
                return;
            }

            connection.Open();
        }
    }

    internal sealed class GovernanceReadinessReport
    {
        public bool Success { get; private set; }
        public IReadOnlyList<GovernanceReadinessCheck> Checks { get; private set; } = Array.Empty<GovernanceReadinessCheck>();

        public string ToDisplayText()
        {
            return string.Join(
                Environment.NewLine,
                Checks.Select(x => $"- [{x.Severity}] {x.Message}"));
        }

        public static GovernanceReadinessReport FromChecks(IEnumerable<GovernanceReadinessCheck> checks)
        {
            var list = (checks ?? Array.Empty<GovernanceReadinessCheck>()).ToList();
            return new GovernanceReadinessReport
            {
                Success = list.All(x => x.Severity != "FAIL"),
                Checks = list
            };
        }
    }

    internal sealed class GovernanceReadinessCheck
    {
        public string Severity { get; private set; } = "INFO";
        public string Message { get; private set; } = string.Empty;

        public static GovernanceReadinessCheck Pass(string message)
        {
            return new GovernanceReadinessCheck { Severity = "PASS", Message = message };
        }

        public static GovernanceReadinessCheck Warn(string message)
        {
            return new GovernanceReadinessCheck { Severity = "WARN", Message = message };
        }

        public static GovernanceReadinessCheck Fail(string message)
        {
            return new GovernanceReadinessCheck { Severity = "FAIL", Message = message };
        }
    }
}
