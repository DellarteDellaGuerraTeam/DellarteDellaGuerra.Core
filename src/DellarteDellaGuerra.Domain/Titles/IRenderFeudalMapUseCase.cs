using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles
{
    public interface IRenderFeudalMapUseCase
    {
        string RenderMarkdown(FeudalMap map);
        string RenderMermaid(FeudalMap map);
    }
}
