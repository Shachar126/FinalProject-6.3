using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using MyClasses; // Ensure MyClasses is used
using System.Data;
using System.Threading.Tasks;

namespace MyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileSearchingController : ControllerBase
    {
        private readonly string _connectionString;

        public ProfileSearchingController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpPost("create-card")]
        public async Task<IActionResult> CreateCard([FromBody] User_cards card) // Use User_cards
        {
            if (card == null || string.IsNullOrWhiteSpace(card.games))
            {
                return BadRequest("Games are required.");
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var fetchUserQuery = "SELECT first_name, username FROM person WHERE user_id = @UserId";
                using var fetchUserCommand = new MySqlCommand(fetchUserQuery, connection);
                fetchUserCommand.Parameters.AddWithValue("@UserId", card.user_id);

                using var reader = await fetchUserCommand.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    card.first_name = reader.GetString("first_name");
                    card.username = reader.GetString("username");
                }
                else
                {
                    return NotFound("User not found.");
                }
                reader.Close();

                var insertQuery = @"
                    INSERT INTO user_cards (user_id, first_name, username, bio, games, picture_url, created_at)
                    VALUES (@UserId, @FirstName, @Username, @Bio, @Games, @PictureUrl, NOW())";

                using var insertCommand = new MySqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@UserId", card.user_id);
                insertCommand.Parameters.AddWithValue("@FirstName", card.first_name);
                insertCommand.Parameters.AddWithValue("@Username", card.username);
                insertCommand.Parameters.AddWithValue("@Bio", card.bio ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@Games", card.games);
                insertCommand.Parameters.AddWithValue("@PictureUrl", card.picture_url ?? (object)DBNull.Value);

                await insertCommand.ExecuteNonQueryAsync();
                return Ok("Card created successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
