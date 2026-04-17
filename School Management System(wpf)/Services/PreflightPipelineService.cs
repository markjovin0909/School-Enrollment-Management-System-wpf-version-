using System;
using System.Collections.Generic;
using System.Linq;

namespace School_Management_System.Services
{
    internal enum PreflightCheckOutcome
    {
        PASS,
        WARNING,
        BLOCK
    }

    internal sealed class PreflightCheckResult
    {
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public PreflightCheckOutcome Outcome { get; init; } = PreflightCheckOutcome.PASS;

        public static PreflightCheckResult Pass(string code, string message)
        {
            return new PreflightCheckResult { Code = code, Message = message, Outcome = PreflightCheckOutcome.PASS };
        }

        public static PreflightCheckResult Warning(string code, string message)
        {
            return new PreflightCheckResult { Code = code, Message = message, Outcome = PreflightCheckOutcome.WARNING };
        }

        public static PreflightCheckResult Block(string code, string message)
        {
            return new PreflightCheckResult { Code = code, Message = message, Outcome = PreflightCheckOutcome.BLOCK };
        }
    }

    internal sealed class PreflightEvaluationResult
    {
        public string Operation { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<PreflightCheckResult> Checks { get; } = new();
        public List<string> BlockingReasons { get; } = new();
        public List<string> Warnings { get; } = new();
    }

    internal sealed class PreflightPipelineService
    {
        public PreflightEvaluationResult Evaluate(string operation, IEnumerable<Func<PreflightCheckResult>> checks)
        {
            var result = new PreflightEvaluationResult
            {
                Operation = string.IsNullOrWhiteSpace(operation) ? "Operation" : operation.Trim()
            };

            foreach (var check in checks ?? Enumerable.Empty<Func<PreflightCheckResult>>())
            {
                PreflightCheckResult item;
                try
                {
                    item = check();
                }
                catch (Exception ex)
                {
                    item = PreflightCheckResult.Block("CHECK_RUNTIME_ERROR", ex.Message);
                }

                result.Checks.Add(item);
                if (item.Outcome == PreflightCheckOutcome.BLOCK)
                {
                    result.BlockingReasons.Add(item.Message);
                }
                else if (item.Outcome == PreflightCheckOutcome.WARNING)
                {
                    result.Warnings.Add(item.Message);
                }
            }

            result.Success = result.BlockingReasons.Count == 0;
            result.Summary = result.Success
                ? $"{result.Operation} preflight passed ({result.Checks.Count} checks)."
                : $"{result.Operation} preflight blocked ({result.BlockingReasons.Count} blocker(s)).";

            return result;
        }
    }
}
