using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Graph;
using Newtonsoft.Json;

using Package.AAD.Security.Entities;
using Group = Microsoft.Graph.Group;


namespace Package.AAD.Security.Services
{
    public class GraphService : IGraphService
    {
        private readonly IOptions<GraphApiSetting> _graphApiSetting;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly IOptions<AzureAdB2CSetting> _azureB2CSetting;


        public GraphService(IOptions<GraphApiSetting> graphApiSetting, IGraphServiceClientProvider graphServiceClientProvider, IOptions<AzureAdB2CSetting> azureB2CSetting)
        {
            _graphServiceClient = graphServiceClientProvider.GraphServiceClientWithClientCredentialProviderAsync().Result;
            _graphApiSetting = graphApiSetting;
            _azureB2CSetting = azureB2CSetting;
        }

        public async Task AddUserInRole(string mail, string roleId)
        {
            var user = await _graphServiceClient.Users[mail].Request().GetAsync();

            //https://stackoverflow.com/questions/44115248/ms-graph-api-c-sharp-add-user-to-group
            User userToAdd = await _graphServiceClient.Users[user.Id.ToString()].Request().GetAsync();

            await _graphServiceClient.DirectoryRoles[roleId].Members.References.Request().AddAsync(userToAdd);
        }

        public async Task RemoveUserFromRole(string mail, string roleId)
        {
            var user = await _graphServiceClient.Users[mail].Request().GetAsync();

            await _graphServiceClient.DirectoryRoles[roleId].Members[user.Id.ToString()].Reference.Request().DeleteAsync();
        }

