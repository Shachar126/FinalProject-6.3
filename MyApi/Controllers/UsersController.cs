using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using MySql.Data.MySqlClient;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Data;
using MyClasses;
using Google.Protobuf.Compiler;

namespace MyApi
{
    [ApiController]
    [Route("api/[controller]")]
    public class PersonController : ControllerBase
    {
        private readonly string _connectionString;

        public PersonController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Register endpoint
        // Register endpoint (unchanged; assumes role is provided during registration)
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel registerModel)
        {
            if (registerModel == null || string.IsNullOrWhiteSpace(registerModel.username) || string.IsNullOrWhiteSpace(registerModel.password))
            {
                return BadRequest("Invalid registration data. Username and password are required.");
            }

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Check if the username already exists
            var checkQuery = "SELECT COUNT(*) FROM person WHERE username = @Username";
            using (var checkCommand = new MySqlCommand(checkQuery, connection))
            {
                checkCommand.Parameters.AddWithValue("@Username", registerModel.username);
                var userExists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;
                if (userExists)
                {
                    return Conflict("Username already exists.");
                }
            }

            // 🔥 Set role to "User" explicitly
            string userRole = "User";

            // Hash the password
            var hashedPassword = HashPassword(registerModel.password);

            // Insert new user into the database
            var query = @"
        INSERT INTO person (first_name, last_name, username, password, email, role, created_at) 
        VALUES (@FirstName, @LastName, @Username, @Password, @Email, @Role, @CreatedAt)";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@FirstName", registerModel.first_name);
            command.Parameters.AddWithValue("@LastName", registerModel.last_name);
            command.Parameters.AddWithValue("@Username", registerModel.username);
            command.Parameters.AddWithValue("@Password", hashedPassword);
            command.Parameters.AddWithValue("@Email", registerModel.email);
            command.Parameters.AddWithValue("@Role", userRole);  // 🔥 This ensures role is always "User"
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

