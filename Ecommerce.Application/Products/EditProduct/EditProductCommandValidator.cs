using Ecommerce.Application.ProductCategories.EditProductCategory;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.Products.EditProduct;
public class EditProductCommandValidator : AbstractValidator<EditProductCommand>
{
    public EditProductCategoryValidator()
    {
        RuleFor(c => c.id).NotEmpty();
        RuleFor(c => c.name).NotEmpty();
        RuleFor(c => c.description).NotEmpty();
        RuleFor(c => c.priceCurrency).NotEmpty();
        RuleFor(c => c.priceAmount).NotEmpty();
        RuleFor(c => c.quantity).NotEmpty();
        RuleFor(c => c.productCategoryId).NotEmpty();

    }
}
