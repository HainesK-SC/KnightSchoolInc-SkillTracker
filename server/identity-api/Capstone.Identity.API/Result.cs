namespace Capstone.Identity.API
{
    // provides an error code if the request fails - provides more insight on failure
    public enum ResultErrorType
    {
        None,
        Validation,     // 400 
        Unauthorized,   // 401
        NotFound,       // 404
        Conflict        // 409
    }

    public class Result<T>
    {
        public bool Succeeded { get; set; }
        public T? Data { get; set; }
        public string? Error { get; set; }
        public ResultErrorType ErrorType { get; private set; }

        public static Result<T> Success(T data) =>
            new() { Succeeded = true, Data = data };

        public static Result<T> Failure(string error, ResultErrorType errorType) =>
            new() { Succeeded = false, Error = error, ErrorType = errorType };
    }
}
