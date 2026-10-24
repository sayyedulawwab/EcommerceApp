using Bogus;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Coupons;
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
            FirstName firstName = FirstName.Create(faker.Person.FirstName).Value;
            LastName lastName = LastName.Create(faker.Person.LastName).Value;
            Domain.Users.Email email = Domain.Users.Email.Create(faker.Internet.Email()).Value;


            string passwordSalt = authService.GenerateSalt();
            string passwordHash = authService.HashPassword("test1234", passwordSalt);

            bool isAdmin = faker.Random.Bool();

            var newUser = User.Create(firstName, lastName, email, passwordHash, passwordSalt, isAdmin);

            context.Set<User>().Add(newUser);
        }

        // Seed product categories
        for (int i = 0; i < 10; i++)
        {
            CategoryName name = CategoryName.Create(faker.Commerce.Department()).Value;
            CategoryCode code = CategoryCode.Create(faker.Random.AlphaNumeric(5)).Value;

            var category = Category.Create(name, code, DateTime.UtcNow);

            context.Set<Category>().Add(category);
        }

        context.SaveChanges();

        var productCategoryGuids = context.Set<Category>().Select(pc => pc.Id).ToList();

        // Seed products
        for (int i = 0; i < 100; i++)
        {
            ProductName name = ProductName.Create(faker.Commerce.ProductName()).Value;
            ProductDescription description = ProductDescription.Create(faker.Commerce.ProductAdjective()).Value;
            int quantity = faker.Random.Number(1, 100);
            var price = new Money(faker.Finance.Amount(1, 1000), Currency.Bdt);
            CategoryId categoryId = productCategoryGuids[i % productCategoryGuids.Count];

            Product product = Product.Create(name, description, price, quantity, categoryId, DateTime.UtcNow).Value;

            context.Set<Product>().Add(product);
        }

        // Seed coupons. There's no endpoint to create them yet, because coupons need an admin role.
        DateTime now = DateTime.UtcNow;

        context.Set<Coupon>().AddRange(
            Coupon.Create(CouponCode.Create("WELCOME10").Value, 10, now, now.AddMonths(3), 1000, now).Value,
            Coupon.Create(CouponCode.Create("FLASH25").Value, 25, now, now.AddDays(2), 50, now).Value);

        context.SaveChanges();
    }
}
