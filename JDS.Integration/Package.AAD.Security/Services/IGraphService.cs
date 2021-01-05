using System;
using Microsoft.Graph;
using Package.AAD.Security.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Package.AAD.Security.Services
{
    public interface IGraphService
    {
        Task AddUserInGroupAsync(string mail, string groupId);
        Task RemoveUserFromGroupAsync(string mail, string groupId);
        Task<IList<DirectoryObject>> GetAllGroupMembers(string groupId);
        Task<IGraphServiceUsersCollectionPage> GetAllUsers();
        Task<bool> IsInGroup(string mail, string groupId);
        Task<bool> IsGroupOwner(string mail, string groupId);
        Task<IEnumerable<GroupInfo>> GetAllGroupsOfUser(string mail);
        Task<IEnumerable<User>> GetAllB2CUsers();
        Task<IEnumerable<User>> GetAllB2CUsersByOrganizationId(int organizationId);
        Task<User> GetB2CUserByEmail(string email);
        Task<User> GetB2CUserByUserId(Guid userId);
        Task<User> CreateB2CUser(User user, int organizationId, bool isAdmin, bool isActive, string password);
        Task<User> UpdateB2CByEmail(string email, int organizationId, string displayName, bool isAdmin, bool isActive, bool isEnabled);
        Task DeleteB2CUserById(string userId);


    }
}
