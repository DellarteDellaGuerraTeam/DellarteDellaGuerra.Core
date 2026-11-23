using DellarteDellaGuerra.Domain.Visor;
using DellarteDellaGuerra.Domain.Visor.Port;

namespace DellarteDellaGuerra.Domain.Tests.Visor
{
    public class VisorVariantCatalogTests
    {
        private static readonly VisorVariantPair Pair = new("helm_closed", "helm_open");

        private readonly VisorVariantCatalog _catalog =
            new(new FakeVisorVariantRepository(new VisorVariantPair("other_closed", "other_open"), Pair));

        [Theory]
        [InlineData("helm_closed")]
        [InlineData("helm_open")]
        public void TryFindPair_EitherVariant_FindsThePair(string itemId)
        {
            Assert.True(_catalog.TryFindPair(itemId, out var pair));
            Assert.Equal(Pair, pair);
        }

        [Fact]
        public void TryFindPair_UnknownItem_False()
        {
            Assert.False(_catalog.TryFindPair("plain_helm", out _));
        }

        private class FakeVisorVariantRepository : IVisorVariantRepository
        {
            private readonly IReadOnlyList<VisorVariantPair> _pairs;

            public FakeVisorVariantRepository(params VisorVariantPair[] pairs)
            {
                _pairs = pairs;
            }

            public IReadOnlyList<VisorVariantPair> LoadPairs() => _pairs;
        }
    }
}
