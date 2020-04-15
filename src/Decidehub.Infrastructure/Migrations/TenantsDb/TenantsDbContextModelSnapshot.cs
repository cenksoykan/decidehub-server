﻿// <auto-generated />

using Decidehub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Decidehub.Infrastructure.Migrations.TenantsDb
{
    [DbContext(typeof(TenantsDbContext))]
    partial class TenantsDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("Decidehub.Core.Entities.Tenant", b =>
                {
                    b.Property<string>("Id");

                    b.Property<string>("HostName");

                    b.Property<bool>("InActive");

                    b.Property<string>("Lang");

                    b.HasKey("Id", "HostName");

                    b.HasAlternateKey("Id");

                    b.ToTable("Tenants");
                });
#pragma warning restore 612, 618
        }
    }
}
