using System.Net;
using System.Net.Http.Json;

using landing_page_backend.DTOs;

namespace landing_page_backend.Tests.Integration.Controllers;

public class MediaFileControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public MediaFileControllerIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsSuccessStatusCode()
    {
        var response = await _client.GetAsync("/api/MediaFile");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithKeyword_ReturnsFilteredResults()
    {
        var response = await _client.GetAsync("/api/MediaFile?keyword=test");

        response.EnsureSuccessStatusCode();
        var files = await response.Content.ReadFromJsonAsync<List<MediaFileResponseDto>>();
        Assert.NotNull(files);
    }

    [Fact]
    public async Task GetAll_WithHash_ReturnsFilteredResults()
    {
        var response = await _client.GetAsync("/api/MediaFile?hash=abc123");

        response.EnsureSuccessStatusCode();
        var files = await response.Content.ReadFromJsonAsync<List<MediaFileResponseDto>>();
        Assert.NotNull(files);
    }

    [Fact]
    public async Task Upload_WithValidImage_ReturnsCreatedResult()
    {
        var content = new MultipartFormDataContent();
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
        var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "test-image.jpg");
        content.Add(new StringContent("Test description"), "description");

        var response = await _client.PostAsync("/api/MediaFile", content);

        Assert.True(response.IsSuccessStatusCode);
        var result = await response.Content.ReadFromJsonAsync<MediaFileResponseDto>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.NotEmpty(result.StoragePath);
        Assert.NotEmpty(result.PublicUrl);
        Assert.NotEmpty(result.Hash);
    }

    [Fact]
    public async Task Upload_WithoutFile_ReturnsBadRequest()
    {
        var content = new MultipartFormDataContent();

        var response = await _client.PostAsync("/api/MediaFile", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithExistingId_ReturnsFile()
    {
        var uploadContent = new MultipartFormDataContent();
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
        var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(fileContent, "file", "get-test.jpg");
        var uploadResponse = await _client.PostAsync("/api/MediaFile", uploadContent);
        var uploadedFile = await uploadResponse.Content.ReadFromJsonAsync<MediaFileResponseDto>();

        var response = await _client.GetAsync($"/api/MediaFile/{uploadedFile!.Id}");

        response.EnsureSuccessStatusCode();
        var file = await response.Content.ReadFromJsonAsync<MediaFileResponseDto>();
        Assert.NotNull(file);
        Assert.Equal(uploadedFile.Id, file.Id);
    }

    [Fact]
    public async Task GetById_WithNonExistingId_ReturnsNotFound()
    {
        var nonExistingId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/MediaFile/{nonExistingId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDescription_WithExistingId_ReturnsNoContent()
    {
        var uploadContent = new MultipartFormDataContent();
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
        var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(fileContent, "file", "update-test.jpg");
        var uploadResponse = await _client.PostAsync("/api/MediaFile", uploadContent);
        var uploadedFile = await uploadResponse.Content.ReadFromJsonAsync<MediaFileResponseDto>();

        var updateRequest = new UpdateMediaFileDescriptionRequestDto
        {
            Description = "Updated description"
        };
        var response = await _client.PatchAsJsonAsync($"/api/MediaFile/{uploadedFile!.Id}/description", updateRequest);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/MediaFile/{uploadedFile.Id}");
        var updatedFile = await getResponse.Content.ReadFromJsonAsync<MediaFileResponseDto>();
        Assert.Equal(updateRequest.Description, updatedFile!.Description);
    }

    [Fact]
    public async Task UpdateDescription_WithNonExistingId_ReturnsNotFound()
    {
        var nonExistingId = Guid.NewGuid();
        var updateRequest = new UpdateMediaFileDescriptionRequestDto
        {
            Description = "Should Fail"
        };

        var response = await _client.PatchAsJsonAsync($"/api/MediaFile/{nonExistingId}/description", updateRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithExistingId_ReturnsNoContent()
    {
        var uploadContent = new MultipartFormDataContent();
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
        var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(fileContent, "file", "delete-test.jpg");
        var uploadResponse = await _client.PostAsync("/api/MediaFile", uploadContent);
        var uploadedFile = await uploadResponse.Content.ReadFromJsonAsync<MediaFileResponseDto>();

        var response = await _client.DeleteAsync($"/api/MediaFile/{uploadedFile!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/MediaFile/{uploadedFile.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WithNonExistingId_ReturnsNotFound()
    {
        var nonExistingId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"/api/MediaFile/{nonExistingId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Upload_DuplicateFile_ReturnsExistingFile()
    {
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };

        var content1 = new MultipartFormDataContent();
        var fileContent1 = new ByteArrayContent(imageBytes);
        fileContent1.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content1.Add(fileContent1, "file", "duplicate.jpg");
        var response1 = await _client.PostAsync("/api/MediaFile", content1);
        var file1 = await response1.Content.ReadFromJsonAsync<MediaFileResponseDto>();

        var content2 = new MultipartFormDataContent();
        var fileContent2 = new ByteArrayContent(imageBytes);
        fileContent2.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content2.Add(fileContent2, "file", "duplicate-copy.jpg");
        var response2 = await _client.PostAsync("/api/MediaFile", content2);
        var file2 = await response2.Content.ReadFromJsonAsync<MediaFileResponseDto>();

        Assert.True(response1.IsSuccessStatusCode);
        Assert.True(response2.IsSuccessStatusCode);
        Assert.NotNull(file1);
        Assert.NotNull(file2);
        Assert.Equal(file1.Hash, file2.Hash);
    }
}
