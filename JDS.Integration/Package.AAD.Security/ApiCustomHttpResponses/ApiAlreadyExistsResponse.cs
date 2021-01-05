using System.Collections.Generic;

namespace Package.AAD.Security.ApiCustomHttpResponses
{
    public class ApiAlreadyExistsResponse : ApiResponse
    {
        public IEnumerable<string> Errors { get; }

        public ApiAlreadyExistsResponse()
            : base(409)
        {
            var errors = new List<string> { "Resource already exists" };
            Errors = errors;
        }
    }
}
