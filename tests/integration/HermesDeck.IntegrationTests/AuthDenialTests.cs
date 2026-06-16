using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace HermesDeck.IntegrationTests;

/// <summary>
/// T043: Encodes the "auth denial" safety property — unauthenticated requests, and requests
/// with invalid Telegram launch data, must be denied without revealing protected details
/// (conversation content, run/tool state, or stack traces) about the resource being accessed.
/// </summary>
public class AuthDenialTests : IClassFixture<HermesApiFixture>
{
    private readonly HermesApiFixture _fixture;

    public AuthDenialTests(HermesApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Health_Endpoint_Is_Publicly_Reachable()
    {
        // Sanity check that the harness itself boots and responds, independent of auth.
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Unauthenticated_Request_To_Protected_Conversations_Resource_Is_Denied()
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync("/conversations");

        // The endpoint does not exist yet (pre-US1), but the default behavior for any
        // not-yet-authorized/not-yet-existing protected path must never be a bare 200,
        // and must never leak implementation details in the body.
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        AssertNoProtectedDetails(body);
    }

    [Fact]
    public async Task Unauthenticated_Request_To_Protected_Run_Resource_Is_Denied()
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync("/runs/some-run-id");

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        AssertNoProtectedDetails(body);
    }

    [Fact(Skip =
        "Pending T057 (Implement POST /auth/telegram endpoint): requires the real Telegram " +
        "launch validation pipeline to assert 401 on invalid initData without leaking details.")]
    public async Task Invalid_Telegram_Launch_Data_Is_Rejected_With_401_And_No_Details()
    {
        using var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/telegram", new { initData = "tampered-or-expired-payload" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadAsStringAsync();
        AssertNoProtectedDetails(body);
        body.Should().NotContain("TelegramUserId");
        body.Should().NotContain("IdentityId");
    }

    [Fact(Skip =
        "Pending T056/T057 (session token issuing and POST /auth/telegram endpoint): requires " +
        "a real session token format to assert that a missing/garbage bearer token is denied.")]
    public async Task Request_With_Invalid_Session_Token_Is_Denied_Without_Revealing_Resource_State()
    {
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        var response = await client.GetAsync("/conversations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadAsStringAsync();
        AssertNoProtectedDetails(body);
    }

    private static void AssertNoProtectedDetails(string body)
    {
        body.Should().NotContainEquivalentOf("Exception");
        body.Should().NotContainEquivalentOf("StackTrace");
        body.Should().NotContainEquivalentOf("   at ");
        body.Should().NotContainEquivalentOf("System.");
    }
}
