using System.Collections.Generic;

namespace Package.AAD.Security.ApiCustomHttpResponses
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
