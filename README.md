# Identity Server 4

```sh
mkdir security04
cd security04

# ASP.NET Core Empty Project
dotnet new web -n is4 -f netcoreapp3.1
dotnet sln add is4
```

📄 is4/Properties/launchSettings.json

```json
{
  "profiles": {
    "is4": {
      "commandName": "Project",
      "launchBrowser": true,
      "applicationUrl": "https://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

```sh
cd is4
dotnet add package IdentityServer4 --version 4.1.1
```

📄 is4/Config.cs

```csharp
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Test;
using System.Collections.Generic;
using System.Security.Claims;

namespace is4
{
	public class Config
	{
		public static List<TestUser> TestUsers =>
			new List<TestUser>
			{
				new TestUser
				{
					SubjectId = "1144",
					Username = "mukesh",
					Password = "mukesh",
					Claims =
					{
						new Claim(JwtClaimTypes.Name, "Mukesh Murugan"),
						new Claim(JwtClaimTypes.GivenName, "Mukesh"),
						new Claim(JwtClaimTypes.FamilyName, "Murugan"),
						new Claim(JwtClaimTypes.WebSite, "http://codewithmukesh.com"),
					}
				}
			};

		public static IEnumerable<IdentityResource> IdentityResources =>
			new IdentityResource[]
			{
				new IdentityResources.OpenId(),
				new IdentityResources.Profile(),
			};

		public static IEnumerable<ApiResource> ApiResources =>
			new ApiResource[]
			{
				new ApiResource("myApi")
				{
					Scopes = new List<string>{ "myApi.read","myApi.write" },
					ApiSecrets = new List<Secret>{ new Secret("supersecret".Sha256()) }
				}
			};

		public static IEnumerable<ApiScope> ApiScopes =>
			new ApiScope[]
			{
				new ApiScope("myApi.read"),
				new ApiScope("myApi.write"),
			};

