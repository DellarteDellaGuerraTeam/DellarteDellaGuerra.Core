using System.Text;
using System.Text.RegularExpressions;
using DellarteDellaGuerra.Domain.Titles.Model;

namespace DellarteDellaGuerra.Domain.Titles
{
    /// <summary>
    /// Renders a feudal map as human-readable text: a markdown document or a mermaid
    /// "flowchart TD" graph (without code fences, so callers decide how to embed it).
    /// Output is deterministic and follows the order of the map entries.
    /// </summary>
    public class RenderFeudalMapUseCase : IRenderFeudalMapUseCase
    {
        private const string VacantHolder = "vacant";

        public string RenderMarkdown(FeudalMap map)
        {
            var builder = new StringBuilder();
            builder.Append("# Feudal map\n");

            foreach (FeudalMapEntry realm in map.Realms)
            {
                builder.Append('\n');
                builder.Append($"## {realm.Name}\n");
                builder.Append('\n');
                AppendMarkdownEntry(builder, realm, 0);
            }

            return builder.ToString();
        }

        public string RenderMermaid(FeudalMap map)
        {
            var nodes = new StringBuilder();
            var edges = new StringBuilder();
            foreach (FeudalMapEntry realm in map.Realms)
            {
                AppendMermaidEntry(nodes, edges, realm);
            }

            var builder = new StringBuilder();
            builder.Append("flowchart TD\n");
            builder.Append(nodes);
            builder.Append(edges);
            return builder.ToString();
        }

        private static void AppendMarkdownEntry(StringBuilder builder, FeudalMapEntry entry, int depth)
        {
            string indent = new string(' ', depth * 2);
            string holder = entry.HolderClanId is null ? $"*{VacantHolder}*" : $"`{entry.HolderClanId}`";
            builder.Append(
                $"{indent}- **{entry.Name}** (`{entry.TitleId}`) — seat: `{entry.SeatSettlementId}` — held by: {holder}\n");

            foreach (FeudalMapEntry vassal in entry.Vassals)
            {
                AppendMarkdownEntry(builder, vassal, depth + 1);
            }
        }

        private static void AppendMermaidEntry(StringBuilder nodes, StringBuilder edges, FeudalMapEntry entry)
        {
            string nodeId = SanitiseNodeId(entry.TitleId);
            string holder = entry.HolderClanId ?? VacantHolder;
            nodes.Append($"    {nodeId}[\"{entry.Name}\\n({holder})\"]\n");

            foreach (FeudalMapEntry vassal in entry.Vassals)
            {
                edges.Append($"    {nodeId} --> {SanitiseNodeId(vassal.TitleId)}\n");
                AppendMermaidEntry(nodes, edges, vassal);
            }
        }

        private static string SanitiseNodeId(string titleId) => Regex.Replace(titleId, "[^A-Za-z0-9]", "_");
    }
}
