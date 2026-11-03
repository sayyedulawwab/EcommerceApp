using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Categories.Events;

namespace Ecommerce.Domain.UnitTests.Categories;

public class CategoryTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly CategoryName Name = CategoryName.Create("Electronics").Value;
    private static readonly CategoryCode Code = CategoryCode.Create("ELEC").Value;

    [Fact]
    public void Create_Should_SetTheValuesAndRaiseCategoryCreatedDomainEvent()
    {
        var category = Category.Create(Name, Code, UtcNow);

        Assert.Equal(Name, category.Name);
        Assert.Equal(Code, category.Code);
        Assert.Equal(UtcNow, category.CreatedOnUtc);
        Assert.Null(category.UpdatedOnUtc);

        IDomainEvent domainEvent = Assert.Single(category.GetDomainEvents());
        Assert.Equal(new CategoryCreatedDomainEvent(category.Id), domainEvent);
    }

    [Fact]
    public void Update_Should_ChangeTheNameAndCodeAndSetUpdatedOn()
    {
        var category = Category.Create(Name, Code, UtcNow);
        CategoryName newName = CategoryName.Create("Computers").Value;
        CategoryCode newCode = CategoryCode.Create("COMP").Value;

        Category.Update(category, newName, newCode, UtcNow.AddDays(1));

        Assert.Equal(newName, category.Name);
        Assert.Equal(newCode, category.Code);
        Assert.Equal(UtcNow.AddDays(1), category.UpdatedOnUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CategoryName_Should_ReturnInvalid_WhenItIsEmpty(string? value)
    {
        Assert.Equal(CategoryName.Invalid, CategoryName.Create(value).Error);
    }

    [Fact]
    public void CategoryName_Should_ReturnInvalid_WhenItIsLongerThanTheMaximum()
    {
        Assert.Equal(CategoryName.Invalid, CategoryName.Create(new string('a', CategoryName.MaxLength + 1)).Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CategoryCode_Should_ReturnInvalid_WhenItIsEmpty(string? value)
    {
        Assert.Equal(CategoryCode.Invalid, CategoryCode.Create(value).Error);
    }

    [Fact]
    public void CategoryCode_Should_ReturnInvalid_WhenItIsLongerThanTheMaximum()
    {
        Assert.Equal(CategoryCode.Invalid, CategoryCode.Create(new string('a', CategoryCode.MaxLength + 1)).Error);
    }
}
