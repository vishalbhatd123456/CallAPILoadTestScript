using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using System.Globalization;
using Newtonsoft.Json;
using System.Threading;

class Program
{
    static async Task Main(string[] args)
    {
	// Step 1: Path to the CSV File
	string csvFilePath = @"";// Update this with your actual CSV file path

	// Step 2: Read order IDs from CSV
	var orderIds = ReadCsv(csvFilePath);

	// Limit to the first 20 records for testing
	orderIds = orderIds.GetRange(0, Math.Min(orderIds.Count, 20));

	// Step 3: Initialize HttpClient
	using HttpClient client = new HttpClient();
	client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ""); // Replace with your token
	client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

	string apiUrl = "Your_API_URL";

	// Step 4: Use SemaphoreSlim for controlled parallelism
	int maxConcurrentRequests = 200; // Adjust this for higher or lower concurrency
	SemaphoreSlim semaphore = new SemaphoreSlim(maxConcurrentRequests);

	Console.WriteLine("Starting API calls...");
	var tasks = new List<Task>();

	foreach (var orderId in orderIds)
	{
	    await semaphore.WaitAsync(); // Ensure concurrency limit
	    tasks.Add(Task.Run(async () =>
	    {
		try
		{
		    await CallApiAsync(client, apiUrl, orderId);
		}
		finally
		{
		    semaphore.Release(); // Release the semaphore after request completes
		}
	    }));
	}

	// Wait for all tasks to complete
	await Task.WhenAll(tasks);

	Console.WriteLine("All API calls completed.");
    }

    // Method to make an API call
    static async Task CallApiAsync(HttpClient client, string apiUrl, string orderId)
    {
	// Construct the payload
	var payload = new
	{
	    
	};

	try
	{
	    // Serialize payload to JSON
	    var jsonPayload = JsonConvert.SerializeObject(payload);
	    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

	    // Send the API request
	    var response = await client.PutAsync(apiUrl, content);

	    // Log the response
	    Console.WriteLine($"Completed, Status: {response.StatusCode}");
	    if (!response.IsSuccessStatusCode)
	    {
		string error = await response.Content.ReadAsStringAsync();
		Console.WriteLine($"Error for OrderId {orderId}: {error}");
	    }
	}
	catch (Exception ex)
	{
	    Console.WriteLine($"Exception for OrderId {orderId}: {ex.Message}");
	}
    }

    // Method to read Order IDs from CSV
    static List<string> ReadCsv(string filePath)
    {
	var orderIds = new List<string>();
	using var reader = new StreamReader(filePath);
	using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
	csv.Read();
	csv.ReadHeader();
	while (csv.Read())
	{
	    orderIds.Add(csv.GetField("Value_To_Be_Read"));
	}
	return orderIds;
    }
}
