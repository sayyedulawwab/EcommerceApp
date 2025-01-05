﻿// <auto-generated />
using System;
using Ecommerce.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Ecommerce.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Ecommerce.Domain.Categories.Category", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("code");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("name");

                    b.Property<DateTime?>("UpdatedOn")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_on");

                    b.HasKey("Id")
                        .HasName("pk_categories");

                    b.ToTable("categories", (string)null);
                });

            modelBuilder.Entity("Ecommerce.Domain.Orders.Order", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTime>("CreatedOnUtc")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on_utc");

                    b.Property<int>("Status")
                        .HasColumnType("integer")
                        .HasColumnName("status");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_orders");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_orders_user_id");

                    b.ToTable("orders", (string)null);
                });

            modelBuilder.Entity("Ecommerce.Domain.Orders.OrderItem", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on");

                    b.Property<Guid>("OrderId")
                        .HasColumnType("uuid")
                        .HasColumnName("order_id");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uuid")
                        .HasColumnName("product_id");

                    b.Property<int>("Quantity")
                        .HasColumnType("integer")
                        .HasColumnName("quantity");

                    b.HasKey("Id")
                        .HasName("pk_order_items");

                    b.HasIndex("OrderId")
                        .HasDatabaseName("ix_order_items_order_id");

                    b.HasIndex("ProductId")
                        .HasDatabaseName("ix_order_items_product_id");

                    b.ToTable("order_items", (string)null);
                });

            modelBuilder.Entity("Ecommerce.Domain.Products.Product", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)")
                        .HasColumnName("description");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("name");

                    b.Property<Guid>("ProductCategoryId")
                        .HasColumnType("uuid")
                        .HasColumnName("product_category_id");

                    b.Property<int>("Quantity")
                        .HasColumnType("integer")
                        .HasColumnName("quantity");

                    b.Property<DateTime?>("UpdatedOn")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_on");

                    b.HasKey("Id")
                        .HasName("pk_products");

                    b.HasIndex("ProductCategoryId")
                        .HasDatabaseName("ix_products_product_category_id");

                    b.ToTable("products", (string)null);
                });

            modelBuilder.Entity("Ecommerce.Domain.Reviews.Review", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<string>("Comment")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("comment");

                    b.Property<DateTime>("CreatedOnUtc")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on_utc");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uuid")
                        .HasColumnName("product_id");

                    b.Property<int>("Rating")
                        .HasColumnType("integer")
                        .HasColumnName("rating");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_reviews");

                    b.HasIndex("ProductId")
                        .HasDatabaseName("ix_reviews_product_id");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_reviews_user_id");

                    b.ToTable("reviews", (string)null);
                });

            modelBuilder.Entity("Ecommerce.Domain.Users.User", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("email");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("first_name");

                    b.Property<bool>("IsAdmin")
                        .HasColumnType("boolean")
                        .HasColumnName("is_admin");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("last_name");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("password_hash");

                    b.Property<string>("PasswordSalt")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("password_salt");

                    b.HasKey("Id")
                        .HasName("pk_users");

                    b.ToTable("users", (string)null);
                });

            modelBuilder.Entity("Ecommerce.Domain.Orders.Order", b =>
                {
                    b.HasOne("Ecommerce.Domain.Users.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_orders_users_user_id");

                    b.OwnsOne("Ecommerce.Domain.Shared.Money", "TotalPrice", b1 =>
                        {
                            b1.Property<Guid>("OrderId")
                                .HasColumnType("uuid")
                                .HasColumnName("id");

                            b1.Property<decimal>("Amount")
                                .HasColumnType("decimal(18, 2)")
                                .HasColumnName("total_price_amount");

                            b1.Property<string>("Currency")
                                .IsRequired()
                                .ValueGeneratedOnAdd()
                                .HasColumnType("text")
                                .HasDefaultValue("BDT")
                                .HasColumnName("total_price_currency");

                            b1.HasKey("OrderId");

                            b1.ToTable("orders");

                            b1.WithOwner()
                                .HasForeignKey("OrderId")
                                .HasConstraintName("fk_orders_orders_id");
                        });

                    b.Navigation("TotalPrice")
                        .IsRequired();
                });

            modelBuilder.Entity("Ecommerce.Domain.Orders.OrderItem", b =>
                {
                    b.HasOne("Ecommerce.Domain.Orders.Order", null)
                        .WithMany("OrderItems")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_order_items_orders_order_id");

                    b.HasOne("Ecommerce.Domain.Products.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_order_items_products_product_id");

                    b.Navigation("Product");
                });

            modelBuilder.Entity("Ecommerce.Domain.Products.Product", b =>
                {
                    b.HasOne("Ecommerce.Domain.Categories.Category", null)
                        .WithMany()
                        .HasForeignKey("ProductCategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_products_categories_product_category_id");

                    b.OwnsOne("Ecommerce.Domain.Shared.Money", "Price", b1 =>
                        {
                            b1.Property<Guid>("ProductId")
                                .HasColumnType("uuid")
                                .HasColumnName("id");

                            b1.Property<decimal>("Amount")
                                .HasColumnType("decimal(18, 2)")
                                .HasColumnName("price_amount");

                            b1.Property<string>("Currency")
                                .IsRequired()
                                .ValueGeneratedOnAdd()
                                .HasColumnType("text")
                                .HasDefaultValue("BDT")
                                .HasColumnName("price_currency");

                            b1.HasKey("ProductId");

                            b1.ToTable("products");

                            b1.WithOwner()
                                .HasForeignKey("ProductId")
                                .HasConstraintName("fk_products_products_id");
                        });

                    b.Navigation("Price")
                        .IsRequired();
                });

            modelBuilder.Entity("Ecommerce.Domain.Reviews.Review", b =>
                {
                    b.HasOne("Ecommerce.Domain.Products.Product", null)
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_reviews_products_product_id");

                    b.HasOne("Ecommerce.Domain.Users.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_reviews_users_user_id");
                });

            modelBuilder.Entity("Ecommerce.Domain.Orders.Order", b =>
                {
                    b.Navigation("OrderItems");
                });
#pragma warning restore 612, 618
        }
    }
}
