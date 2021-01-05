using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Package.AAD.Security.ApiCustomHttpResponses;
using Package.AAD.Security.Entities;
using Package.AAD.Security.Services;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;

namespace Package.AAD.Security.Filters
{
    public class AllowedGroupsFilterAttribute : ActionFilterAttribute
    {
        private List<string> adminGroups;
        private List<string> partnersGroups;
        private List<string> sfrsUsersGroups;
        private List<string> allowedGroupList;
        private IGraphService graphService;
        public string AllowedGroups { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            graphService = context.HttpContext.RequestServices.GetService(typeof(IGraphService)) as IGraphService;
            
            SetupGroups(context);

            string currentUserName = string.Empty;
            IEnumerable<GroupInfo> allGroupsCurrentUserBelongsTo = null;
            currentUserName = context.HttpContext.User.FindFirst(ClaimTypes.Name).Value;

            allGroupsCurrentUserBelongsTo = graphService.GetAllGroupsOfUser(currentUserName).Result;
            bool isAuthorized = AuthorizeIfUserIsMemberOfAllowedGroups(allGroupsCurrentUserBelongsTo.Select(x => x.Id));

            if (!isAuthorized)
            {
                isAuthorized = AuthorizeIfUserIsOwnerOfAllowedGroups(currentUserName);
            }

            if (!isAuthorized)
            {
                var result = new ObjectResult(new ApiUnauthorizedResponse())
                {
                    StatusCode = (int)HttpStatusCode.Unauthorized
                };

                context.Result = result;
            }

            base.OnActionExecuting(context);
        }

        private bool AuthorizeIfUserIsOwnerOfAllowedGroups(string currentUserName)
        {
            bool isAuthorized = false;
        
            foreach (var group in allowedGroupList)
            {
                switch (group)
                {
                    case ApplicationRoles.Admins:
                        isAuthorized |= adminGroups.Any(x => graphService.IsGroupOwner(currentUserName, x).Result);
                        break;
                    case ApplicationRoles.Partners:
                        isAuthorized |= partnersGroups.Any(x => graphService.IsGroupOwner(currentUserName, x).Result);
                        break;
                    case ApplicationRoles.SFRSUsers:
                        isAuthorized |= sfrsUsersGroups.Any(x => graphService.IsGroupOwner(currentUserName, x).Result);
                        break;
                }
            }

            return isAuthorized;
        }

        private bool AuthorizeIfUserIsMemberOfAllowedGroups(IEnumerable<string> allGroupIdsCurrentUserBelongsTo)
        {
            bool isAuthorized = false;
            
            foreach (var group in allowedGroupList)
            {
                switch (group)
                {
                    case ApplicationRoles.Admins:
                        isAuthorized |= allGroupIdsCurrentUserBelongsTo.Any(x => adminGroups.Contains(x));
                        break;
                    case ApplicationRoles.Partners:
                        isAuthorized |= allGroupIdsCurrentUserBelongsTo.Any(x => partnersGroups.Contains(x));
                        break;
                    case ApplicationRoles.SFRSUsers:
                        isAuthorized |= allGroupIdsCurrentUserBelongsTo.Any(x => sfrsUsersGroups.Contains(x));
                        break;
                }
            }

            return isAuthorized;
        }

        private void SetupGroups(ActionExecutingContext context)
        {
            var applicationGroups = context.HttpContext.RequestServices.GetService(typeof(IOptions<ApplicationGroupsSetting>))
                      as IOptions<ApplicationGroupsSetting>;

            var applicationGroupsSetting = applicationGroups.Value;
            adminGroups = applicationGroupsSetting.Admins.Split(",").ToList();
            partnersGroups = applicationGroupsSetting.Partners.Split(",").ToList();
            sfrsUsersGroups = applicationGroupsSetting.SFRSUsers.Split(",").ToList();

            adminGroups.ForEach(x => x.Trim());
            partnersGroups.ForEach(x => x.Trim());
            sfrsUsersGroups.ForEach(x => x.Trim());

            allowedGroupList = AllowedGroups.Split(",").Select(p => p.Trim()).ToList(); 
            
        }
    }
}
