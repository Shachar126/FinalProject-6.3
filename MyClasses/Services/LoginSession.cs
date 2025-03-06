namespace MyClasses.Services
{
    public class LoginSession
    {
        // Default values for a guest.
        public int user_id { get; private set; } = 0;
        public string Name { get; private set; } = "Guest";
        public string email { get; private set; } = "guest@site.com";
        public string role { get; private set; } = "Guest";

        public void SetLoginDetails(int user_id, string name, string email, string role)
        {
            // Use "this." to reference the class fields.
            this.user_id = user_id;
            this.Name = name;
            this.email = email;
            this.role = role;
        }

        public void ClearSession()
        {
            user_id = 0;
            Name = "Guest";
            email = "guest@site.com";
            role = "Guest";
        }

        // Returns true if the current session is still a guest.
        public bool IsGuest()
        {
            return role == "Guest";
        }
    }
}
