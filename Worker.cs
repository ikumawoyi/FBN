using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Globalization;

namespace TOPdeskToAjua
{
	public class Worker : BackgroundService
	{
		private readonly ILogger<Worker> _logger;
		private HttpClient client;

		public Worker(ILogger<Worker> logger)
		{
			_logger = logger;
		}

		public override Task StartAsync(CancellationToken cancellationToken)
		{
			client = new HttpClient();
			return base.StartAsync(cancellationToken);
		}
		public override Task StopAsync(CancellationToken cancellationToken)
		{
			client.Dispose();
			return base.StopAsync(cancellationToken);
		}

		public Object newSingleObjectToAjua;

		public IDictionary<string, object> newObjectToAjua = new Dictionary<string, object>();
		IList<string> idList = new List<string>();

		public string ticketCallerId { get; set; }

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				var	TimeToRun = DateTime.Now.Date.AddDays(-1).ToString("yyyy-MM-dd");
				var TimeToRunYearly = DateTime.Now.Month;
				var username = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("Myconstants")["username"];
				var applicationPassword = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("Myconstants")["applicationPassword"];
				var authToken = Encoding.ASCII.GetBytes($"{username}:{applicationPassword}");
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
						Convert.ToBase64String(authToken));

				var topDeskUrl = "https://fbncapital.topdesk.net/tas/api/incidents?completed=true&page_size=1000&closed_date_start=" + TimeToRun;

				//var result = await client.GetAsync("https://fbncapital.topdesk.net/tas/api/operatorChanges?status=closed");
				//var result = await client.GetAsync("https://fbncapital.topdesk.net/tas/api/incidents");
				//var result = await client.GetAsync("https://fbncapital.topdesk.net/tas/api/incidents?page_size=1000&completed=true");
				//var result = await client.GetAsync("https://fbncapital.topdesk.net/tas/api/incidents?completed=true&page_size=10000&closed_date_start=2019-01-14");
				var result = await client.GetAsync(topDeskUrl);



