using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillPayment.Infra.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "bill_payment");

            migrationBuilder.CreateTable(
                name: "bill_expectations",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    recurrence = table.Column<int>(type: "integer", nullable: false),
                    expected_due_day = table.Column<int>(type: "integer", nullable: false),
                    observed_lead_days = table.Column<int>(type: "integer", nullable: false),
                    alert_lead_days = table.Column<int>(type: "integer", nullable: false),
                    origin = table.Column<int>(type: "integer", nullable: false),
                    observation_count = table.Column<int>(type: "integer", nullable: false),
                    hint_source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    paused_until = table.Column<DateOnly>(type: "date", nullable: true),
                    deactivation_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bill_expectations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bills",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    rail = table.Column<int>(type: "integer", nullable: false),
                    origin_source_kind = table.Column<int>(type: "integer", nullable: false),
                    origin_source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    origin_sender_address = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    origin_external_message_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    origin_received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    origin_content_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    origin_storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    instruments = table.Column<string>(type: "jsonb", nullable: false),
                    dedup_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    lookup = table.Column<string>(type: "jsonb", nullable: true),
                    pix_lookup = table.Column<string>(type: "jsonb", nullable: true),
                    reading = table.Column<string>(type: "jsonb", nullable: true),
                    extracted_payer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    extracted_payer_tax_id = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    payee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    routing_confidence = table.Column<int>(type: "integer", nullable: true),
                    risk_level = table.Column<int>(type: "integer", nullable: true),
                    lookup_history = table.Column<string>(type: "jsonb", nullable: false),
                    approval_decided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approval_decision = table.Column<int>(type: "integer", nullable: true),
                    approval_decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approval_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    approval_risk_at_decision = table.Column<int>(type: "integer", nullable: true),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    scheduled_for = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bills", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "capture_items",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_message_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    internet_message_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    artifact_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    sender = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    content_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    routing_confidence = table.Column<int>(type: "integer", nullable: true),
                    source_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    unlocked_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    extraction_method = table.Column<int>(type: "integer", nullable: true),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bill_id = table.Column<Guid>(type: "uuid", nullable: true),
                    discarded_of = table.Column<Guid>(type: "uuid", nullable: true),
                    claimed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    claimed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dismissed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    dismissed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    manually_supplied = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    processing_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    lease_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_capture_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "capture_retention_policies",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    window_days = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_capture_retention_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "capture_sources",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    credential_ref = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    capture_since = table.Column<DateOnly>(type: "date", nullable: true),
                    last_sync_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_sync_error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_capture_sources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "captured_messages",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_message_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    internet_message_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    sender = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    first_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    body_storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    body_content_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_captured_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "client_requests",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_dead_letters",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "text", nullable: false),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_dead_letters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payees",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tax_id = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    amount_policy = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    aliases = table.Column<string>(type: "jsonb", nullable: false),
                    accepted_banks = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payees", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payer_profiles",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    legal_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    primary_tax_id = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    additional_tax_ids = table.Column<string>(type: "jsonb", nullable: false),
                    match_by_cnpj_root = table.Column<bool>(type: "boolean", nullable: false),
                    asaas_account_ref = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payer_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "processed_event_log",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_event_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_secrets",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    kek_version = table.Column<int>(type: "integer", nullable: false),
                    wrapped_dek = table.Column<byte[]>(type: "bytea", nullable: false),
                    dek_nonce = table.Column<byte[]>(type: "bytea", nullable: false),
                    dek_tag = table.Column<byte[]>(type: "bytea", nullable: false),
                    ciphertext = table.Column<byte[]>(type: "bytea", nullable: false),
                    nonce = table.Column<byte[]>(type: "bytea", nullable: false),
                    tag = table.Column<byte[]>(type: "bytea", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_secrets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trusted_origins",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    decision = table.Column<int>(type: "integer", nullable: false),
                    decided_by = table.Column<Guid>(type: "uuid", nullable: false),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trusted_origins", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bill_expectation_cycles",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    competence = table.Column<int>(type: "integer", nullable: false),
                    expected_due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    alert_at = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    fulfilled_by_bill_id = table.Column<Guid>(type: "uuid", nullable: true),
                    blocked_by_capture_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    miss_reason = table.Column<int>(type: "integer", nullable: true),
                    waived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    waive_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    alerts = table.Column<string>(type: "jsonb", nullable: false),
                    bill_expectation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bill_expectation_cycles", x => x.id);
                    table.ForeignKey(
                        name: "FK_bill_expectation_cycles_bill_expectations_bill_expectation_~",
                        column: x => x.bill_expectation_id,
                        principalSchema: "bill_payment",
                        principalTable: "bill_expectations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bill_checks",
                schema: "bill_payment",
                columns: table => new
                {
                    type = table.Column<int>(type: "integer", nullable: false),
                    bill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outcome = table.Column<int>(type: "integer", nullable: false),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    evidence = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    evaluated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bill_checks", x => new { x.bill_id, x.type });
                    table.ForeignKey(
                        name: "FK_bill_checks_bills_bill_id",
                        column: x => x.bill_id,
                        principalSchema: "bill_payment",
                        principalTable: "bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "capture_source_folders",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    path = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    sync_cursor = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    last_sync_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_sync_error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    capture_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_capture_source_folders", x => x.id);
                    table.ForeignKey(
                        name: "FK_capture_source_folders_capture_sources_capture_source_id",
                        column: x => x.capture_source_id,
                        principalSchema: "bill_payment",
                        principalTable: "capture_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "captured_message_artifacts",
                schema: "bill_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    artifact_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    content_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    outcome = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    capture_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bill_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    captured_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_captured_message_artifacts", x => x.id);
                    table.ForeignKey(
                        name: "FK_captured_message_artifacts_captured_messages_captured_messa~",
                        column: x => x.captured_message_id,
                        principalSchema: "bill_payment",
                        principalTable: "captured_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bill_expectation_cycles_expectation_competence",
                schema: "bill_payment",
                table: "bill_expectation_cycles",
                columns: new[] { "bill_expectation_id", "competence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bill_expectations_active_updated",
                schema: "bill_payment",
                table: "bill_expectations",
                columns: new[] { "is_active", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_bill_expectations_tenant_payee_account",
                schema: "bill_payment",
                table: "bill_expectations",
                columns: new[] { "tenant_id", "payee_id", "account_reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bills_dedup_key_active",
                schema: "bill_payment",
                table: "bills",
                column: "dedup_key",
                unique: true,
                filter: "\"dedup_key\" IS NOT NULL AND \"status\" NOT IN (5, 9)");

            migrationBuilder.CreateIndex(
                name: "ix_bills_origin_sender",
                schema: "bill_payment",
                table: "bills",
                column: "origin_sender_address");

            migrationBuilder.CreateIndex(
                name: "ix_bills_tenant_created",
                schema: "bill_payment",
                table: "bills",
                columns: new[] { "tenant_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_bills_tenant_due_date",
                schema: "bill_payment",
                table: "bills",
                columns: new[] { "tenant_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_capture_items_tenant_content_hash",
                schema: "bill_payment",
                table: "capture_items",
                columns: new[] { "tenant_id", "content_hash" });

            migrationBuilder.CreateIndex(
                name: "ix_capture_items_tenant_source_message_artifact",
                schema: "bill_payment",
                table: "capture_items",
                columns: new[] { "tenant_id", "source_id", "external_message_id", "artifact_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_capture_items_tenant_status_received",
                schema: "bill_payment",
                table: "capture_items",
                columns: new[] { "tenant_id", "status", "received_at" });

            migrationBuilder.CreateIndex(
                name: "ix_capture_items_worker_queue",
                schema: "bill_payment",
                table: "capture_items",
                columns: new[] { "status", "received_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_capture_retention_policies_tenant",
                schema: "bill_payment",
                table: "capture_retention_policies",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_capture_source_folders_source_path",
                schema: "bill_payment",
                table: "capture_source_folders",
                columns: new[] { "capture_source_id", "path" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_capture_sources_address_global",
                schema: "bill_payment",
                table: "capture_sources",
                column: "address");

            migrationBuilder.CreateIndex(
                name: "ix_capture_sources_enabled_last_sync",
                schema: "bill_payment",
                table: "capture_sources",
                columns: new[] { "is_enabled", "last_sync_at" });

            migrationBuilder.CreateIndex(
                name: "ix_capture_sources_tenant_address",
                schema: "bill_payment",
                table: "capture_sources",
                columns: new[] { "tenant_id", "address" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_captured_message_artifacts_message_key",
                schema: "bill_payment",
                table: "captured_message_artifacts",
                columns: new[] { "captured_message_id", "artifact_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_captured_messages_tenant_received",
                schema: "bill_payment",
                table: "captured_messages",
                columns: new[] { "tenant_id", "received_at", "id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "ix_captured_messages_tenant_source_message",
                schema: "bill_payment",
                table: "captured_messages",
                columns: new[] { "tenant_id", "source_id", "external_message_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_unprocessed",
                schema: "bill_payment",
                table: "outbox_messages",
                column: "created_at",
                filter: "processed = false");

            migrationBuilder.CreateIndex(
                name: "ix_payees_tax_id_global",
                schema: "bill_payment",
                table: "payees",
                column: "tax_id");

            migrationBuilder.CreateIndex(
                name: "ix_payees_tenant_tax_id",
                schema: "bill_payment",
                table: "payees",
                columns: new[] { "tenant_id", "tax_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payer_profiles_tenant",
                schema: "bill_payment",
                table: "payer_profiles",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_secrets_tenant_kind",
                schema: "bill_payment",
                table: "tenant_secrets",
                columns: new[] { "tenant_id", "kind" });

            migrationBuilder.CreateIndex(
                name: "ix_trusted_origins_tenant_kind_value",
                schema: "bill_payment",
                table: "trusted_origins",
                columns: new[] { "tenant_id", "kind", "value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trusted_origins_tenant_value",
                schema: "bill_payment",
                table: "trusted_origins",
                columns: new[] { "tenant_id", "value" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bill_checks",
                schema: "bill_payment");

            migrationBuilder.DropTable(
                name: "bill_expectation_cycles",
                schema: "bill_payment");

            migrationBuilder.DropTable(
                name: "capture_items",
                schema: "bill_payment");

            migrationBuilder.DropTable(
                name: "capture_retention_policies",
                schema: "bill_payment");

            migrationBuilder.DropTable(
                name: "capture_source_folders",
                schema: "bill_payment");

            migrationBuilder.DropTable(
                name: "captured_message_artifacts",
                schema: "bill_payment");

            migrationBuilder.DropTable(
                name: "client_requests",
                schema: "bill_payment");

            migrationBuilder.DropTable(
                name: "outbox_dead_letters",
                schema: "bill_payment");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "bill_payment");

            migrationBuilder.DropTable(
                name: "payees",
                schema: "bill_payment");

            migrationBuilder.DropTable(
                name: "payer_profiles",
                schema: "bill_payment");

            migrationBuilder.DropTable(
                name: "processed_event_log",
                schema: "bill_payment");

            migrationBuilder.DropTable(
                name: "tenant_secrets",
                schema: "bill_payment");

            migrationBuilder.DropTable(
                name: "trusted_origins",
                schema: "bill_payment");

            migrationBuilder.DropTable(
                name: "bills",
                schema: "bill_payment");

            migrationBuilder.DropTable(
                name: "bill_expectations",
                schema: "bill_payment");

            migrationBuilder.DropTable(
                name: "capture_sources",
                schema: "bill_payment");

            migrationBuilder.DropTable(
                name: "captured_messages",
                schema: "bill_payment");
        }
    }
}