		public static IEnumerable<Client> Clients =>
			new Client[]
			{
				new Client
				{
					ClientId = "cwm.client",
					ClientName = "Client Credentials Client",
					AllowedGrantTypes = GrantTypes.ClientCredentials,
					ClientSecrets = { new Secret("secret".Sha256()) },
					AllowedScopes = { "myApi.read" }
				},
			};
	}
}
```

📄 is4/Startup.cs

```csharp
public void ConfigureServices(IServiceCollection services)
{
	services
		.AddIdentityServer()
		.AddInMemoryClients(Config.Clients)
		.AddInMemoryIdentityResources(Config.IdentityResources)
		.AddInMemoryApiResources(Config.ApiResources)
		.AddInMemoryApiScopes(Config.ApiScopes)
		.AddTestUsers(Config.TestUsers)
		.AddDeveloperSigningCredential();
}
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
	app.UseIdentityServer();
}
```

```powershell
# Verifique a porta em 📄 is4/appsettings.json
https://localhost:5001/.well-known/openid-configuration
```

# Post 1

```sh
cd ..
dotnet new console -n post1 -f netcoreapp3.1
dotnet sln add post1
cd post1
dotnet add package Newtonsoft.Json --version 13.0.3
```

📄 post1/Program.cs

```csharp
using System;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
	private
	static async Task Main()
	{
		string tokenEndpoint = "https://localhost:5001/connect/token";
		string grantType = "client_credentials";
		string clientId = "cwm.client";
		string clientSecret = "secret";
		string scope = "myApi.read";

		string requestBody = $"grant_type={grantType}&scope={scope}&client_id={clientId}&client_secret={clientSecret}";
		using (var httpClient = new HttpClient())
		{
			var content = new StringContent(
				requestBody,
				System.Text.Encoding.UTF8,
				"application/x-www-form-urlencoded"
			);

			var rp = await httpClient.PostAsync(tokenEndpoint, content);

			if (rp.IsSuccessStatusCode)
			{
				var rs = await rp.Content.ReadAsStringAsync();
				if (rs != null)
				{
					Console.WriteLine(rs);
				}
			}
			else
			{
				Console.WriteLine($"Erro na solicitação: {rp.StatusCode}");
			}
		}
	}
}
```

# Api

```sh
cd ..
# WebAPI do ASP.NET Core
dotnet new webapi -n WebApi -f netcoreapp3.1
dotnet sln add WebApi
```

📄 WebApi/Properties/launchSettings.json

```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "WebApi": {
      "commandName": "Project",
      "launchBrowser": true,
      "launchUrl": "weatherforecast",
      "applicationUrl": "https://localhost:5002",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

```sh
cd WebApi
dotnet add package IdentityServer4.AccessTokenValidation --version 3.0.1
```

📄 WebApi/Startup.cs

```csharp
public void ConfigureServices(IServiceCollection services)
{
	services
		.AddAuthentication("Bearer")
		.AddIdentityServerAuthentication("Bearer", options =>
		{
			options.ApiName = "myApi";
			options.Authority = "https://localhost:5001";
			// Permite sem https
			// options.RequireHttpsMetadata = false;
		});
}
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
	app.UseAuthentication();
	app.UseAuthorization();
}
```

📄 WebApi/Controllers/WeatherForecastController.cs

```csharp
[ApiController]
[Route("[controller]")]
[Authorize]
public class WeatherForecastController : ControllerBase
```

📄 post1/IAccessToken.cs

```csharp
namespace post1
{
	internal class IAccessToken
	{
		public string access_token { get; set; }
		public string expires_in { get; set; }
		public string token_type { get; set; }
		public string scope { get; set; }
	}
}
```

📄 post1/IResponse.cs

```csharp
namespace post1
{
	internal class IResponse
	{
		public string date { get; set; }
		public string temperatureC { get; set; }
		public string temperatureF { get; set; }
		public string summary { get; set; }
	}
}
```

📄 post1/Program.cs

```csharp
using Newtonsoft.Json;
using post1;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

class Program
{
	private
	static async Task Main()
	{
		string tokenEndpoint = "https://localhost:5001/connect/token";
		string grantType = "client_credentials";
		string clientId = "cwm.client";
		string clientSecret = "secret";
		string scope = "myApi.read";

		string requestBody = $"grant_type={grantType}&scope={scope}&client_id={clientId}&client_secret={clientSecret}";
		using (var httpClient = new HttpClient())
		{
			var content = new StringContent(
				requestBody,
				System.Text.Encoding.UTF8,
				"application/x-www-form-urlencoded"
			);

			var rp = await httpClient.PostAsync(tokenEndpoint, content);

			if (rp.IsSuccessStatusCode)
			{
				var rs = await rp.Content.ReadAsStringAsync();
				if (rs != null)
				{
					var obj = JsonConvert.DeserializeObject<IAccessToken>(rs);
					if (obj != null)
					{
						httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", obj.access_token);
						tokenEndpoint = "https://localhost:5002/weatherforecast";
						rp = await httpClient.GetAsync(tokenEndpoint);
						if (rp.IsSuccessStatusCode)
						{
							rs = await rp.Content.ReadAsStringAsync();
							if (rs != null)
							{
								var obj2 = JsonConvert.DeserializeObject<List<IResponse>>(rs);
								if (obj2 != null)
								{
									foreach (var obx in obj2)
									{
										Console.WriteLine($"date: {obx.date}");
										Console.WriteLine($"temperatureC: {obx.temperatureC}");
										Console.WriteLine($"temperatureF: {obx.temperatureF}");
										Console.WriteLine($"summary: {obx.summary}");
										Console.WriteLine("");
									}
								}
							}
						}
						else
						{
							Console.WriteLine($"Erro na solicitação: {rp.StatusCode}");
						}
					}
				}
			}
			else
			{
				Console.WriteLine($"Erro na solicitação: {rp.StatusCode}");
			}
		}
	}
}
```

# Client

```sh
cd ..
# ASP.NET Core web Application (mvc)
dotnet new mvc -n WebClient -f netcoreapp3.1
dotnet sln add WebClient
```

📄 WebClient/Properties/launchSettings.json

```json
{
  "profiles": {
    "WebClient": {
      "commandName": "Project",
      "launchBrowser": true,
      "applicationUrl": "https://localhost:5003",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

📄 WebClient/Startup.cs

```csharp
public void ConfigureServices(IServiceCollection services)
{
	services.AddTransient<TokenService>();
}
```

```sh
cd WebClient
dotnet add package IdentityModel --version 4.4.0
dotnet add package Microsoft.VisualStudio.Web.CodeGeneration.Design --version 3.1.4
```

📄 WebClient/Services/ITokenService.cs

```csharp
using IdentityModel.Client;
using System.Threading.Tasks;

public interface ITokenService
{
	Task<TokenResponse> GetToken(string scope);
}
```

📄 WebClient/Services/TokenService.cs

```csharp
using IdentityModel.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;

public class TokenService : ITokenService
{
	private DiscoveryDocumentResponse _discDocument { get; set; }
	public TokenService()
	{
		using (var client = new HttpClient())
		{
			_discDocument = client.GetDiscoveryDocumentAsync("https://localhost:5001/.well-known/openid-configuration").Result;
		}
	}
	public async Task<TokenResponse> GetToken(string scope)
	{
		using (var client = new HttpClient())
		{
			var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
			{
				Address = _discDocument.TokenEndpoint,
				ClientId = "cwm.client",
				Scope = scope,
				ClientSecret = "secret"
			});
			if (tokenResponse.IsError)
			{
				throw new Exception("Token Error");
			}
			return tokenResponse;
		}
	}
}
```

📄 WebClient/Models/WeatherModel.cs

```csharp
using System;

public class WeatherModel
{
	public DateTime Date { get; set; }
	public int TemperatureC { get; set; }
	public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
	public string Summary { get; set; }
}
```

📄 WebClient/Controllers/HomeController.cs

```csharp
namespace WebClient.Controllers
{
	public class HomeController : Controller
	{
		private readonly TokenService _tokenService;

        public HomeController(ILogger<HomeController> logger, TokenService tokenService)
        {
            _logger = logger;
            _tokenService = tokenService;
        }

		public async Task<IActionResult> Weather()
		{
			var data = new List<WeatherModel>();
			var token = await _tokenService.GetToken("myApi.read");
			using (var client = new HttpClient())
			{
				client.SetBearerToken(token.AccessToken);
				var result = await client.GetAsync("https://localhost:5002/weatherforecast");
				if (result.IsSuccessStatusCode)
				{
					var model = await result.Content.ReadAsStringAsync();
					data = JsonConvert.DeserializeObject<List<WeatherModel>>(model);
					return View(data);
				}
				else
				{
					throw new Exception("Failed to get Data from API");
				}
			}
		}
	}
}
```

📄 WebClient/Views/Home/Weather.cshtml

```html
@model List<WeatherModel>
  @{ ViewData["Title"] = "Weather"; }
  <h1>Weather</h1>
  <table class="table table-striped">
    @foreach (var weather in Model) {
    <tr>
      <td>@weather.Date</td>
      <td>@weather.Summary</td>
      <td>@weather.TemperatureC</td>
      <td>@weather.TemperatureF</td>
    </tr>
    }
  </table></WeatherModel
>
```