				if (result.IsSuccessStatusCode)
				{
					string jsonString = await result.Content.ReadAsStringAsync();

					dynamic newObject = JsonConvert.DeserializeObject(jsonString);


					foreach (var item in newObject)
					{
						ticketCallerId = string.Empty;
						var joinCode = string.Empty;
						var department = string.Empty;
						var category = string.Empty;
						var subcategory = string.Empty;
						var gender = string.Empty;
						var phoneNumber = string.Empty;
						var name = string.Empty;
						var channel = "top desk";
						var service = string.Empty;
						var commDomain = "FBNQuest";
						if (item != null)
						{
							ticketCallerId = item.caller.id;
							
							
							if (item.category != null)
							{
								category = item.category.name;
							}
							if (item.subcategory != null)
							{
								subcategory = item.subcategory.name;
							}

							Console.WriteLine("Ticket last fetching time: {0}", TimeToRun);
							Console.WriteLine("Ticket ID: {0}", ticketCallerId);
							Console.WriteLine("Ticket Category: {0}", category);
							Console.WriteLine("Ticket Subcategory: {0}", subcategory);
							var url = "https://fbncapital.topdesk.net/tas/api/persons/id/" + ticketCallerId;
							var personDetails = await client.GetAsync(url);
							string personDetailsString = await personDetails.Content.ReadAsStringAsync();
							dynamic personDetailsObject = JsonConvert.DeserializeObject(personDetailsString);
							if (personDetailsObject.phoneNumber != null)
							{
								phoneNumber = personDetailsObject.phoneNumber;
							}
							if (personDetailsObject.firstName != null)
							{
								name = personDetailsObject.firstName;
							}
							if (personDetailsObject.gender != null)
							{
								gender = personDetailsObject.gender;
							}
							if (personDetailsObject.department != null)
							{
								department = personDetailsObject.department.name;
							}
							Console.WriteLine("Ticket caller Name: {0}", name);
							Console.WriteLine("Ticket Caller Phone number: {0}", phoneNumber);
							Console.WriteLine("Ticket Caller Gender: {0}", gender);
							Console.WriteLine("Ticket Caller Department: {0}", department);


							//Streamlining data to Ajua
							if (department == ("General Services") || category.Contains("GS"))
							{
								joinCode = "GSSC01";
								service = "GS Service Catalogue";
							}
							else if (department == ("Legal") || category.Contains("Legal"))
							{
								joinCode = "LEGAL01";
								service = "Legal Services";
							}
							else if (department == ("People Management") && category.Contains("PM"))
							{
								joinCode = "PMGT01";
								service = "People Management";
							}
							else if (department == ("M&CC") && category.Contains("M&CC-Existing Project Support"))
							{
								joinCode = "MACC08";
								service = "Existing Project Support";
							}
							else if (department == ("M&CC") && category.Contains("M&CC-Publications Support"))
							{
								joinCode = "MACC07";
								service = "Publications Support";
							}
							else if (department == ("M&CC") && category.Contains("M&CC-New Project Support"))
							{
								joinCode = "MACC06";
								service = "New Project Support";
							}
							else if (department == ("M&CC") && category.Contains("M&CC-Website Management"))
							{
								joinCode = "MACC05";
								service = "Website Management";
							}
							else if (department == ("M&CC") && category.Contains("M&CC-New Project Support") && subcategory.Contains("Advertising & Strategic Support"))
							{
								joinCode = "MACC04";
								service = "New Project Support (Advertising & Strategic Support)";
							}
							else if (department == ("M&CC") && category.Contains("M&CC-New Project Support") && subcategory.Contains("Internal Communications"))
							{
								joinCode = "MACC03";
								service = "New Project Support (Internal Communications)";
							}
							else if (department == ("M&CC") && category.Contains("M&CC-Conference Support"))
							{
								joinCode = "MACC02";
								service = "Conference Support";
							}
							else if (department == ("M&CC") && category.Contains("M&CC-Existing Project Support") && subcategory.Contains("External Communications"))
							{
								joinCode = "MACC01";
								service = "Existing Project Support (External Communications)";
							}
							else if (department == ("Information technology") || category.Contains("IT"))
							{
								joinCode = "TECH01";
								service = "Information technology";
							}
							else
							{
								joinCode = "";
							}
							Console.WriteLine("Ticket Caller Join code: {0}", joinCode);

							_logger.LogInformation("The data was fetched with status {StatusCode}", result.StatusCode);

						}
						else
						{
							Console.WriteLine("item is empty");
						}
						var count = phoneNumber.Count();
						newSingleObjectToAjua = new
						{
							joincode = joinCode,
							commDomain = commDomain,
							//commId = "+2348032353494",
							commId = phoneNumber,
							participantMetadata = new
							{
								name = name,
								department = department,
								category = category,
								gender = gender,
								service = service,
								channel = channel
							},
						};
						if (!idList.Contains(ticketCallerId) && joinCode != "")
						{
							newObjectToAjua.Add(ticketCallerId, newSingleObjectToAjua);
						}

						await SendDataToAjua();      
					}
					_logger.LogInformation("The data was sent to Ajua");
				}
				else
				{
					_logger.LogError("Could not fetch data with {StatusCode}", result.StatusCode);
				}
				await Task.Delay(24*60*60*1000, stoppingToken);
			}
		}
		private async Task SendDataToAjua()
		{
			var client = new HttpClient();
			var ajuaUrl = "https://api.ajua.com/v1/surveys/participant/send";

			foreach (KeyValuePair<string, object> item in newObjectToAjua)
			{
					var ajuaContent = JsonConvert.SerializeObject(item.Value);
					var buffer = Encoding.UTF8.GetBytes(ajuaContent);
					var byteajuaContent = new ByteArrayContent(buffer);
					byteajuaContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
					var response = await client.PostAsync(ajuaUrl, byteajuaContent);
					string ajuaResult = response.Content.ReadAsStringAsync().Result;
					Console.WriteLine(ajuaResult);
					Console.WriteLine("________________________________________________");
					idList.Add(ticketCallerId);
					newObjectToAjua.Remove(item.Key);
				
			}

		}

	}
}