            await command.ExecuteNonQueryAsync();
            return Ok("Registration successful.");
        }

        public class RegisterModel
        {
            public string first_name { get; set; } = string.Empty;
            public string last_name { get; set; } = string.Empty;
            public string username { get; set; } = string.Empty;
            public string password { get; set; } = string.Empty;
            public string email { get; set; } = string.Empty;
        }

        // Login endpoint (updated)
        // POST: api/Person/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            if (loginRequest == null
                || string.IsNullOrWhiteSpace(loginRequest.Username)
                || string.IsNullOrWhiteSpace(loginRequest.Password))
            {
                return BadRequest("Username or password is missing.");
            }

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT * FROM person WHERE username = @Username";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", loginRequest.Username);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var storedHash = reader.GetString("password");
                if (!VerifyPassword(loginRequest.Password, storedHash))
                {
                    return Unauthorized("Invalid username or password.");
                }

                // 🔥 Return Name as first_name + last_name
                return Ok(new
                {
                    Message = "Login successful",
                    user_id = reader.GetInt32("user_id"),
                    username = reader.GetString("username"),
                    // Combine first and last names for "Name"
                    Name = $"{reader.GetString("first_name")} {reader.GetString("last_name")}",
                    email = reader.GetString("email"),
                    role = reader.GetString("role")
                });
            }

            return Unauthorized("Invalid username or password.");
        }


        [HttpGet("getUserById/{user_id}")]
        public async Task<IActionResult> GetUserById(int user_id)
        {
            if (user_id <= 0)
            {
                return BadRequest("Invalid user ID.");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "SELECT * FROM person WHERE user_id = @UserId";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", user_id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var user = new
                    {
                        user_id = reader.GetInt32("user_id"),
                        username = reader.GetString("username"),
                        email = reader.GetString("email"),
                        first_name = reader.GetString("first_name"),
                        last_name = reader.GetString("last_name"),
                        created_at = reader.GetDateTime("created_at"),
                        role = reader.GetString("role")
                    };

                    return Ok(user);
                }
                else
                {
                    return NotFound("User not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching the user.");
            }
        }

        [HttpGet("getUserByUsername/{username}")]
        public async Task<IActionResult> GetUserByUsername(string username)
        {
            Console.WriteLine($"Received getUserByUsername request for username: {username}");

            if (string.IsNullOrWhiteSpace(username))
            {
                Console.WriteLine("Invalid username received.");
                return BadRequest("Invalid username.");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                var query = "SELECT * FROM person WHERE username = @Username";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var user = new
                    {
                        user_id = reader.GetInt32("user_id"),
                        username = reader.GetString("username"),
                        email = reader.GetString("email"),
                        first_name = reader.GetString("first_name"),
                        last_name = reader.GetString("last_name"),
                        created_at = reader.GetDateTime("created_at"),
                        role = reader.GetString("role")
                    };

                    Console.WriteLine($"✅ User found: {user.username}");
                    return Ok(user);
                }
                else
                {
                    Console.WriteLine("❌ User not found.");
                    return NotFound("User not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching user: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching the user.");
            }
        }

        // PUT: api/person/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] Person updatedUser)
        {
            if (updatedUser == null)
            {
                return BadRequest(new { message = "Invalid request. User data is required." });
            }

            if (id <= 0)
            {
                return BadRequest(new { message = "Invalid user ID." });
            }

            // Override the user_id in the payload with the id from the route.
            updatedUser.user_id = id;

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Retrieve existing email so that it is not modified.
            var getExistingQuery = "SELECT email FROM person WHERE user_id = @UserID";
            string existingEmail = "";

            using (var getCommand = new MySqlCommand(getExistingQuery, connection))
            {
                getCommand.Parameters.AddWithValue("@UserID", id);
                using var reader = await getCommand.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    existingEmail = reader["email"].ToString();
                }
                else
                {
                    return NotFound(new { message = "User not found." });
                }
            }

            // Validate the provided role.
            if (!Enum.IsDefined(typeof(RoleIn), updatedUser.role))
            {
                return BadRequest(new { message = "Invalid role provided. Role must be 'Guest', 'User', or 'Admin'." });
            }

            // Check for duplicate username (for another user).
            var checkUsernameQuery = "SELECT COUNT(*) FROM person WHERE username = @Username AND user_id <> @UserID";
            using (var checkCommand = new MySqlCommand(checkUsernameQuery, connection))
            {
                checkCommand.Parameters.AddWithValue("@Username", updatedUser.username);
                checkCommand.Parameters.AddWithValue("@UserID", id);
                int usernameExists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                if (usernameExists > 0)
                {
                    return Conflict(new { message = "Username already exists. Please choose a different one." });
                }
            }

            // Update only username, first_name, last_name, and role.
            var updateQuery = @"
        UPDATE person 
        SET username = @Username, 
            first_name = @FirstName, 
            last_name = @LastName, 
            role = @Role
        WHERE user_id = @UserID";

            using var command = new MySqlCommand(updateQuery, connection);
            command.Parameters.AddWithValue("@Username", updatedUser.username ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@FirstName", updatedUser.first_name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@LastName", updatedUser.last_name ?? (object)DBNull.Value);
            // Save the role as a string (using the enum's name).
            command.Parameters.AddWithValue("@Role", updatedUser.role.ToString());
            command.Parameters.AddWithValue("@UserID", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            if (rowsAffected > 0)
            {
                updatedUser.email = existingEmail;
                return Ok(new
                {
                    message = "User updated successfully",
                    user = updatedUser
                });
            }
            else
            {
                return NotFound(new { message = "No changes were made." });
            }
        }
    


        // DELETE: api/person/{id}
        // DELETE: api/person/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                // מחיקת כל הלייקים/דיסלייקים שהמשתמש נתן או קיבל
                var deleteLikesDislikesQuery = "DELETE FROM likesdislikes WHERE user_id = @UserID OR target_user_id = @UserID";
                using var deleteLikesDislikesCommand = new MySqlCommand(deleteLikesDislikesQuery, connection, transaction);
                deleteLikesDislikesCommand.Parameters.AddWithValue("@UserID", id);
                await deleteLikesDislikesCommand.ExecuteNonQueryAsync();

                // מחיקת כל הצ'אטים שהמשתמש שלח
                var deleteChatQuery = "DELETE FROM chatdates WHERE sender_id = @UserID";
                using var deleteChatCommand = new MySqlCommand(deleteChatQuery, connection, transaction);
                deleteChatCommand.Parameters.AddWithValue("@UserID", id);
                await deleteChatCommand.ExecuteNonQueryAsync();

                // מחיקת כל הפרופילים של המשתמש
                var deleteProfileQuery = "DELETE FROM profiles WHERE user_id = @UserID";
                using var deleteProfileCommand = new MySqlCommand(deleteProfileQuery, connection, transaction);
                deleteProfileCommand.Parameters.AddWithValue("@UserID", id);
                await deleteProfileCommand.ExecuteNonQueryAsync();

                // מחיקת כל ה"מאצ'ים" של המשתמש
                var deleteMatchesQuery = "DELETE FROM matches WHERE user1_id = @UserID OR user2_id = @UserID";
                using var deleteMatchesCommand = new MySqlCommand(deleteMatchesQuery, connection, transaction);
                deleteMatchesCommand.Parameters.AddWithValue("@UserID", id);
                await deleteMatchesCommand.ExecuteNonQueryAsync();

                // מחיקת המשתמש עצמו מהטבלה person
                var deleteUserQuery = "DELETE FROM person WHERE user_id = @UserID";
                using var deleteUserCommand = new MySqlCommand(deleteUserQuery, connection, transaction);
                deleteUserCommand.Parameters.AddWithValue("@UserID", id);
                var rowsAffected = await deleteUserCommand.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    await transaction.CommitAsync();
                    return NoContent();
                }
                return NotFound(new { message = "User not found." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error during delete operation: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while deleting the user." });
            }
        }

    



    public class GetUserByUsernameRequest
        {
            public string username { get; set; } = string.Empty;
        }

        public class GetUserByIdRequest
        {
            public int user_id { get; set; }
        }

        // HashPassword method
        private string HashPassword(string password)
        {
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return $"{Convert.ToBase64String(salt)}:{hashed}";
        }

        // VerifyPassword method
        private bool VerifyPassword(string enteredPassword, string storedHash)
        {
            var parts = storedHash.Split(':');
            Console.WriteLine(storedHash);
            if (parts.Length != 2) return false;

            var salt = Convert.FromBase64String(parts[0]);
            var storedHashedPassword = parts[1];

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: enteredPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return hashed == storedHashedPassword;
        }

        // Models
       
        public class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }
}