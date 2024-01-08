using Ecommerce.Domain.Common.Models;

namespace Ecommerce.Domain.Product.ValueObjects
{
    public sealed class ProductCategoryId : ValueObject
    {
        public Guid Value { get; }

        private ProductCategoryId(Guid value) 
        {
            Value = value;
        }

        public static ProductCategoryId CreateUnique()
        {
            return new(Guid.NewGuid());
        }

        public override IEnumerable<object> GetEqualityComponents()
        {
            throw new NotImplementedException();
        }
    }
}
