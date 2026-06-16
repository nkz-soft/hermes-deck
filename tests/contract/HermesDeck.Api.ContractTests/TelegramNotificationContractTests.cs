using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace HermesDeck.Api.ContractTests;

/// <summary>
/// Canonical, in-code catalog of the Telegram notification contract defined in
/// <c>telegram-notifications.md</c>: notification reasons, required payload fields, allowed
/// target types, statuses, and the no-protected-details delivery rule.
/// </summary>
public static class TelegramNotificationCatalog
{
    public static readonly IReadOnlyList<string> Reasons =
        ["approval-requested", "review-required", "important-result"];

    public static readonly IReadOnlyList<string> RequiredPayloadFields =
        ["notificationId", "targetType", "targetId", "reason", "message", "deepLink"];

    public static readonly IReadOnlyList<string> TargetTypes =
        ["conversation", "run", "approval", "panel"];

    public static readonly IReadOnlyList<string> Statuses =
        ["created", "sent", "opened", "expired", "failed", "unauthorized"];
}

public class TelegramNotificationContractTests
{
    private static string ContractText => File.ReadAllText(ContractPaths.GetContractFile("telegram-notifications.md"));

    [Fact]
    public void Catalog_ShouldDeclareExactlyThreeReasons()
    {
        TelegramNotificationCatalog.Reasons.Should().HaveCount(3);
    }

    [Theory]
    [InlineData("approval-requested")]
    [InlineData("review-required")]
    [InlineData("important-result")]
    public void Contract_ShouldDocumentReason(string reason)
    {
        ContractText.Should().Contain($"`{reason}`");
    }

    [Fact]
    public void Catalog_ShouldDeclareExactlySixRequiredPayloadFields()
    {
        TelegramNotificationCatalog.RequiredPayloadFields.Should().HaveCount(6);
    }

    [Theory]
    [InlineData("notificationId")]
    [InlineData("targetType")]
    [InlineData("targetId")]
    [InlineData("reason")]
    [InlineData("message")]
    [InlineData("deepLink")]
    public void Contract_ShouldDeclareRequiredPayloadField(string field)
    {
        ContractText.Should().Contain($"`{field}`");
    }

    [Fact]
    public void Contract_ShouldStateRequiredFieldsSentence()
    {
        // The contract's "Required fields:" sentence is the authoritative source for the
        // required-field set; assert it lists exactly our six fields (allowing the line wrap
        // present in the markdown source).
        var normalized = Regex.Replace(ContractText, @"\s+", " ");
        normalized.Should().Contain(
            "Required fields: `notificationId`, `targetType`, `targetId`, `reason`, `message`, `deepLink`.");
    }

    [Fact]
    public void Catalog_ShouldDeclareExactlyFourTargetTypes()
    {
        TelegramNotificationCatalog.TargetTypes.Should().HaveCount(4);
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
    public void Catalog_ShouldDeclareExactlySixStatuses()
    {
        TelegramNotificationCatalog.Statuses.Should().HaveCount(6);
    }

    [Theory]
    [InlineData("created")]
    [InlineData("sent")]
    [InlineData("opened")]
    [InlineData("expired")]
    [InlineData("failed")]
    [InlineData("unauthorized")]
    public void Contract_ShouldDocumentStatus(string status)
    {
        ContractText.Should().Contain($"`{status}`");
    }

    [Fact]
    public void Contract_ShouldRequireNoProtectedDetailsInPreview()
    {
        var normalized = Regex.Replace(ContractText, @"\s+", " ");
        normalized.Should().Contain(
            "Notification text must not include protected task details that would be unsafe in a Telegram notification preview.");
    }

    [Fact]
    public void Contract_ShouldRequireRevalidationOnOpen()
    {
        var normalized = Regex.Replace(ContractText, @"\s+", " ");
        normalized.Should().Contain(
            "Opening a notification must revalidate the current Telegram user, session, and target authorization before showing the target.");
    }
}
