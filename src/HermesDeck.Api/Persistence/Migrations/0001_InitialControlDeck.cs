using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HermesDeck.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialControlDeck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Panels",
                columns: table => new
                {
                    PanelId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    TrustLevel = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: true),
                    AllowedActions = table.Column<string[]>(type: "text[]", nullable: false),
                    RenderingMode = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Panels", x => x.PanelId);
                });

            migrationBuilder.CreateTable(
                name: "TelegramUsers",
                columns: table => new
                {
                    TelegramUserId = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: true),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    LanguageCode = table.Column<string>(type: "text", nullable: true),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramUsers", x => x.TelegramUserId);
                });

            migrationBuilder.CreateTable(
                name: "HermesIdentities",
                columns: table => new
                {
                    IdentityId = table.Column<string>(type: "text", nullable: false),
                    TelegramUserId = table.Column<string>(type: "text", nullable: false),
                    WorkspaceId = table.Column<string>(type: "text", nullable: false),
                    Roles = table.Column<string[]>(type: "text[]", nullable: false),
                    Permissions = table.Column<string[]>(type: "text[]", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HermesIdentities", x => x.IdentityId);
                    table.ForeignKey(
                        name: "FK_HermesIdentities_TelegramUsers_TelegramUserId",
                        column: x => x.TelegramUserId,
                        principalTable: "TelegramUsers",
                        principalColumn: "TelegramUserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    ConversationId = table.Column<string>(type: "text", nullable: false),
                    IdentityId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastRunId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.ConversationId);
                    table.ForeignKey(
                        name: "FK_Conversations_HermesIdentities_IdentityId",
                        column: x => x.IdentityId,
                        principalTable: "HermesIdentities",
                        principalColumn: "IdentityId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<string>(type: "text", nullable: false),
                    IdentityId = table.Column<string>(type: "text", nullable: false),
                    TargetType = table.Column<int>(type: "integer", nullable: false),
                    TargetId = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notifications_HermesIdentities_IdentityId",
                        column: x => x.IdentityId,
                        principalTable: "HermesIdentities",
                        principalColumn: "IdentityId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PanelIntents",
                columns: table => new
                {
                    IntentId = table.Column<string>(type: "text", nullable: false),
                    PanelId = table.Column<string>(type: "text", nullable: false),
                    IdentityId = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    PayloadSummary = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DenialReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PanelIntents", x => x.IntentId);
                    table.ForeignKey(
                        name: "FK_PanelIntents_HermesIdentities_IdentityId",
                        column: x => x.IdentityId,
                        principalTable: "HermesIdentities",
                        principalColumn: "IdentityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PanelIntents_Panels_PanelId",
                        column: x => x.PanelId,
                        principalTable: "Panels",
                        principalColumn: "PanelId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    IdentityId = table.Column<string>(type: "text", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LaunchSource = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_Sessions_HermesIdentities_IdentityId",
                        column: x => x.IdentityId,
                        principalTable: "HermesIdentities",
                        principalColumn: "IdentityId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentRuns",
                columns: table => new
                {
                    RunId = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedByIdentityId = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    ReviewReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentRuns", x => x.RunId);
                    table.ForeignKey(
                        name: "FK_AgentRuns_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "ConversationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    MessageId = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<string>(type: "text", nullable: false),
                    SenderType = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StreamState = table.Column<int>(type: "integer", nullable: false),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_Messages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "ConversationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimelineEvents",
                columns: table => new
                {
                    EventId = table.Column<string>(type: "text", nullable: false),
                    RunId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Metadata = table.Column<IReadOnlyDictionary<string, string>>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimelineEvents", x => x.EventId);
                    table.ForeignKey(
                        name: "FK_TimelineEvents_AgentRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "AgentRuns",
                        principalColumn: "RunId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ToolCalls",
                columns: table => new
                {
                    ToolCallId = table.Column<string>(type: "text", nullable: false),
                    RunId = table.Column<string>(type: "text", nullable: false),
                    ToolName = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: true),
                    RiskLevel = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResultSummary = table.Column<string>(type: "text", nullable: true),
                    ErrorSummary = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolCalls", x => x.ToolCallId);
                    table.ForeignKey(
                        name: "FK_ToolCalls_AgentRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "AgentRuns",
                        principalColumn: "RunId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRequests",
                columns: table => new
                {
                    ApprovalId = table.Column<string>(type: "text", nullable: false),
                    RunId = table.Column<string>(type: "text", nullable: false),
                    ToolCallId = table.Column<string>(type: "text", nullable: true),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    ImpactSummary = table.Column<string>(type: "text", nullable: false),
                    AffectedTarget = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRequests", x => x.ApprovalId);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_AgentRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "AgentRuns",
                        principalColumn: "RunId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_ToolCalls_ToolCallId",
                        column: x => x.ToolCallId,
                        principalTable: "ToolCalls",
                        principalColumn: "ToolCallId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalDecisions",
                columns: table => new
                {
                    DecisionId = table.Column<string>(type: "text", nullable: false),
                    ApprovalId = table.Column<string>(type: "text", nullable: false),
                    IdentityId = table.Column<string>(type: "text", nullable: false),
                    Decision = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalDecisions", x => x.DecisionId);
                    table.ForeignKey(
                        name: "FK_ApprovalDecisions_ApprovalRequests_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "ApprovalRequests",
                        principalColumn: "ApprovalId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalDecisions_HermesIdentities_IdentityId",
                        column: x => x.IdentityId,
                        principalTable: "HermesIdentities",
                        principalColumn: "IdentityId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_ConversationId",
                table: "AgentRuns",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_ApprovalId",
                table: "ApprovalDecisions",
                column: "ApprovalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_IdentityId",
                table: "ApprovalDecisions",
                column: "IdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_RunId",
                table: "ApprovalRequests",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_ToolCallId",
                table: "ApprovalRequests",
                column: "ToolCallId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_IdentityId",
                table: "Conversations",
                column: "IdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_HermesIdentities_TelegramUserId",
                table: "HermesIdentities",
                column: "TelegramUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId",
                table: "Messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IdentityId",
                table: "Notifications",
                column: "IdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_PanelIntents_IdentityId",
                table: "PanelIntents",
                column: "IdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_PanelIntents_PanelId",
                table: "PanelIntents",
                column: "PanelId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_IdentityId",
                table: "Sessions",
                column: "IdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEvents_RunId",
                table: "TimelineEvents",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolCalls_RunId",
                table: "ToolCalls",
                column: "RunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalDecisions");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PanelIntents");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "TimelineEvents");

            migrationBuilder.DropTable(
                name: "ApprovalRequests");

            migrationBuilder.DropTable(
                name: "Panels");

            migrationBuilder.DropTable(
                name: "ToolCalls");

            migrationBuilder.DropTable(
                name: "AgentRuns");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "HermesIdentities");

            migrationBuilder.DropTable(
                name: "TelegramUsers");
        }
    }
}
