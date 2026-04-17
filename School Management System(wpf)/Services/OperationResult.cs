using System.Collections.Generic;

namespace School_Management_System.Services
{
    internal class OperationResult
    {
        public bool Success { get; protected set; }
        public string Message { get; protected set; } = string.Empty;
        public List<string> Errors { get; } = new();

        public static OperationResult Ok(string message = "")
        {
            return new OperationResult { Success = true, Message = message };
        }

        public static OperationResult Fail(string message, IEnumerable<string>? errors = null)
        {
            var result = new OperationResult { Success = false, Message = message };
            if (errors != null)
            {
                result.Errors.AddRange(errors);
            }
            return result;
        }
    }

    internal class OperationResult<T> : OperationResult
    {
        public T? Data { get; private set; }

        public static OperationResult<T> Ok(T data, string message = "")
        {
            return new OperationResult<T> { Data = data, Message = message, Success = true };
        }

        public new static OperationResult<T> Fail(string message, IEnumerable<string>? errors = null)
        {
            var result = new OperationResult<T> { Message = message, Success = false };
            if (errors != null)
            {
                result.Errors.AddRange(errors);
            }
            return result;
        }
    }
}
