namespace ModulynServer.model
{
    public class UserEditDto
    {
        public string Id { get; set; } // Only needed for updates
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Password { get; set; } // Only for new users or password reset (handle carefully!)
    }
}
