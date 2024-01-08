using Ecommerce.Domain.Common.Models;

namespace Ecommerce.Domain.Product.ValueObjects
{
    public sealed class ProductId : ValueObject
    {
        public Guid Value { get; }

        private ProductId(Guid value) 
        {
            Value = value;
        }

        public static ProductId CreateUnique()
        {
            return new(Guid.NewGuid());
        }

        public override IEnumerable<object> GetEqualityComponents()
        {
            throw new NotImplementedException();
        }
    }
}
