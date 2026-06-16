using FluentAssertions;
using Hermes.Deck.Agent.V1;
using Xunit;

namespace HermesDeck.Api.ContractTests;

/// <summary>
/// Validates the stability of the gRPC <c>AgentService</c> contract defined in
/// <c>proto/agent-service.proto</c> (package <c>hermes.deck.agent.v1</c>). Asserts both against
/// the raw proto text and against the generated <see cref="Hermes.Deck.Agent.V1"/> types via
/// reflection over the protobuf <see cref="Google.Protobuf.Reflection.ServiceDescriptor"/>, so
/// that renaming an rpc, changing its streaming kind, or shifting an enum number is caught.
/// </summary>
public class AgentProtoContractTests
{
    [Fact]
    public void ProtoFile_ShouldDeclareExpectedPackageAndServiceName()
    {
        var text = File.ReadAllText(ContractPaths.ProtoFile);

        text.Should().Contain("package hermes.deck.agent.v1;");
        text.Should().Contain("service AgentService {");
    }

    [Fact]
    public void Service_ShouldHaveExpectedFullyQualifiedName()
    {
        AgentService.Descriptor.FullName.Should().Be("hermes.deck.agent.v1.AgentService");
    }

    [Theory]
    [InlineData("ChatStream", true, true, "ChatStreamRequest", "ChatStreamEvent")]
    [InlineData("GetRunStatus", false, false, "GetRunStatusRequest", "RunStatusResponse")]
    [InlineData("GetTimeline", false, false, "GetTimelineRequest", "TimelineResponse")]
    [InlineData("SubmitApproval", false, false, "SubmitApprovalRequest", "ApprovalResult")]
    [InlineData("SubmitPanelIntent", false, false, "SubmitPanelIntentRequest", "PanelIntentResult")]
    public void Service_ShouldDeclareRpc_WithExpectedStreamingKindAndTypes(
        string rpcName,
        bool expectedClientStreaming,
        bool expectedServerStreaming,
        string expectedInputType,
        string expectedOutputType)
    {
        var method = AgentService.Descriptor.Methods.SingleOrDefault(m => m.Name == rpcName);

        method.Should().NotBeNull($"rpc '{rpcName}' should exist on AgentService");
        method!.IsClientStreaming.Should().Be(expectedClientStreaming);
        method.IsServerStreaming.Should().Be(expectedServerStreaming);
        method.InputType.Name.Should().Be(expectedInputType);
        method.OutputType.Name.Should().Be(expectedOutputType);
    }

    [Fact]
    public void Service_ShouldDeclareExactlyFiveRpcs()
    {
        AgentService.Descriptor.Methods.Should().HaveCount(5);
    }

    [Theory]
    [InlineData(typeof(ChatStreamRequest))]
    [InlineData(typeof(ChatStreamEvent))]
    [InlineData(typeof(GetRunStatusRequest))]
    [InlineData(typeof(RunStatusResponse))]
    [InlineData(typeof(GetTimelineRequest))]
    [InlineData(typeof(TimelineResponse))]
    [InlineData(typeof(TimelineItem))]
    [InlineData(typeof(SubmitApprovalRequest))]
    [InlineData(typeof(ApprovalResult))]
    [InlineData(typeof(ApprovalPrompt))]
    [InlineData(typeof(SubmitPanelIntentRequest))]
    [InlineData(typeof(PanelIntentResult))]
    public void GeneratedMessageType_ShouldExist(Type messageType)
    {
        messageType.Should().Implement(typeof(Google.Protobuf.IMessage));
    }

    [Theory]
    [InlineData(RunStatus.Unspecified, 0)]
    [InlineData(RunStatus.Waiting, 1)]
    [InlineData(RunStatus.Running, 2)]
    [InlineData(RunStatus.ReviewRequired, 3)]
    [InlineData(RunStatus.Completed, 4)]
    [InlineData(RunStatus.Failed, 5)]
    public void RunStatus_ShouldHaveStableNumericValue(RunStatus value, int expectedNumber)
    {
        ((int)value).Should().Be(expectedNumber);
    }

    [Theory]
    [InlineData(ApprovalDecision.Unspecified, 0)]
    [InlineData(ApprovalDecision.Approve, 1)]
    [InlineData(ApprovalDecision.Reject, 2)]
    public void ApprovalDecision_ShouldHaveStableNumericValue(ApprovalDecision value, int expectedNumber)
    {
        ((int)value).Should().Be(expectedNumber);
    }

    [Theory]
    [InlineData(ApprovalStatus.Unspecified, 0)]
    [InlineData(ApprovalStatus.Pending, 1)]
    [InlineData(ApprovalStatus.Approved, 2)]
    [InlineData(ApprovalStatus.Rejected, 3)]
    [InlineData(ApprovalStatus.Expired, 4)]
    [InlineData(ApprovalStatus.Executed, 5)]
    [InlineData(ApprovalStatus.ExecutionFailed, 6)]
    public void ApprovalStatus_ShouldHaveStableNumericValue(ApprovalStatus value, int expectedNumber)
    {
        ((int)value).Should().Be(expectedNumber);
    }

    [Fact]
    public void RunStatus_ShouldHaveExactlySixValues()
    {
        Enum.GetValues<RunStatus>().Should().HaveCount(6);
    }

    [Fact]
    public void ApprovalDecision_ShouldHaveExactlyThreeValues()
    {
        Enum.GetValues<ApprovalDecision>().Should().HaveCount(3);
    }

    [Fact]
    public void ApprovalStatus_ShouldHaveExactlySevenValues()
    {
        Enum.GetValues<ApprovalStatus>().Should().HaveCount(7);
    }
}
