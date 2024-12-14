using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RestSharp;
using System.Net;

namespace ApiTests
{
    [TestFixture]
    public class DestinationTests : IDisposable
    {
        private RestClient client;
        private string token;

        [SetUp]
        public void Setup()
        {
            client = new RestClient(GlobalConstants.BaseUrl);
            token = GlobalConstants.AuthenticateUser("john.doe@example.com", "password123");

            Assert.That(token, Is.Not.Null.Or.Empty, "Authentication token should not be null or empty");
        }

        [Test]
        public void Test_GetAllDestinations()
        {
            // Arrange
            var getRequest = new RestRequest("destination", Method.Get);

            // Act
            var getResponse = client.Execute(getRequest);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Expected status code is not OK");

                Assert.That(getResponse.Content, Is.Not.Null.Or.Empty, 
                    "Response content is null or empty");

                var destinations = JArray.Parse(getResponse.Content);

                Assert.That(destinations.Type, Is.EqualTo(JTokenType.Array),
                    "The getResponse content is not a JSON array");

                Assert.That(destinations.Count, Is.GreaterThan(0), 
                    "Expected destinations is less than 0");

                foreach (var destination in destinations)
                {
                    Assert.That(destination["name"]?.ToString(), Is.Not.Null.Or.Empty,
                        "Property name is not as expected");

                    Assert.That(destination["location"]?.ToString(), Is.Not.Null.Or.Empty,
                        "Property name is not as expected");

                    Assert.That(destination["description"]?.ToString(), Is.Not.Null.Or.Empty,
                        "Property name is not as expected");

                    Assert.That(destination["category"]?.ToString(), Is.Not.Null.Or.Empty,
                        "Property name is not as expected");

                    Assert.That(destination["attractions"]?.Type, Is.EqualTo(JTokenType.Array),
                        "Atractions property is not an array");

                    Assert.That(destination["bestTimeToVisit"]?.ToString(), Is.Not.Null.Or.Empty,
                        "Property name is not as expected");
                }
            });
        }

        [Test]
        public void Test_GetDestinationByName()
        {
            // Arrange
            var getRequest = new RestRequest("destination", Method.Get);

            // Act
            var getResponse = client.Execute(getRequest);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Response status code is not as expected");

                Assert.That(getResponse.Content, Is.Not.Null.Or.Empty,
                    "Response content is not as expected");

                var destinations = JArray.Parse(getResponse.Content);

                var destination = destinations.FirstOrDefault
                (d => d["name"]?.ToString() == "New York City");

                Assert.That(destination["location"]?.ToString(), Is.EqualTo("New York, USA"),
                    "Location property does not contain correct value");

                Assert.That(destination["description"]?.ToString(), 
                    Is.EqualTo("The largest city in the USA, known for its skyscrapers, culture, and entertainment."),
                    "Destination property does not contain correct value");
            });
        }

        [Test]
        public void Test_AddDestination()
        {
            // Arrange
            // Get all categories and extract first category ID
            var getCategoriesRequest = new RestRequest("category", Method.Get);
            var getCategoriesResponse = client.Execute(getCategoriesRequest);

            var categories = JArray.Parse(getCategoriesResponse.Content);

            var firstCategory = categories.First();
            var categoryId = firstCategory["_id"]?.ToString();

            // Create new destination
            var addRequest = new RestRequest("destination", Method.Post);
            addRequest.AddHeader("Authorization", $"Bearer {token}");
            var name = "Random name";
            var location = "New location";
            var description = "A beautiful beach with crystal clear waters and white sands.";
            var bestTimeToVisit = "April to October";
            var attractions = new[] { "Attraction1", "Attraction2", "Attraction3" };
            addRequest.AddJsonBody(new
            {
                name,
                location,
                description,
                bestTimeToVisit,
                attractions,
                category = categoryId
            });

            // Act
            var addResponse = client.Execute(addRequest);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(addResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), 
                    "Response code is not as expected");
                Assert.That(addResponse.Content, Is.Not.Null.Or.Empty, 
                    "Response content is not as expected");
            });

            var createdDestination = JObject.Parse(addResponse.Content);
            Assert.That(createdDestination["_id"]?.ToString(), Is.Not.Empty, 
                "Created destination didn't have an Id.");

            var createdDestinationId = createdDestination["_id"].ToString();

            // Get destination by ID
            var getDestinationRequest = new RestRequest($"destination/{createdDestinationId}", Method.Get);
            var getResponse = client.Execute(getDestinationRequest);

            Assert.Multiple(() =>
            {
                // Response Assertions
                Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Response code is not as expected");
                Assert.That(getResponse.Content, Is.Not.Null.Or.Empty,
                    "Response content is not as expected");

                // Destination Fields Assertions
                var destination = JObject.Parse(getResponse.Content);

                Assert.That(destination["name"]?.ToString(), Is.EqualTo(name), 
                    "Destination name should match the input.");
                Assert.That(destination["location"]?.ToString(), Is.EqualTo(location), 
                    "Destination location should match the input.");
                Assert.That(destination["description"]?.ToString(), Is.EqualTo(description), 
                    "Destination description should match the input.");
                Assert.That(destination["bestTimeToVisit"]?.ToString(), Is.EqualTo(bestTimeToVisit), 
                    "Destination bestTimeToVisit should match the input.");

                Assert.That(destination["category"]?.ToString(), Is.Not.Null.Or.Empty, 
                    "Destination category should not be null or empty");
                Assert.That(destination["category"]["_id"]?.ToString(), Is.EqualTo(categoryId), 
                    "Destination category should be correct category Id'");

                // The attractions should be a JSON array
                Assert.That(destination["attractions"]?.Type, Is.EqualTo(JTokenType.Array),
                    "Expected Destination attractions content to be a JSON array");

                // The array should have the same number of elements as the input value for attractions
                Assert.That(destination["attractions"].Count, Is.EqualTo(3),
                    "Destination attractions did not have the correct number of elements.");

                // The values of the elements should be the same as the input values for attractions
                Assert.That(destination["attractions"][0]?.ToString(), Is.EqualTo("Attraction1"), 
                    "Destination attractions is missing element");
                Assert.That(destination["attractions"][1]?.ToString(), Is.EqualTo("Attraction2"),
                    "Destination attractions is missing element");
                Assert.That(destination["attractions"][2]?.ToString(), Is.EqualTo("Attraction3"),
                    "Destination attractions is missing element");
            });
        }

        [Test]
        public void Test_UpdateDestination()
        {
            // Arrange
            // Get all destinations and extract with name Machu Picchu
            var getRequest = new RestRequest("destination", Method.Get);
            var getResponse = client.Execute(getRequest);

            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Response code is not as expected");
            Assert.That(getResponse.Content, Is.Not.Null.Or.Empty,
                "Response content is not as expected");

            var destinations = JArray.Parse(getResponse.Content);
            var destinationToUpdate = destinations.FirstOrDefault
                (d => d["name"]?.ToString() == "Machu Picchu");

            Assert.That(destinationToUpdate, Is.Not.Null, 
                "Destination with name 'Machu Picchu' not found");

            // Get the id of the Destination
            var destinationId = destinationToUpdate["_id"]?.ToString();

            // Create update request
            var updateRequest = new RestRequest($"destination/{destinationId}", Method.Put);

            updateRequest.AddHeader("Authorization", $"Bearer {token}");
            updateRequest.AddJsonBody(new
            {
                name = "Updated Name",
                bestTimeToVisit = "Winter",
            });

            // Act
            var updateResponse = client.Execute(updateRequest);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Response code is not as expected");
                Assert.That(updateResponse.Content, Is.Not.Null.Or.Empty,
                    "Response content is not as expected");

                var updatedDestination = JObject.Parse(updateResponse.Content);

                Assert.That(updatedDestination["name"]?.ToString(), Is.EqualTo("Updated Name"), 
                    "Destination name should match the updated value");
                Assert.That(updatedDestination["bestTimeToVisit"]?.ToString(), Is.EqualTo("Winter"), 
                    "Destination best time to visit should match the updated value");
            });
        }

        [Test]
        public void Test_DeleteDestination()
        {
            // Arrange
            // Get all destinations and extract with name Yellowstone National Park
            var getRequest = new RestRequest("destination", Method.Get);
            var getResponse = client.Execute(getRequest);

            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Response code is not as expected");
            Assert.That(getResponse.Content, Is.Not.Null.Or.Empty,
                "Response content is not as expected");

            var destinations = JArray.Parse(getResponse.Content);
            var destinationToDelete = destinations.FirstOrDefault
                (d => d["name"]?.ToString() == "Yellowstone National Park");

            Assert.That(destinationToDelete, Is.Not.Null, 
                "Destination with name 'Yellowstone National Park' not found");

            // Create delete request
            var destinationId = destinationToDelete["_id"]?.ToString();

            var deleteRequest = new RestRequest($"destination/{destinationId}", Method.Delete);
            deleteRequest.AddHeader("Authorization", $"Bearer {token}");

            // Act
            var deleteResponse = client.Execute(deleteRequest);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Response code is not as expected");

                // Get request to get the destination that we deleted
                var verifyGetRequest = new RestRequest($"destination/{destinationId}");

                var verifyGetResponse = client.Execute(verifyGetRequest);

                Assert.That(verifyGetResponse.Content, Is.EqualTo("null"), 
                    "Verify get response content should be empty");
            });
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
