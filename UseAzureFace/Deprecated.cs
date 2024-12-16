using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;

internal class Deprecated {
    private const string imageUrl =
        "https://d2ubrtwy6ww54e.cloudfront.net/www.uvmhealth.org/assets/2020-11/uvmhn-staying-healthy-coronavirus-man-wearing-mask.jpg?VersionId=J4Kw2bZmworjom6E_Jo_3CPV2CFyOhYY"; 
    
    private static async Task DeprecatedMain(string[] args)
    {
        await DetectFacesWithAttributes();
    }

    private static async Task DetectFacesWithAttributes()
    {
        using (var client = new HttpClient())
        {
            // Define the Face API base URL
            string baseUrl = "https://westcentralus.api.cognitive.microsoft.com/face/v1.2-preview.1";
            string resourcePath = "/detect";
            
            // Query parameters for the Face API
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["returnFaceAttributes"] = "glasses,occlusion,qualityForRecognition,accessories";
            queryString["returnFaceId"] = "true";
            queryString["returnRecognitionModel"] = "true";
            queryString["recognitionModel"] = "recognition_03";

            // Combine the base URL, resource path, and query parameters
            string fullUrl = $"{baseUrl}{resourcePath}?{queryString}";

            // Add the subscription key to the request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "bcfb24b0e09347dea2be0cf6a17c4a50");

            // Request body: Specify the online image URL
            var requestBody = new
            {
                url = imageUrl // Replace with your online image URL
            };
            var jsonBody = JsonSerializer.Serialize(requestBody);

            // Create the HTTP request
            using (var content = new StringContent(jsonBody))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                // Send the POST request to the Face API
                HttpResponseMessage response = await client.PostAsync(fullUrl, content);

                // Handle the response
                string result = await response.Content.ReadAsStringAsync();

                // Pretty-print the JSON response
                try
                {
                    var parsedJson = JsonSerializer.Deserialize<object>(result);
                    string formattedJson = JsonSerializer.Serialize(parsedJson, new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine("HttpResponse: " + response.StatusCode);
                    Console.WriteLine("Response Msg: ");
                    Console.WriteLine(formattedJson);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to parse JSON response:");
                    Console.WriteLine(result);
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }
    }
}