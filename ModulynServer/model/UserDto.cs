namespace ModulynServer.model
{
    public class UserDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; } // Assuming single role for simplicity, or List<string> Roles for multiple
        // Add other properties you want to display/edit (e.g., PhoneNumber)
    }
}
