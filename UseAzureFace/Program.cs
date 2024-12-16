using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using System.Text;

internal class Program
{
    private const string subscriptionKey = "bcfb24b0e09347dea2be0cf6a17c4a50"; // Replace with your key
    private const string baseUrl = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0";
    private const string personGroupId = "example-group5";

    private static async Task Main(string[] args)
    {
        // Uncomment the required functionality to test it
        
        await CreatePersonGroup();
        await AddImageToGroup("person1", "https://t3.ftcdn.net/jpg/01/97/11/64/360_F_197116416_hpfTtXSoJMvMqU99n6hGP4xX0ejYa4M7.jpg");
        await AddImageToGroup("person2", "https://as2.ftcdn.net/v2/jpg/01/27/72/61/1000_F_127726178_17rLYmSg6jKnxUxCGUOde3vKNfZbuNZm.jpg");
        await AddImageToGroup("person3", "https://img.ebdcdn.com/product/model/portrait/pm0136_m0.jpg?im=Resize,width=400,height=600,aspect=fill;UnsharpMask,sigma=1.0,gain=1.0");
        await AddImageToGroup("lebron1", "https://upload.wikimedia.org/wikipedia/commons/7/7a/LeBron_James_%2851959977144%29_%28cropped2%29.jpg");
        await ListPersonsInGroup();
        await TrainPersonGroup();
        await DetectAndCompareFaces("https://a.espncdn.com/combiner/i?img=/i/headshots/nba/players/full/1966.png");
    }

    private static async Task CreatePersonGroup()
    {
        using (var client = new HttpClient())
        {
            string url = $"{baseUrl}/persongroups/{personGroupId}";

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            var requestBody = new
            {
                name = "Example Group 5",
                userData = "Group for example purposes",
                recognitionModel = "recognition_03"
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PutAsync(url, content);

            Console.WriteLine("Create Person Group Response: " + response);
        }
    }

    private static async Task AddImageToGroup(string personName, string imageUrl)
    {
        using (var client = new HttpClient())
        {
            // Step 1: Create a person
            string createPersonUrl = $"{baseUrl}/persongroups/{personGroupId}/persons";
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            var createPersonBody = new { name = personName };
            var createPersonContent = new StringContent(JsonSerializer.Serialize(createPersonBody), Encoding.UTF8, "application/json");
            var createPersonResponse = await client.PostAsync(createPersonUrl, createPersonContent);

            var createPersonResult = await createPersonResponse.Content.ReadAsStringAsync();
            var personId = JsonDocument.Parse(createPersonResult).RootElement.GetProperty("personId").GetString();

            Console.WriteLine($"Created Person: {personName} with ID {personId}");

            // Step 2: Add face to the person
            string addFaceUrl = $"{baseUrl}/persongroups/{personGroupId}/persons/{personId}/persistedFaces";

            var addFaceBody = new { url = imageUrl };
            var addFaceContent = new StringContent(JsonSerializer.Serialize(addFaceBody), Encoding.UTF8, "application/json");
            var addFaceResponse = await client.PostAsync(addFaceUrl, addFaceContent);

            Console.WriteLine("Add Face Response: " + addFaceResponse.Content.ReadAsStringAsync().Result);
        }
    }

    private static async Task ListPersonsInGroup()
    {
        using (var client = new HttpClient())
        {
            string url = $"{baseUrl}/persongroups/{personGroupId}/persons";
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            var response = await client.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();

            var persons = JsonDocument.Parse(result).RootElement.EnumerateArray();

            Console.WriteLine("Persons in Group: ");

            foreach (var person in persons)
            {
                string personId = person.GetProperty("personId").GetString();
                string name = person.GetProperty("name").GetString();

                Console.WriteLine($"Person: {name} (ID: {personId})");

                // Fetch face attributes for each person
                string getFaceUrl = $"{baseUrl}/persongroups/{personGroupId}/persons/{personId}";
                client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

                var faceResponse = await client.GetAsync(getFaceUrl);
                var faceResult = await faceResponse.Content.ReadAsStringAsync();

                var faces = JsonDocument.Parse(faceResult).RootElement.GetProperty("persistedFaceIds").EnumerateArray();

                foreach (var faceId in faces)
                {
                    Console.WriteLine($"  Face ID: {faceId.GetString()}");
                }
            }
        }
    }

    private static async Task DetectAndCompareFaces(string newImageUrl)
    {
        using (var client = new HttpClient())
        {
            // Step 1: Detect face in the new image
            string detectUrl = $"{baseUrl}/detect";
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["returnFaceAttributes"] = "glasses,occlusion,qualityForRecognition,accessories";
            queryString["returnFaceId"] = "true";
            queryString["returnRecognitionModel"] = "true";
            queryString["recognitionModel"] = "recognition_03";

            string detectFullUrl = $"{detectUrl}?{queryString}";

            var detectBody = new { url = newImageUrl };
            var detectContent = new StringContent(JsonSerializer.Serialize(detectBody), Encoding.UTF8, "application/json");

            var detectResponse = await client.PostAsync(detectFullUrl, detectContent);
            var detectResult = await detectResponse.Content.ReadAsStringAsync();

            var detectedFaceId = JsonDocument.Parse(detectResult).RootElement[0].GetProperty("faceId").GetString();

            Console.WriteLine("Detected Face ID: " + detectedFaceId);
            
            // Pretty-print the JSON response
            try
            {
                var parsedJson = JsonSerializer.Deserialize<object>(detectResult);
                string formattedJson = JsonSerializer.Serialize(parsedJson, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine("Response Msg: ");
                Console.WriteLine(formattedJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to parse JSON response:");
                Console.WriteLine(detectResult);
                Console.WriteLine("Error: " + ex.Message);
            }

            // Step 2: Identify face against the group
            string identifyUrl = $"{baseUrl}/identify";

            var identifyBody = new
            {
                personGroupId = personGroupId,
                faceIds = new[] { detectedFaceId },
                maxNumOfCandidatesReturned = 1,
                confidenceThreshold = 0.5
            };

            var identifyContent = new StringContent(JsonSerializer.Serialize(identifyBody), Encoding.UTF8, "application/json");
            var identifyResponse = await client.PostAsync(identifyUrl, identifyContent);
            var identifyResult = await identifyResponse.Content.ReadAsStringAsync();

            Console.WriteLine("Identify Response: ");
            Console.WriteLine(JsonSerializer.Serialize(JsonDocument.Parse(identifyResult), new JsonSerializerOptions { WriteIndented = true }));
        }
    }
    
    private static async Task TrainPersonGroup()
    {
        using (var client = new HttpClient())
        {
            string url = $"{baseUrl}/persongroups/{personGroupId}/train";

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            var response = await client.PostAsync(url, null);

            Console.WriteLine("Train Person Group Response: " + response.StatusCode);

            // Optionally, check training status
            await CheckTrainingStatus(client);
        }
    }

    private static async Task CheckTrainingStatus(HttpClient client)
    {
        string url = $"{baseUrl}/persongroups/{personGroupId}/training";

        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

        HttpResponseMessage response;
        do
        {
            response = await client.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();
            var status = JsonDocument.Parse(result).RootElement.GetProperty("status").GetString();
            Console.WriteLine("Training Status: " + status);

            if (status == "succeeded" || status == "failed")
                break;

            await Task.Delay(1000); // Wait before rechecking
        } while (true);
    }
}
