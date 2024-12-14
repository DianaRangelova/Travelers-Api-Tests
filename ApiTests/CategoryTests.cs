using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using RestSharp;
using System.Net;

namespace ApiTests
{
    [TestFixture]
    public class CategoryTests : IDisposable
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
        public void Test_CategoryLifecycle()
        {

            // Step 1: Create a new category
            var createCategoryRequest = new RestRequest("/category", Method.Post);
            createCategoryRequest.AddHeader("Authorization", $"Bearer {token}");

            createCategoryRequest.AddJsonBody(new
            {
                name = "Test Category"
            });

            var createResponse = client.Execute(createCategoryRequest);

            // Assert
            Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "Expected status code is not OK");

            var createdCategory = JObject.Parse(createResponse.Content);
            string categoryId = createdCategory["_id"]?.ToString();

            Assert.That(categoryId, Is.Not.Null.Or.Empty, 
                "Category ID should not be null or empty");

            // Step 2: Get all categories
            var getAllCategories = new RestRequest("category", Method.Get);
            var getAllCategoriesResponse = client.Execute(getAllCategories);

            Assert.Multiple(() =>
            {
                Assert.That(getAllCategoriesResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Expected status code is not OK");
                Assert.That(getAllCategoriesResponse.Content, Is.Not.Empty, 
                   "Response content should not be empty");

                var categories = JArray.Parse(getAllCategoriesResponse.Content);

                Assert.That(categories.Type, Is.EqualTo(JTokenType.Array), 
                    "Expected response content to be a JSON array");
                Assert.That(categories.Count, Is.GreaterThan(0), 
                    "Expected at least one category in the response");

                var createdCategory = categories.FirstOrDefault(c => c 
                    ["name"]?.ToString() == "Test Category");
                Assert.That(createdCategory, Is.Not.Null);
            });

            // Step 3: Get category by ID
            var getCategoryByIdRequest = new RestRequest($"category/{categoryId}", Method.Get);
            var getCategoryByIdRequestResponse = client.Execute(getCategoryByIdRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getCategoryByIdRequestResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Expected status code is not OK");
                Assert.That(getCategoryByIdRequestResponse.Content, Is.Not.Empty, 
                    "Response content should not be empty");

                var category = JObject.Parse(getCategoryByIdRequestResponse.Content);

                Assert.That(category["_id"]?.ToString(), Is.EqualTo(categoryId), 
                    "Expected the category ID to match");
                Assert.That(category["name"]?.ToString(), Is.EqualTo("Test Category"), 
                    "Expected the category name to match");
            });

            // Step 4: Edit the category
            var editRequest = new RestRequest($"category/{categoryId}", Method.Put);
            editRequest.AddHeader("Authorization", $"Bearer {token}");
            editRequest.AddJsonBody(new
            {
                name = "Updated Test Category"
            });

            var editResponse = client.Execute(editRequest);
            Assert.That(editResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "Expected status code is not OK");

            // Step 5: Verify the category is updated
            var getUpdatedCategoryRequest = new RestRequest($"category/{categoryId}", Method.Get);
            var getUpdatedCategoryResponse = client.Execute(getUpdatedCategoryRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getUpdatedCategoryResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Expected status code is not OK");
                Assert.That(getUpdatedCategoryResponse.Content, Is.Not.Empty, 
                    "Response content should not be empty");

                var updatedCategory = JObject.Parse(getUpdatedCategoryResponse.Content);
                Assert.That(updatedCategory["name"]?.ToString(), Is.EqualTo("Updated Test Category"), 
                    "Expected the updated category name to match");
            });

            // Step 6: Delete the category
            var deleteRequest = new RestRequest($"category/{categoryId}", Method.Delete);
            deleteRequest.AddHeader("Authorization", $"Bearer {token}");

            var deleteResponse = client.Execute(deleteRequest);
            Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "Expected status code is not OK");

            // Step 7: Verify that the deleted category cannot be found
            var getDeletedCategoryRequest = new RestRequest($"category/{categoryId}", Method.Get);
            var getDeletedCategoryResponse = client.Execute(getDeletedCategoryRequest);

            Assert.That(getDeletedCategoryResponse.Content, Is.Empty.Or.EqualTo("null"), 
                "Deleted category should not be found");
    }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
