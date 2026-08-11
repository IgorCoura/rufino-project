using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillPayment.Infra.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "bill_payment");

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
                    origin_content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    origin_storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    instruments = table.Column<string>(type: "jsonb", nullable: false),
                    dedup_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    lookup = table.Column<string>(type: "jsonb", nullable: true),
                    pix_lookup = table.Column<string>(type: "jsonb", nullable: true),
                    extracted_payer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    extracted_payer_tax_id = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    payee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    routing_confidence = table.Column<int>(type: "integer", nullable: true),
                    lookup_history = table.Column<string>(type: "jsonb", nullable: false),
                    approval_decided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approval_decision = table.Column<int>(type: "integer", nullable: true),
                    approval_decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approval_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    artifact_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
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
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_capture_items", x => x.id);
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
                name: "ix_outbox_messages_unprocessed",
                schema: "bill_payment",
                table: "outbox_messages",
                column: "created_at",
                filter: "processed = false");

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
                name: "capture_items",
                schema: "bill_payment");

            migrationBuilder.DropTable(
                name: "capture_source_folders",
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
                name: "capture_sources",
                schema: "bill_payment");
        }
    }
}
