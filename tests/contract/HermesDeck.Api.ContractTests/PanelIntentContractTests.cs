using FluentAssertions;
using Xunit;

namespace HermesDeck.Api.ContractTests;

/// <summary>
/// Canonical, in-code catalog of the panel-intent contract defined in <c>panel-intents.md</c>:
/// trust levels, intent statuses, and the rule that <c>untrusted-external</c> panels are
/// non-interactive (interactive intents must be denied). Intended for reuse by later
/// story-level tests; the tests below assert the catalog matches the markdown contract.
/// </summary>
public static class PanelIntentCatalog
{
    public static readonly IReadOnlyList<string> TrustLevels =
        ["native", "internal-mcp-app", "trusted-external-mcp-app", "untrusted-external"];

    public static readonly IReadOnlyList<string> IntentStatuses =
        ["submitted", "validated", "denied", "pending-approval", "executed", "rejected"];

    public const string NonInteractiveTrustLevel = "untrusted-external";

    public static bool AllowsInteractiveIntents(string trustLevel) => trustLevel != NonInteractiveTrustLevel;
}

public class PanelIntentContractTests
{
    private static string ContractText => File.ReadAllText(ContractPaths.GetContractFile("panel-intents.md"));

    [Fact]
    public void Catalog_ShouldDeclareExactlyFourTrustLevels()
    {
        PanelIntentCatalog.TrustLevels.Should().HaveCount(4);
    }

    [Theory]
    [InlineData("native")]
    [InlineData("internal-mcp-app")]
    [InlineData("trusted-external-mcp-app")]
    [InlineData("untrusted-external")]
    public void Contract_ShouldDocumentTrustLevel(string trustLevel)
    {
        ContractText.Should().Contain($"`{trustLevel}`");
    }

    [Fact]
    public void Catalog_ShouldDeclareExactlySixIntentStatuses()
    {
        PanelIntentCatalog.IntentStatuses.Should().HaveCount(6);
    }

    [Theory]
    [InlineData("submitted")]
    [InlineData("validated")]
    [InlineData("denied")]
    [InlineData("pending-approval")]
    [InlineData("executed")]
    [InlineData("rejected")]
    public void Contract_ShouldDocumentIntentStatus(string status)
    {
        ContractText.Should().Contain($"`{status}`");
    }

    [Fact]
    public void UntrustedExternal_ShouldNotAllowInteractiveIntents()
    {
        PanelIntentCatalog.AllowsInteractiveIntents("untrusted-external").Should().BeFalse();
    }

    [Theory]
    [InlineData("native")]
    [InlineData("internal-mcp-app")]
    [InlineData("trusted-external-mcp-app")]
    public void OtherTrustLevels_ShouldAllowInteractiveIntents(string trustLevel)
    {
        PanelIntentCatalog.AllowsInteractiveIntents(trustLevel).Should().BeTrue();
    }

    [Fact]
    public void Contract_ShouldStateUntrustedExternalIsNonInteractive()
    {
        ContractText.Should().Contain("`untrusted-external`: Rendered as text or JSON only. Interactive intents are denied.");
    }

    [Fact]
    public void Contract_ShouldDocumentSevenValidationRules()
    {
        // The numbered list "1." through "7." anchors the rule count; if a rule is added or
        // removed this assertion (and the validation-rule-content checks below) must change too.
        for (var i = 1; i <= 7; i++)
        {
            ContractText.Should().Contain($"{i}. ");
        }

        ContractText.Should().NotContain("8. ");
    }

    [Fact]
    public void Contract_ShouldRequireHostValidationBeforePrivilegedActions()
    {
        ContractText.Should().Contain("the host validates it before any tool or operational action is performed");
    }

    [Fact]
    public void Contract_ShouldRequireDenialReasonsWithoutProtectedInternals()
    {
        ContractText.Should().Contain("Denied intents must return a user-readable denial reason without protected internals.");
    }

    [Fact]
    public void Contract_ShouldRequireApprovalForSensitiveActions()
    {
        ContractText.Should().Contain("Sensitive actions must create an approval request before execution.");
    }
}
