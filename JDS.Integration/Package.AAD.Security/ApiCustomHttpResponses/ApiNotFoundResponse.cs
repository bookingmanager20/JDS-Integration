using System.Collections.Generic;

namespace Api.Safe.Well.ApiCustomHttpResponses
{
    public class ApiNotFoundResponse : ApiResponse
    {
        public IEnumerable<string> Errors { get; }

        public ApiNotFoundResponse()
            : base(404)
        {
            var errors = new List<string> { "Resource not found" };
            Errors = errors;
        }
    }
}
