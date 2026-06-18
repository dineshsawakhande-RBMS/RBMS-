using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_on_hand = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    avg_cost = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory", x => x.id);
                    table.CheckConstraint("ck_inventory_nonneg", "quantity_on_hand >= 0");
                    table.ForeignKey(
                        name: "fk_inventory_product_variants_variant_id",
                        column: x => x.variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_adjustments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    adjustment_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    adjustment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_adjustments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    balance_after = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    reference_type = table.Column<string>(type: "text", nullable: true),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_movements_product_variants_variant_id",
                        column: x => x.variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_adjustment_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    adjustment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_delta = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_adjustment_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_adjustment_lines_stock_adjustments_adjustment_id",
                        column: x => x.adjustment_id,
                        principalTable: "stock_adjustments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_store_id_variant_id",
                table: "inventory",
                columns: new[] { "store_id", "variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inventory_variant_id",
                table: "inventory",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adjustment_lines_adjustment_id",
                table: "stock_adjustment_lines",
                column: "adjustment_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adjustments_tenant_id_adjustment_no",
                table: "stock_adjustments",
                columns: new[] { "tenant_id", "adjustment_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_reference_type_reference_id",
                table: "stock_movements",
                columns: new[] { "reference_type", "reference_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_variant_id_created_at",
                table: "stock_movements",
                columns: new[] { "variant_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory");

            migrationBuilder.DropTable(
                name: "stock_adjustment_lines");

            migrationBuilder.DropTable(
                name: "stock_movements");

            migrationBuilder.DropTable(
                name: "stock_adjustments");
        }
    }
}
