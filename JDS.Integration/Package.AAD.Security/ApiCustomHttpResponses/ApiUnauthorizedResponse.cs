using System.Collections.Generic;

namespace Package.AAD.Security.ApiCustomHttpResponses
{
    public class ApiUnauthorizedResponse : ApiResponse
    {
        public IEnumerable<string> Errors { get; }

        public ApiUnauthorizedResponse()
            : base(401)
        {
            var errors = new List<string> { "User does not have sufficient permission." };
            Errors = errors;
        }
    }
}
