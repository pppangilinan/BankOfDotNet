using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IdentityModel.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BankOfDotNet.ConsoleClient
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        private static async Task MainAsync()
        {
            var discoveryResponse = await DiscoveryClient.GetAsync("http://localhost:6576");

            if (discoveryResponse.IsError) Console.WriteLine(discoveryResponse.Error);

            var tokenClient = new TokenClient(discoveryResponse.TokenEndpoint, "client", "secret");
            var tokenResponse = await tokenClient.RequestClientCredentialsAsync("bankOfDotNetApi");

            if (tokenResponse.IsError) Console.WriteLine(tokenResponse.Error);

            Console.WriteLine(tokenResponse.Json);
            Console.WriteLine("\n\n");

            var client = new HttpClient();
            client.SetBearerToken(tokenResponse.AccessToken);

            var customerInformation = new StringContent(
                JsonConvert.SerializeObject(new
                {
                    Id = 10,
                    FirstName = "Manish",
                    LastName = "Narayan"
                }), Encoding.UTF8, "application/json");

            var createCustomerResponse = await client
                .PostAsync(
                    "http://localhost:9266/api/customers",
                    customerInformation
                );

            if (!createCustomerResponse.IsSuccessStatusCode) Console.WriteLine(createCustomerResponse.StatusCode);

            var getCustomerResponse = await client.GetAsync("http://localhost:9266/api/customers");

            if (!createCustomerResponse.IsSuccessStatusCode)
            {
                Console.WriteLine(getCustomerResponse.StatusCode);
            }
            else
            {
                var content = await getCustomerResponse.Content.ReadAsStringAsync();

                Console.WriteLine(JArray.Parse(content));
            }

            Console.Read();
        }
    }
}