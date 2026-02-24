using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MMagnetic.UsersService.Data;
using System.Linq;
using Xunit;

namespace MMagnetic.UsersService.XunitTests
{
    public class UsuarioIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public UsuarioIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<UsersDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<UsersDbContext>(options => options.UseInMemoryDatabase("TestDb"));
                });
            });
        }

        [Fact]
        public async Task Register_Then_Login_Should_Return_Token()
        {
            var client = _factory.CreateClient();

            var register = new
            {
                TipoDocumento = "CC",
                NumeroDocumento = "99999999",
                PrimerNombre = "Test",
                PrimerApellido = "User",
                CorreoElectronico = "testuser@local",
                Password = "Pass123!"
            };

            var regResp = await client.PostAsJsonAsync("/api/usuarios/registrar", register);
            regResp.IsSuccessStatusCode.Should().BeTrue();

            var login = new
            {
                TipoDocumento = "CC",
                NumeroDocumento = "99999999",
                Password = "Pass123!"
            };

            var loginResp = await client.PostAsJsonAsync("/api/auth/login", login);
            loginResp.IsSuccessStatusCode.Should().BeTrue();

            var body = await loginResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            body.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Object);
            body.TryGetProperty("Token", out var tokenProp).Should().BeTrue();
            tokenProp.GetString().Should().NotBeNullOrEmpty();
        }
    }
}
