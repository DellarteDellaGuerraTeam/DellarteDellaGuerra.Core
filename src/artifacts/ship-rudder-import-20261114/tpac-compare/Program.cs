using System.Collections;
using System.Reflection;
using System.Text.Json;
using TpacTool.Lib;

static object? SnapshotValue(object? value, int depth = 0)
{
    if (value is null) return null;
    var type = value.GetType();
    if (value is string or Guid || type.IsPrimitive || type.IsEnum || value is decimal)
        return value.ToString();
    if (value is byte[] bytes) return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
    if (depth >= 3) return type.FullName;
    if (value is IEnumerable sequence)
        return sequence.Cast<object>().Select(item => SnapshotValue(item, depth + 1)).ToArray();
    var members = new SortedDictionary<string, object?>();
    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        members[field.Name] = SnapshotValue(field.GetValue(value), depth + 1);
    foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        if (property.GetIndexParameters().Length == 0 && !members.ContainsKey(property.Name))
            try { members[property.Name] = SnapshotValue(property.GetValue(value), depth + 1); } catch { }
    return members;
}

static object Snapshot(string path)
{
    var package = new AssetPackage(path);
    package.Load();
    return package.Items.Select(asset => new {
        asset.Name, asset.Guid, Type = asset.GetType().FullName,
        Members = SnapshotValue(asset),
        TextureSlots = asset is Material material ? material.Textures.OrderBy(x => x.Key).Select(x => new { Slot = x.Key, Guid = x.Value.Guid }).ToArray() : null
    }).ToArray();
}

if (args.Length == 3 && args[0] == "--package-cache-index")
{
    var packageRows = new List<object>();
    foreach (var path in Directory.EnumerateFiles(args[1], "*.tpac", SearchOption.AllDirectories))
    {
        try
        {
            var package = new AssetPackage(path);
            package.Load();
            packageRows.Add(new { Package = path, PackageGuid = package.Guid,
                Assets = package.Items.Select(asset => new { asset.Name, asset.Guid, Type = asset.GetType().Name }).ToArray() });
        }
        catch (Exception error)
        {
            packageRows.Add(new { Package = path, Error = error.Message });
        }
    }
    File.WriteAllText(args[2], JsonSerializer.Serialize(packageRows, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Indexed {packageRows.Count} packages without changing compiled assets.");
    return;
}
if (args.Length == 3 && args[0] == "--mesh-cache-index")
{
    var cacheRows = new List<object>();
    foreach (var path in Directory.EnumerateFiles(args[1], "*.tpac", SearchOption.AllDirectories))
    {
        var package = new AssetPackage(path);
        package.Load();
        foreach (var asset in package.Items.OfType<Metamesh>())
            cacheRows.Add(new { Package = path, PackageGuid = package.Guid, asset.Name, asset.Guid,
                Meshes = asset.Meshes.Select(mesh => new { mesh.Name, mesh.Guid, mesh.VertexCount }).ToArray() });
    }
    File.WriteAllText(args[2], JsonSerializer.Serialize(cacheRows, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Indexed {cacheRows.Count} metameshes without changing compiled assets.");
    return;
}
if (args.Length == 2 && args[0] == "--inventory")
{
    var names = new HashSet<string> {
        "dadg_ws_english_boat_b_body", "dadg_ws_kogge_body_without_yards_v1", "dadg_ws_carrack_body_without_yards_v1",
        "dadg_ws_english_boat_b_body_no_rudder_v1", "dadg_ws_kogge_body_no_rudder_v1", "dadg_ws_carrack_body_no_rudder_v1",
        "dadg_ws_english_boat_b_rudder_v1", "dadg_ws_kogge_rudder_v1", "dadg_ws_carrack_rudder_v1"
    };
    var meshes = new List<object>();
    var rudderPackages = new List<object>();
    foreach (var path in Directory.EnumerateFiles(args[1], "*.tpac", SearchOption.AllDirectories))
    {
        var package = new AssetPackage(path);
        package.Load();
        foreach (var asset in package.Items.OfType<Metamesh>().Where(asset => names.Contains(asset.Name)))
            meshes.Add(new { asset.Name, asset.Guid, Package = path });
        if (Path.GetDirectoryName(path)!.EndsWith(Path.DirectorySeparatorChar + "rudder"))
            rudderPackages.Add(new { Path = path, Metameshes = package.Items.OfType<Metamesh>().Select(asset => asset.Name).ToArray() });
    }
    Console.WriteLine(JsonSerializer.Serialize(new { Meshes = meshes, RudderPackages = rudderPackages }, new JsonSerializerOptions { WriteIndented = true }));
    return;
}
if (args.Length != 3) throw new ArgumentException("source Assets, target Assets, output JSON");
var rows = new List<object>();
foreach (var source in Directory.EnumerateFiles(args[0], "*.tpac", SearchOption.AllDirectories))
{
    var rel = Path.GetRelativePath(args[0], source);
    var target = Path.Combine(args[1], rel);
    if (File.Exists(target) && !File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(target)))
        rows.Add(new { Path = rel, Source = Snapshot(source), Target = Snapshot(target) });
}
File.WriteAllText(args[2], JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Compared {rows.Count} differing ship TPAC packages without changing them.");
