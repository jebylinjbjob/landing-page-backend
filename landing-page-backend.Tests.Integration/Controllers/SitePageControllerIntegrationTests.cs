using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

using landing_page_backend.DTOs;

namespace landing_page_backend.Tests.Integration.Controllers;

public class SitePageControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SitePageControllerIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsSuccessStatusCode()
    {
        var response = await _client.GetAsync("/api/SitePage");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithKeyword_ReturnsFilteredResults()
    {
        var response = await _client.GetAsync("/api/SitePage?keyword=test");

        response.EnsureSuccessStatusCode();
        var pages = await response.Content.ReadFromJsonAsync<List<SitePageResponseDto>>();
        Assert.NotNull(pages);
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedResult()
    {
        var request = new CreateSitePageRequestDto
        {
            ContentJson = "{\"title\":\"Test Page\",\"content\":\"Test Content\"}",
            Name = "Test Page"
        };

        var response = await _client.PostAsJsonAsync("/api/SitePage", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdPage = await response.Content.ReadFromJsonAsync<SitePageResponseDto>();
        Assert.NotNull(createdPage);
        Assert.NotEqual(Guid.Empty, createdPage.Id);
        Assert.Equal(request.ContentJson, createdPage.ContentJson);
    }

    [Fact]
    public async Task Create_WithInvalidData_ReturnsBadRequest()
    {
        var request = new CreateSitePageRequestDto
        {
            ContentJson = "x",
            Name = "x"
        };

        var response = await _client.PostAsJsonAsync("/api/SitePage", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithExistingId_ReturnsPage()
    {
        var createRequest = new CreateSitePageRequestDto
        {
            ContentJson = "{\"title\":\"Get By Id Test\"}",
            Name = "Get By Id Test"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/SitePage", createRequest);
        var createdPage = await createResponse.Content.ReadFromJsonAsync<SitePageResponseDto>();

        var response = await _client.GetAsync($"/api/SitePage/{createdPage!.Id}");

        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<SitePageResponseDto>();
        Assert.NotNull(page);
        Assert.Equal(createdPage.Id, page.Id);
        Assert.Equal(createRequest.ContentJson, page.ContentJson);
    }

    [Fact]
    public async Task GetById_WithNonExistingId_ReturnsNotFound()
    {
        var nonExistingId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/SitePage/{nonExistingId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithExistingId_ReturnsNoContent()
    {
        var createRequest = new CreateSitePageRequestDto
        {
            ContentJson = "{\"title\":\"Original\"}",
            Name = "Original"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/SitePage", createRequest);
        var createdPage = await createResponse.Content.ReadFromJsonAsync<SitePageResponseDto>();

        var updateRequest = new UpdateSitePageRequestDto
        {
            ContentJson = "{\"title\":\"Updated\"}",
            Name = "Updated"
        };
        var response = await _client.PutAsJsonAsync($"/api/SitePage/{createdPage!.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/SitePage/{createdPage.Id}");
        var updatedPage = await getResponse.Content.ReadFromJsonAsync<SitePageResponseDto>();
        Assert.Equal(updateRequest.ContentJson, updatedPage!.ContentJson);
    }

    [Fact]
    public async Task Update_WithNonExistingId_ReturnsNotFound()
    {
        var nonExistingId = Guid.NewGuid();
        var updateRequest = new UpdateSitePageRequestDto
        {
            ContentJson = "{\"title\":\"Should Fail\"}",
            Name = "Should Fail"
        };

        var response = await _client.PutAsJsonAsync($"/api/SitePage/{nonExistingId}", updateRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithExistingId_ReturnsNoContent()
    {
        var createRequest = new CreateSitePageRequestDto
        {
            ContentJson = "{\"title\":\"To Delete\"}",
            Name = "To Delete"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/SitePage", createRequest);
        var createdPage = await createResponse.Content.ReadFromJsonAsync<SitePageResponseDto>();

        var response = await _client.DeleteAsync($"/api/SitePage/{createdPage!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/SitePage/{createdPage.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WithNonExistingId_ReturnsNotFound()
    {
        var nonExistingId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"/api/SitePage/{nonExistingId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLandingPage_WhenNothingPublished_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/SitePage/GetLandingPage");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLandingPage_WhenPublished_ReturnsContentJsonBody()
    {
        var contentJson = "{\"title\":\"Landing\",\"blocks\":[]}";
        var createRequest = new CreateSitePageRequestDto
        {
            ContentJson = contentJson,
            Name = "Landing"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/SitePage", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<SitePageResponseDto>();

        var publishResponse = await _client.PutAsync(
            $"/api/SitePage/{created!.Id}/publish?isPublished=true",
            content: null);
        publishResponse.EnsureSuccessStatusCode();

        var response = await _client.GetAsync("/api/SitePage/GetLandingPage");

        response.EnsureSuccessStatusCode();
        var raw = await response.Content.ReadAsStringAsync();
        AssertJsonPayloadEquals(contentJson, raw);
    }

    [Fact]
    public async Task Publish_WithExistingId_PublishesAndGetLandingPageReturnsContent()
    {
        var createRequest = new CreateSitePageRequestDto
        {
            ContentJson = "{\"slug\":\"p1\"}",
            Name = "P1"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/SitePage", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<SitePageResponseDto>();

        var response = await _client.PutAsync(
            $"/api/SitePage/{created!.Id}/publish?isPublished=true",
            content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/SitePage/{created.Id}");
        var page = await getResponse.Content.ReadFromJsonAsync<SitePageResponseDto>();
        Assert.NotNull(page);
        Assert.True(page.IsPublished);
    }

    [Fact]
    public async Task Publish_WithNonExistingId_ReturnsNotFound()
    {
        var response = await _client.PutAsync(
            $"/api/SitePage/{Guid.NewGuid()}/publish?isPublished=true",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Publish_WithIsPublishedFalse_Unpublishes()
    {
        var createRequest = new CreateSitePageRequestDto
        {
            ContentJson = "{}",
            Name = "To Unpublish"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/SitePage", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<SitePageResponseDto>();

        await _client.PutAsync($"/api/SitePage/{created!.Id}/publish?isPublished=true", content: null);

        var unpublishResponse = await _client.PutAsync(
            $"/api/SitePage/{created.Id}/publish?isPublished=false",
            content: null);

        Assert.Equal(HttpStatusCode.NoContent, unpublishResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/SitePage/{created.Id}");
        var page = await getResponse.Content.ReadFromJsonAsync<SitePageResponseDto>();
        Assert.NotNull(page);
        Assert.False(page.IsPublished);

        var landingResponse = await _client.GetAsync("/api/SitePage/GetLandingPage");
        Assert.Equal(HttpStatusCode.NotFound, landingResponse.StatusCode);
    }

    [Fact]
    public async Task Publish_WhenPublishingNewPage_UnpublishesPreviouslyPublished()
    {
        var first = await _client.PostAsJsonAsync(
            "/api/SitePage",
            new CreateSitePageRequestDto { ContentJson = "{\"id\":1}", Name = "First" });
        var firstPage = await first.Content.ReadFromJsonAsync<SitePageResponseDto>();
        await _client.PutAsync($"/api/SitePage/{firstPage!.Id}/publish?isPublished=true", content: null);

        var second = await _client.PostAsJsonAsync(
            "/api/SitePage",
            new CreateSitePageRequestDto { ContentJson = "{\"id\":2}", Name = "Second" });
        var secondPage = await second.Content.ReadFromJsonAsync<SitePageResponseDto>();
        await _client.PutAsync($"/api/SitePage/{secondPage!.Id}/publish?isPublished=true", content: null);

        var firstAgain = await _client.GetAsync($"/api/SitePage/{firstPage.Id}");
        var firstDto = await firstAgain.Content.ReadFromJsonAsync<SitePageResponseDto>();
        Assert.NotNull(firstDto);
        Assert.False(firstDto.IsPublished);

        var landing = await _client.GetAsync("/api/SitePage/GetLandingPage");
        landing.EnsureSuccessStatusCode();
        var landingRaw = await landing.Content.ReadAsStringAsync();
        AssertJsonPayloadEquals("{\"id\":2}", landingRaw);
    }

    /// <summary>
    /// GetLandingPage 回傳 Ok(ContentJson)：依序列化設定，本體可能是 JSON 字串或已解析的 JSON 物件，兩者皆視為與預期內容等價。
    /// </summary>
    private static void AssertJsonPayloadEquals(string expectedJson, string responseBody)
    {
        var expected = JsonNode.Parse(expectedJson);
        var root = JsonNode.Parse(responseBody);
        if (root is JsonValue jv && jv.TryGetValue<string>(out var innerJson))
        {
            root = JsonNode.Parse(innerJson);
        }

        Assert.True(JsonNode.DeepEquals(expected, root), $"expected: {expected}, actual: {root}");
    }
}
