using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using ExamPrepIdeaCenter.Models;

namespace ExamPrepIdeaCenter
{
    [TestFixture]
    public class Tests
    {

        private RestClient client;
        private static string lastCreatedIdeaId;

        private const string BaseUrl = "http://144.91.123.158:82";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiI5ZDVmYTE5Ny03OWQxLTRlZDAtOGI1My0wMjAyOTcxMzNjZDUiLCJpYXQiOiIwNC8xNy8yMDI2IDA1OjU3OjE0IiwiVXNlcklkIjoiM2ViYzAyOWEtYTFlOS00NDAxLTUzYTUtMDhkZTc2YTJkM2VjIiwiRW1haWwiOiJJVERvbnlAZXhhbXBsZS5jb20iLCJVc2VyTmFtZSI6IklURG9ueSIsImV4cCI6MTc3NjQyNzAzNCwiaXNzIjoiSWRlYUNlbnRlcl9BcHBfU29mdFVuaSIsImF1ZCI6IklkZWFDZW50ZXJfV2ViQVBJX1NvZnRVbmkifQ.kM2iGJThRsxzVEFvEHfbVmJQyBDTWu7kNPT0FL-sv0Y";

        private const string LoginEmail = "ITDony@example.com";
        private const string LoginPassword = "ITDony";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;
            // Use static token if provided; otherwise obtain one via authentication.
            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);

        }

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token is not found in the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateIdea_WithRequiredFields_ShouldReturnSuccess()
        {
            var ideaData = new IdeaDTO
            {
                Title = "Test Idea",
                Description = "This is a test idea description.",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaData);

            var response = this.client.Execute(request);

            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));
            // Store created idea id for subsequent tests
            lastCreatedIdeaId = createResponse?.Id;
            if (string.IsNullOrWhiteSpace(lastCreatedIdeaId))
            {
                var allReq = new RestRequest("/api/Idea/All", Method.Get);
                var allResp = this.client.Execute(allReq);
                var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(allResp.Content);
                lastCreatedIdeaId = responseItems?.LastOrDefault()?.Id;
            }

        }

        [Order(2)]
        [Test]
        public void GetAllIdeas_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Idea/All", Method.Get);
            var response = this.client.Execute(request);

            var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(responseItems, Is.Not.Empty);
            Assert.That(responseItems, Is.Not.Null);

            lastCreatedIdeaId = responseItems.LastOrDefault()?.Id;


        }
        

        [Order(3)]
        [Test]
        public void EditTheLastCreatedIdea_ShouldReturnSuccess()
        {
            // Ensure we have an idea id to edit. Create one if not present.
            if (string.IsNullOrWhiteSpace(lastCreatedIdeaId))
            {
                var createIdea = new IdeaDTO
                {
                    Title = "Temp Idea for Edit",
                    Description = "Temporary idea created for edit test.",
                    Url = ""
                };

                var createRequest = new RestRequest("/api/Idea/Create", Method.Post);
                createRequest.AddJsonBody(createIdea);
                var createResponse = this.client.Execute(createRequest);
                Assert.That(createResponse, Is.Not.Null, "Create request returned null response.");
                Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Create request failed: " + (createResponse?.Content ?? "<no content>"));
                var createResponseDto = JsonSerializer.Deserialize<ApiResponseDTO>(createResponse.Content);
                lastCreatedIdeaId = createResponseDto?.Id;
                // If API doesn't return id in create response, fetch all ideas and take the last one.
                if (string.IsNullOrWhiteSpace(lastCreatedIdeaId))
                {
                    var allReq = new RestRequest("/api/Idea/All", Method.Get);
                    var allResp = this.client.Execute(allReq);
                    var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(allResp.Content);
                    lastCreatedIdeaId = responseItems?.LastOrDefault()?.Id;
                }
                Assert.That(lastCreatedIdeaId, Is.Not.Null.And.Not.Empty, "Failed to obtain idea ID for edit test.");
            }

            var editRequestData = new IdeaDTO
            {
                Title = "Updated Test Idea",
                Description = "This is an updated test idea description.",
                Url = ""
            };
            var request = new RestRequest("/api/Idea/Edit", Method.Put);

            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            request.AddJsonBody(editRequestData);

            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            // API returns "Edited successfully"; assert against actual message to avoid brittle test
            Assert.That(editResponse.Msg, Is.EqualTo("Edited successfully"));
            Console.WriteLine("Response Content: " + response.Content);
            Console.WriteLine("Response Status Code: " + response.StatusCode);

        }

        [Order(4)]
        [Test]

        public void DeleteTheLastCreatedIdea_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = this.client.Execute(request);

            
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(response.Content, Is.EqualTo("\"The idea is deleted!\""));
        }

        [Order(5)]
        [Test]
        public void CreateIdea_WithoutTitle_ShouldReturnBadRequest()
        {
            var ideaData = new IdeaDTO
            {
                Title = "",
                Description = "This is a test idea description without title.",
                Url = ""
            };
            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaData);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
        }

        [Order(6)]
        [Test]
        public void EditIdea_WithInvalidId_ShouldReturnBadRequest()
        {
            string invalidIdeaId = "99999999";
            var editRequestData = new IdeaDTO
            {
                Title = "Updated Test Idea with Invalid ID",
                Description = "This is an updated test idea description with invalid ID.",
                Url = ""
            };
            var request = new RestRequest("/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", invalidIdeaId);
            request.AddJsonBody(editRequestData);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
        }

        [Order(7)]
        [Test]
        public void DeleteIdea_WithInvalidId_ShouldReturnBadRequest()
        {
            string invalidIdeaId = "99999999";
            var request = new RestRequest("/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", invalidIdeaId);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
            Assert.That(response.Content, Is.EqualTo("\"There is no such idea!\""));
        }

        [OneTimeTearDown]
            public void TearDown()
                {
                    this.client?.Dispose();
                }
    }
}