        public async Task<bool> IsGroupOwner(string mail, string groupId)
        {
            var owners = await _graphServiceClient.Groups[groupId].Owners.Request().GetAsync();

            do
            {
                foreach (User user in owners)
                {
                    if (user.Mail.Equals(mail, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            while (owners.NextPageRequest != null && (owners = await owners.NextPageRequest.GetAsync()).Count > 0);

            return false;
        }

        public async Task<IGraphServiceUsersCollectionPage> GetAllUsers()
        {
            return await _graphServiceClient.Users.Request().GetAsync();
        }

        public async Task<bool> IsInGroup(string mail, string groupId)
        {
            var allGroupsOfUser = await GetAllGroupsOfUser(mail);

            return allGroupsOfUser.Any(x => x.Id.Equals(groupId));
        }

        public async Task<IList<DirectoryObject>> GetAllGroupMembers(string groupId)
        {
            var group = await _graphServiceClient.Groups[groupId].Request().Expand("members").GetAsync();
            List<DirectoryObject> result = new List<DirectoryObject>();

            var members = group.Members;

            do
            {
                result.AddRange(members);
            }
            while (members.NextPageRequest != null && (members = await members.NextPageRequest.GetAsync()).Count > 0);

            return result;
        }

        public async Task AddUserInGroupAsync(string mail, string groupId)
        {
            var user = await _graphServiceClient.Users[mail].Request().GetAsync();

            //https://stackoverflow.com/questions/44115248/ms-graph-api-c-sharp-add-user-to-group

            await _graphServiceClient.Groups[groupId].Members.References.Request().AddAsync(user);
        }

        public async Task RemoveUserFromGroupAsync(string mail, string groupId)
        {
            var user = await _graphServiceClient.Users[mail].Request().GetAsync();

            await _graphServiceClient.Groups[groupId].Members[user.Id.ToString()].Reference.Request().DeleteAsync();
        }

        public async Task<IEnumerable<GroupInfo>> GetAllGroupsOfUser(string mail)
        {
            IList<GroupInfo> result = new List<GroupInfo>();

            IUserMemberOfCollectionWithReferencesPage memberOfGroups = await _graphServiceClient.Users[mail].MemberOf.Request().GetAsync();
            //var transitiveMemberOf = await _graphServiceClient.Users[mail].TransitiveMemberOf.Request().GetAsync();

            do
            {
                foreach (var directoryObject in memberOfGroups)
                {
                    // We only want groups, so ignore DirectoryRole objects.
                    if (directoryObject is Group)
                    {
                        Group group = directoryObject as Group;
                        result.Add(new GroupInfo { Id = group.Id, DisplayName = group.DisplayName });
                    }
                }
            }
            while (memberOfGroups.NextPageRequest != null && (memberOfGroups = await memberOfGroups.NextPageRequest.GetAsync()).Count > 0);

            return result;
        }

        public async Task<IEnumerable<User>> GetAllB2CUsers()
        {
            var result = await _graphServiceClient.Users
                .Request()
                .Select($"id,displayName,identities,{GetOrganizationCustomAttributeName()},{GetIsAdminCustomAttributeName()},{GetIsActiveCustomAttributeName()}")
                .GetAsync();

            return (result.ToList());
        }

        public async Task<IEnumerable<User>> GetAllB2CUsersByOrganizationId(int organizationId)
        {

            if (string.IsNullOrWhiteSpace(_graphApiSetting.Value.B2CExtensionAppClientId))
            {
                throw new ArgumentException("B2cExtensionAppClientId (its Application ID) is missing from appsettings.json. Find it in the App registrations pane in the Azure portal. The app registration has the name 'b2c-extensions-app. Do not modify. Used by AADB2C for storing user data.'.");
            }

            // Get all users (one page)
            var users = await _graphServiceClient.Users
                .Request()
                .Filter($"{GetOrganizationCustomAttributeName()} eq {organizationId}")
                .Select($"id,displayName,identities,{GetOrganizationCustomAttributeName()},{GetIsAdminCustomAttributeName()},{GetIsActiveCustomAttributeName()}")
                .GetAsync();

            return (users.ToList());

        }

        public async Task<User> GetB2CUserByEmail(string email)
        {
        // Get user by sign-in name
            var result = await _graphServiceClient.Users
                .Request()
                .Filter($"identities/any(c:c/issuerAssignedId eq '{email}' and c/issuer eq '{_graphApiSetting.Value.TenantId}')")
                .Select($"id,displayName,identities,{GetOrganizationCustomAttributeName()},{GetIsAdminCustomAttributeName()},{GetIsActiveCustomAttributeName()}")
                .GetAsync();
            return result.Count > 0 ? result.First() : null;

          
        }

        public async Task<User> GetB2CUserByUserId(Guid userId)
        {

            // Get user by sign-in name
            var result = await _graphServiceClient.Users[userId.ToString()]
                .Request()
                .Select($"id,displayName,identities,{GetOrganizationCustomAttributeName()},{GetIsAdminCustomAttributeName()},{GetIsActiveCustomAttributeName()}")
                .GetAsync();

            return result;

        }

        public async Task<User> UpdateB2CByEmail(string email, int organizationId, string displayName, bool isAdmin, bool isActive, bool isEnabled)
        {

            // Get user by sign-in name
            var existingUser = await GetB2CUserByEmail(email);

                existingUser.DisplayName= displayName;
                existingUser.AccountEnabled = isEnabled;

                var extensionInstance = new Dictionary<string, object>
                    {
                        { GetOrganizationCustomAttributeName(), organizationId },
                        { GetIsAdminCustomAttributeName(), isAdmin },
                        { GetIsActiveCustomAttributeName(), isActive }
            };
                existingUser.AdditionalData = extensionInstance;


            // Update user by object ID
          await _graphServiceClient.Users[existingUser.Id]
                    .Request()
                    .UpdateAsync(existingUser);
            
              return existingUser;

        }

        public async Task<User> CreateB2CUser(User user, int organizationId, bool isAdmin, bool isActive, string password)
        {
            var extensionInstance = new Dictionary<string, object>
            {
                { GetOrganizationCustomAttributeName(), organizationId },
                { GetIsAdminCustomAttributeName(), isAdmin },
                { GetIsActiveCustomAttributeName(), isActive }
            };

            var userPassword = !string.IsNullOrWhiteSpace(password)
                ? password
                : _azureB2CSetting.Value.DefaultUserPassword;

            user.AdditionalData = extensionInstance;

            var identities = new List<ObjectIdentity>
            {
                new ObjectIdentity()
                {
                    SignInType = "emailAddress",
                    Issuer = _azureB2CSetting.Value.Tenant,
                    IssuerAssignedId = user.Mail
                }
            };

            var passwordProfile = new PasswordProfile()
            {
                Password = userPassword,
                ForceChangePasswordNextSignIn = false

            };


            user.Identities = identities;
            user.PasswordProfile = passwordProfile;
            if (!string.IsNullOrEmpty(_azureB2CSetting.Value.PasswordPolicies))
                user.PasswordPolicies = _azureB2CSetting.Value.PasswordPolicies;

            try
            {

                var result = await _graphServiceClient.Users
                    .Request()
                    .AddAsync(user);

                result = await _graphServiceClient.Users[result.Id]
                            .Request()
                            .Select($"id,displayName,identities,{GetOrganizationCustomAttributeName()},{GetIsAdminCustomAttributeName()}")
                            .GetAsync();

                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }



        public async Task DeleteB2CUserById(string userId)
        {
            await _graphServiceClient.Users[userId]
                 .Request()
                 .DeleteAsync();
        }


        private string GetOrganizationCustomAttributeName()
        {
            // Declare the names of the custom attributes
            const string customAttributeName1 = "organizationId";

            // Get the complete name of the custom attribute (Azure AD extension)
            var helper = new Helpers.B2cCustomAttributeHelper(_graphApiSetting.Value.B2CExtensionAppClientId);
            var organizationIdAttributeName = helper.GetCompleteAttributeName(customAttributeName1);

            return organizationIdAttributeName;

        }

        private string GetIsAdminCustomAttributeName()
        {
            // Declare the names of the custom attributes
            const string customAttributeName1 = "isAdmin";

            // Get the complete name of the custom attribute (Azure AD extension)
            var helper = new Helpers.B2cCustomAttributeHelper(_graphApiSetting.Value.B2CExtensionAppClientId);
            var organizationIdAttributeName = helper.GetCompleteAttributeName(customAttributeName1);

            return organizationIdAttributeName;

        }

        private string GetIsActiveCustomAttributeName()
        {
            // Declare the names of the custom attributes
            const string customAttributeName1 = "isActive";

            // Get the complete name of the custom attribute (Azure AD extension)
            var helper = new Helpers.B2cCustomAttributeHelper(_graphApiSetting.Value.B2CExtensionAppClientId);
            var isActiveAttributeName = helper.GetCompleteAttributeName(customAttributeName1);

            return isActiveAttributeName;

        }
    }
}
