namespace FCG.UsersAPI.Api.Authorization
{
    public static class Policies
    {
        public const string AdminOnly = nameof(AdminOnly);
        public const string UserOnly = nameof(UserOnly);
        public const string UserOrAdmin = nameof(UserOrAdmin);
    }
}
