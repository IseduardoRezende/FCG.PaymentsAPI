using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace FCG.Payments.IntegrationTests.Infrastructure;

public static class TestClient
{
    // Must match TokenSettings in appsettings.json: tokens are issued by the Users API in production.
    private const string Issuer = "FCG.Users";
    private const string Audience = "FCG.Client";
    private const string Key = "local-dev-only-key-change-me-0123456789-abcdefghijklmnopqrstuvwxyz";

    private static long _nextId = Random.Shared.Next(1_000, 100_000);

    public static long NewId() => Interlocked.Increment(ref _nextId);

    public static HttpClient AsUser(this PaymentsApiFactory factory, long userId, string role = "User")
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(userId, role));
        return client;
    }

    public static async Task<JsonElement> ReadValueAsync(this HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return document.RootElement.GetProperty("value").Clone();
    }

    public static async Task WaitUntilAsync(Func<Task<bool>> condition, CancellationToken cancellationToken)
    {
        var timeout = DateTime.UtcNow.AddSeconds(15);

        while (!await condition())
        {
            if (DateTime.UtcNow > timeout)
                throw new TimeoutException("Condition was not met within 15 seconds.");

            await Task.Delay(200, cancellationToken);
        }
    }

    private static string CreateToken(long userId, string role)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, $"player{userId}@test.com"),
            new Claim(ClaimTypes.Role, role)
        };

        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(Issuer, Audience, claims, expires: DateTime.UtcNow.AddHours(1), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
