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