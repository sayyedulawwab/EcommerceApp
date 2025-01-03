﻿using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Abstractions.Messaging;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.ProductCategories;

namespace Ecommerce.Application.ProductCategories.AddProductCategory;
internal sealed class AddProductCommandHandler : ICommandHandler<AddProductCategoryCommand, Guid>
{
    private readonly IProductCategoryRepository _productCategoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddProductCommandHandler(IProductCategoryRepository productCategoryRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _productCategoryRepository = productCategoryRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(AddProductCategoryCommand request, CancellationToken cancellationToken)
    {
        var productCategory = ProductCategory.Create(new CategoryName(request.Name), new CategoryCode(request.Code), _dateTimeProvider.UtcNow);

        _productCategoryRepository.Add(productCategory);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return productCategory.Id.Value;

    }
}
