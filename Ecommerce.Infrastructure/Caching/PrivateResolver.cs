using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Ecommerce.Infrastructure.Caching;

public class PrivateResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty prop = base.CreateProperty(member, memberSerialization);

        if (!prop.Writable)
        {
            var property = member as PropertyInfo;

            bool hasPrivateSetter = property?.GetGetMethod(true) != null;

            prop.Writable = hasPrivateSetter;
        }

        return prop;
    }
}

