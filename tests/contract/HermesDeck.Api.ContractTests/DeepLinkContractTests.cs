using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace HermesDeck.Api.ContractTests;

/// <summary>
/// Canonical, in-code catalog of the deep-link contract defined in <c>deep-links.md</c>:
/// link shape, allowed target types, resolution statuses, and the no-protected-details
/// security requirement for access-denied / unavailable responses.
/// </summary>
public static class DeepLinkCatalog
{
    public const string LinkShapePattern = @"^/task/(?<targetType>[a-z]+)/(?<targetId>[^/]+)$";

    public static readonly IReadOnlyList<string> TargetTypes =
        ["conversation", "run", "approval", "panel"];

    public static readonly IReadOnlyList<string> ResolutionStatuses =
        ["authorized", "access-denied", "unavailable"];

    public static bool TryParse(string link, out string? targetType, out string? targetId)
    {
        var match = Regex.Match(link, LinkShapePattern);
        if (!match.Success)
        {
            targetType = null;
            targetId = null;
            return false;
        }

        targetType = match.Groups["targetType"].Value;
        targetId = match.Groups["targetId"].Value;
        return true;
    }
}

public class DeepLinkContractTests
{
    private static string ContractText => File.ReadAllText(ContractPaths.GetContractFile("deep-links.md"));

    [Fact]
    public void Contract_ShouldDeclareLinkShape()
    {
        ContractText.Should().Contain("/task/{targetType}/{targetId}");
    }

    [Theory]
    [InlineData("/task/conversation/conv_123", "conversation", "conv_123")]
    [InlineData("/task/run/run_123", "run", "run_123")]
    [InlineData("/task/approval/appr_123", "approval", "appr_123")]
    [InlineData("/task/panel/panel_service_health", "panel", "panel_service_health")]
    public void LinkShape_ShouldParseDocumentedExamples(string link, string expectedTargetType, string expectedTargetId)
    {
        ContractText.Should().Contain(link);

        var parsed = DeepLinkCatalog.TryParse(link, out var targetType, out var targetId);

        parsed.Should().BeTrue();
        targetType.Should().Be(expectedTargetType);
        targetId.Should().Be(expectedTargetId);
    }

    [Theory]
    [InlineData("/task/conversation")]
    [InlineData("/task//conv_123")]
    [InlineData("task/run/run_123")]
    [InlineData("/wrong/run/run_123")]
    public void LinkShape_ShouldRejectMalformedLinks(string link)
    {
        DeepLinkCatalog.TryParse(link, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void Catalog_ShouldDeclareExactlyFourTargetTypes()
    {
        DeepLinkCatalog.TargetTypes.Should().HaveCount(4);
    }

    [Theory]
    [InlineData("conversation")]
    [InlineData("run")]
    [InlineData("approval")]
    [InlineData("panel")]
    public void Contract_ShouldDocumentTargetType(string targetType)
    {
        ContractText.Should().Contain($"`{targetType}`");
    }

    [Fact]
    public void Catalog_ShouldDeclareExactlyThreeResolutionStatuses()
    {
        DeepLinkCatalog.ResolutionStatuses.Should().HaveCount(3);
    }

    [Theory]
    [InlineData("authorized")]
    [InlineData("access-denied")]
    [InlineData("unavailable")]
    public void Contract_ShouldDocumentResolutionStatus(string status)
    {
        ContractText.Should().Contain($"`{status}`");
    }

    [Fact]
    public void Contract_ShouldRequireSessionAndAuthorizationValidationBeforeResolution()
    {
        ContractText.Should().Contain("Hermes API must validate the Telegram launch/session before resolving the target.");
        ContractText.Should().Contain("Hermes API must authorize the current Hermes identity for the target before returning");
    }

    [Fact]
    public void Contract_ShouldRequireNoProtectedDetailsOnAccessDeniedOrUnavailable()
    {
        var normalized = Regex.Replace(ContractText, @"\s+", " ");

        normalized.Should().Contain(
            "Access-denied and unavailable responses must not reveal conversation content, tool arguments, " +
            "affected resources, or approval impact details.");
    }
}
