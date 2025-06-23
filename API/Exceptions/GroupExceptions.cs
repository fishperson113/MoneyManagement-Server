using API.Models.Entities;

namespace API.Exceptions
{
    public class NotGroupMemberException : UnauthorizedAccessException
    {
        public NotGroupMemberException() : base("User is not a member of this group") { }
    }

    public class InsufficientGroupRoleException : UnauthorizedAccessException
    {
        public InsufficientGroupRoleException(GroupRole requiredRole)
            : base($"This action requires at least {requiredRole} role") { }
    }

    public class UserMutedOrBannedException : UnauthorizedAccessException
    {
        public UserMutedOrBannedException() : base("User is muted or banned in this group") { }
    }

}