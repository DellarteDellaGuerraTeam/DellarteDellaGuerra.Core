using DellarteDellaGuerra.Domain.Visor;

namespace DellarteDellaGuerra.Domain.Tests.Visor
{
    public class VisorToggleResultTests
    {
        private static readonly VisorVariantPair Pair = new("helm_closed", "helm_open");

        [Fact]
        public void From_ClosedHelmet_OpensIt()
        {
            Assert.Equal(new VisorToggleResult("helm_open", true), VisorToggleResult.From(Pair, "helm_closed"));
        }

        [Fact]
        public void From_OpenHelmet_ClosesIt()
        {
            Assert.Equal(new VisorToggleResult("helm_closed", false), VisorToggleResult.From(Pair, "helm_open"));
        }

        [Fact]
        public void ToState_OpenClosedHelmet_ReturnsOpenItem()
        {
            Assert.Equal(new VisorToggleResult("helm_open", true), VisorToggleResult.ToState(Pair, "helm_closed", true));
        }

        [Fact]
        public void ToState_CloseOpenHelmet_ReturnsClosedItem()
        {
            Assert.Equal(new VisorToggleResult("helm_closed", false),
                VisorToggleResult.ToState(Pair, "helm_open", false));
        }

        [Fact]
        public void ToState_OpenAlreadyOpenHelmet_Null()
        {
            Assert.Null(VisorToggleResult.ToState(Pair, "helm_open", true));
        }

        [Fact]
        public void ToState_CloseAlreadyClosedHelmet_Null()
        {
            Assert.Null(VisorToggleResult.ToState(Pair, "helm_closed", false));
        }
    }
}
