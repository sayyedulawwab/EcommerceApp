﻿using Bogus;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;
using Ecommerce.Domain.Users;
using Ecommerce.Infrastructure.Auth;
using Currency = Ecommerce.Domain.Shared.Currency;

namespace Ecommerce.Infrastructure.Data;

public static class DataSeeder
{
    public static void SeedData(ApplicationDbContext context)
    {
        var faker = new Faker();
        var authService = new AuthService();
        // Seed users
        for (int i = 0; i < 10; i++)
        {
            var firstName = new FirstName(faker.Person.FirstName);
            var lastName = new LastName(faker.Person.LastName);
            var email = new Domain.Users.Email(faker.Internet.Email());

            string password = "test1234";

            string passwordSalt = authService.GenerateSalt();
            string passwordHash = authService.HashPassword(password, passwordSalt);

            bool isAdmin = faker.Random.Bool();

            var newUser = User.Create(firstName, lastName, email, passwordHash, passwordSalt, isAdmin);

            context.Set<User>().Add(newUser);
        }

        // Seed product categories
        for (int i = 0; i < 10; i++)
        {
            var name = new CategoryName(faker.Commerce.Department());
            var code = new CategoryCode(faker.Random.AlphaNumeric(5));

            var category = Category.Create(name, code, DateTime.UtcNow);

            context.Set<Category>().Add(category);
        }

        context.SaveChanges();

        var productCategoryGuids = context.Set<Category>().Select(pc => pc.Id).ToList();

        // Seed products
        for (int i = 0; i < 100; i++)
        {
            var name = new ProductName(faker.Commerce.ProductName());
            var description = new ProductDescription(faker.Commerce.ProductAdjective());
            int quantity = faker.Random.Number(1, 100);
            var price = new Money(faker.Finance.Amount(), Currency.Create("BDT"));
            CategoryId categoryId = productCategoryGuids[i % productCategoryGuids.Count];

            var product = Product.Create(name, description, price, quantity, categoryId, DateTime.UtcNow);

            context.Set<Product>().Add(product);
        }

        context.SaveChanges();
    }
}
