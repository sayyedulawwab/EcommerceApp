using Ecommerce.IntegrationTests.Infrastructure;

// One PostgreSQL container, one Redis container and one API host for the whole run, instead of one set per
// test class. Every test resets the same database, so the tests run one at a time.
[assembly: AssemblyFixture(typeof(IntegrationTestFactory))]
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
