using System.Globalization;
using System.Text.Json;
using Game.Core.Definitions;
using Game.Core.Spatial;

namespace Game.Data;

/// <summary>
/// Lê um mapa do Tiled exportado em JSON (<c>.tmj</c>, T-051) e o converte numa <see cref="ArenaDefinition"/>: grade de
/// colisão (camada <c>collision</c>), pontos de surgimento (<c>spawns</c>), áreas de evento (<c>areas</c>) e transições de
/// zona (<c>transitions</c>). Camadas de tiles visuais e tilesets são do cliente e ficam de fora. Convenções em
/// <c>design/map-conventions-v0.1.md</c>.
/// </summary>
public static class TiledMapReader
{
    public const string Extension = ".tmj";

    /// <summary>Tamanho do tile em pixels; 1 tile = 1 unidade de mundo.</summary>
    public const int TileSize = 32;

    public const string CollisionLayer = "collision";
    public const string SpawnsLayer = "spawns";
    public const string AreasLayer = "areas";
    public const string TransitionsLayer = "transitions";

    public const string PlayerSpawnClass = "player_spawn";
    public const string MonsterSpawnClass = "monster_spawn";
    public const string TransitionClass = "transition";

    /// <summary>Id da arena para um arquivo de mapa: <c>maps/fields.tmj</c> → <c>map.fields</c>.</summary>
    public static string MapId(string file) => "map." + Path.GetFileNameWithoutExtension(file);

    /// <summary>
    /// Converte o JSON do mapa. Erros vão para <paramref name="errors"/> com <paramref name="file"/> e o caminho da camada ou
    /// do objeto; retorna null se o mapa não pode ser usado.
    /// </summary>
    public static ArenaDefinition? Read(string file, string json, List<LoadError> errors)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            errors.Add(new LoadError(file, "$", "JSON inválido: " + ex.Message));
            return null;
        }

        using (document)
        {
            var before = errors.Count;
            var reader = new MapReader(file, errors);
            var arena = reader.Read(document.RootElement);
            return errors.Count == before ? arena : null;
        }
    }

    private sealed class MapReader(string file, List<LoadError> errors)
    {
        private int _width;
        private int _height;
        private bool[] _blocked = [];

        private void Error(string path, string message) => errors.Add(new LoadError(file, path, message));

        public ArenaDefinition? Read(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object)
            {
                Error("$", "A raiz do mapa deve ser um objeto.");
                return null;
            }

            var id = MapId(file);
            if (!System.Text.RegularExpressions.Regex.IsMatch(id, "^map\\.[a-z][a-z0-9_]*$"))
            {
                Error("$", $"Nome de arquivo inválido para id '{id}': use snake_case (ex.: maps/vigil_fields.tmj).");
            }

            if (Str(root, "orientation") != "orthogonal")
            {
                Error("orientation", "O mapa deve ser ortogonal (Mapa > Propriedades do mapa > Orientação).");
            }

            if (root.TryGetProperty("infinite", out var inf) && inf.ValueKind == JsonValueKind.True)
            {
                Error("infinite", "Mapa infinito não é suportado: desmarque 'Infinito' nas propriedades do mapa.");
            }

            var tw = Int(root, "tilewidth");
            var th = Int(root, "tileheight");
            if (tw != TileSize || th != TileSize)
            {
                Error("tilewidth", $"Os tiles devem ter {TileSize}×{TileSize} px (o mapa usa {tw}×{th}).");
            }

            _width = Int(root, "width");
            _height = Int(root, "height");
            if (_width <= 0 || _height <= 0)
            {
                Error("width", $"Largura e altura do mapa devem ser maiores que 0 ({_width}×{_height}).");
                return null;
            }

            if (!root.TryGetProperty("layers", out var layersElement) || layersElement.ValueKind != JsonValueKind.Array)
            {
                Error("layers", "O mapa não tem camadas.");
                return null;
            }

            var layers = new List<(string Path, JsonElement Layer)>();
            Flatten(layersElement, "layers", layers);

            var byName = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (path, layer) in layers)
            {
                var name = Str(layer, "name") ?? "";
                if (!byName.TryAdd(name, path))
                {
                    Error(path, $"Camada '{name}' repetida (já existe em {byName[name]}).");
                }
            }

            var collision = layers.Where(l => Str(l.Layer, "name") == CollisionLayer).Select(l => ((string, JsonElement)?)l).FirstOrDefault();
            if (collision is not { } col)
            {
                Error("layers", $"Camada '{CollisionLayer}' faltando: crie uma camada de tiles com esse nome e pinte as células bloqueadas.");
                return null;
            }

            if (!ReadCollision(col.Item1, col.Item2))
            {
                return null;
            }

            var spawns = new List<MapSpawn>();
            var areas = new List<MapArea>();
            var transitions = new List<MapTransition>();
            foreach (var (path, layer) in layers)
            {
                var name = Str(layer, "name");
                var type = Str(layer, "type");
                if (type == "tilelayer")
                {
                    if (name is SpawnsLayer or AreasLayer or TransitionsLayer)
                    {
                        Error(path, $"'{name}' deve ser uma camada de objetos, não de tiles.");
                    }

                    continue;
                }

                if (type != "objectgroup")
                {
                    continue; // camada de imagem: só visual
                }

                switch (name)
                {
                    case SpawnsLayer: ReadObjects(path, layer, (p, o) => ReadSpawn(p, o, spawns)); break;
                    case AreasLayer: ReadObjects(path, layer, (p, o) => ReadArea(p, o, areas)); break;
                    case TransitionsLayer: ReadObjects(path, layer, (p, o) => ReadTransition(p, o, transitions)); break;
                    case CollisionLayer: Error(path, $"'{CollisionLayer}' deve ser uma camada de tiles, não de objetos."); break;
                    default:
                        Error(path, $"Camada de objetos desconhecida '{name}': use '{SpawnsLayer}', '{AreasLayer}' ou '{TransitionsLayer}'.");
                        break;
                }
            }

            Unique(spawns.Select(s => s.Name), SpawnsLayer);
            Unique(areas.Select(a => a.Name), AreasLayer);
            Unique(transitions.Select(t => t.Name), TransitionsLayer);

            var lines = new string[_height];
            for (var y = 0; y < _height; y++)
            {
                var row = new char[_width];
                for (var x = 0; x < _width; x++)
                {
                    row[x] = _blocked[(y * _width) + x] ? '#' : '.';
                }

                lines[y] = new string(row);
            }

            if (!_blocked.Contains(false))
            {
                Error($"layers[{CollisionLayer}]", "O mapa precisa de pelo menos uma célula livre.");
            }

            return new ArenaDefinition
            {
                Id = id,
                Lines = lines,
                Source = file.Replace('\\', '/'),
                Spawns = spawns,
                Areas = areas,
                Transitions = transitions,
            };
        }

        /// <summary>Camadas de grupo do Tiled viram uma lista plana (o caminho mostra onde cada uma está).</summary>
        private static void Flatten(JsonElement layers, string path, List<(string, JsonElement)> output)
        {
            foreach (var layer in layers.EnumerateArray())
            {
                var here = $"{path}[{Str(layer, "name")}]";
                if (Str(layer, "type") == "group" && layer.TryGetProperty("layers", out var inner) && inner.ValueKind == JsonValueKind.Array)
                {
                    Flatten(inner, here + ".layers", output);
                }
                else
                {
                    output.Add((here, layer));
                }
            }
        }

        private bool ReadCollision(string path, JsonElement layer)
        {
            if (Str(layer, "type") != "tilelayer")
            {
                Error(path, $"'{CollisionLayer}' deve ser uma camada de tiles.");
                return false;
            }

            if (Int(layer, "width") != _width || Int(layer, "height") != _height || Int(layer, "x") != 0 || Int(layer, "y") != 0)
            {
                Error(path, "A camada de colisão deve cobrir o mapa inteiro, sem deslocamento.");
                return false;
            }

            if (!layer.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                Error(path + ".data", "Formato de camada não suportado: use 'CSV' em Mapa > Propriedades do mapa > Formato da camada de tiles.");
                return false;
            }

            if (data.GetArrayLength() != _width * _height)
            {
                Error(path + ".data", $"A camada tem {data.GetArrayLength()} células; esperado {_width * _height} ({_width}×{_height}).");
                return false;
            }

            _blocked = new bool[_width * _height];
            var i = 0;
            foreach (var gid in data.EnumerateArray())
            {
                // Qualquer tile pintado (gid ≠ 0, com ou sem bits de espelhamento) bloqueia a célula.
                _blocked[i++] = gid.ValueKind == JsonValueKind.Number && gid.GetUInt64() != 0;
            }

            return true;
        }

        private void ReadObjects(string path, JsonElement layer, Action<string, JsonElement> read)
        {
            if (!layer.TryGetProperty("objects", out var objects) || objects.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var o in objects.EnumerateArray())
            {
                var name = Str(o, "name");
                var label = string.IsNullOrEmpty(name) ? $"id {Int(o, "id")}" : name;
                read($"{path}.objects[{label}]", o);
            }
        }

        private void ReadSpawn(string path, JsonElement o, List<MapSpawn> output)
        {
            var name = RequireName(path, o);
            var cls = ClassOf(o);
            var props = Properties(o);
            MapSpawnKind kind;
            switch (cls)
            {
                case PlayerSpawnClass: kind = MapSpawnKind.Player; break;
                case MonsterSpawnClass: kind = MapSpawnKind.Monster; break;
                default:
                    Error(path, $"Classe '{cls}' inválida na camada '{SpawnsLayer}': use '{PlayerSpawnClass}' ou '{MonsterSpawnClass}'.");
                    return;
            }

            // Ponto: (x, y). Retângulo: o centro.
            var x = (Num(o, "x") + (Num(o, "width") / 2)) / TileSize;
            var y = (Num(o, "y") + (Num(o, "height") / 2)) / TileSize;
            if (x < 0 || y < 0 || x >= _width || y >= _height)
            {
                Error(path, $"Posição ({Fmt(x)}, {Fmt(y)}) fora do mapa.");
            }
            else if (_blocked[((int)Math.Floor(y) * _width) + (int)Math.Floor(x)])
            {
                Error(path, $"Posição ({Fmt(x)}, {Fmt(y)}) está numa célula bloqueada.");
            }

            string? monster = null;
            int? level = null;
            if (kind == MapSpawnKind.Monster)
            {
                monster = props.GetValueOrDefault("monster");
                if (string.IsNullOrEmpty(monster))
                {
                    Error(path, "Propriedade 'monster' (id do monstro, ex.: monster.grey_wolf) faltando.");
                }

                if (props.TryGetValue("level", out var lv))
                {
                    if (int.TryParse(lv, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n >= 1)
                    {
                        level = n;
                    }
                    else
                    {
                        Error(path, $"Propriedade 'level' deve ser um inteiro ≥ 1 (valor: '{lv}').");
                    }
                }
            }

            output.Add(new MapSpawn { Kind = kind, Name = name, Position = new PositionDefinition(x, y), Monster = monster, Level = level });
        }

        private void ReadArea(string path, JsonElement o, List<MapArea> output)
        {
            var name = RequireName(path, o);
            var cls = ClassOf(o);
            if (string.IsNullOrEmpty(cls))
            {
                Error(path, "Área sem classe: diga o que ela é (ex.: altar, seal, web).");
            }

            if (Rect(path, o) is { } rect)
            {
                output.Add(new MapArea { Name = name, Kind = cls ?? "", Area = rect, Properties = Properties(o) });
            }
        }

        private void ReadTransition(string path, JsonElement o, List<MapTransition> output)
        {
            var name = RequireName(path, o);
            var cls = ClassOf(o);
            if (!string.IsNullOrEmpty(cls) && cls != TransitionClass)
            {
                Error(path, $"Classe '{cls}' inválida na camada '{TransitionsLayer}': use '{TransitionClass}'.");
            }

            var props = Properties(o);
            var to = props.GetValueOrDefault("to");
            if (string.IsNullOrEmpty(to))
            {
                Error(path, "Propriedade 'to' (id da zona de destino, ex.: zone.city) faltando.");
            }

            var arrival = props.GetValueOrDefault("arrival");
            if (Rect(path, o) is { } rect)
            {
                output.Add(new MapTransition { Name = name, Area = rect, To = to ?? "", Arrival = string.IsNullOrEmpty(arrival) ? null : arrival });
            }
        }

        private RectDefinition? Rect(string path, JsonElement o)
        {
            if (o.TryGetProperty("polygon", out _) || o.TryGetProperty("polyline", out _) || Bool(o, "ellipse") || Bool(o, "point"))
            {
                Error(path, "Use um retângulo.");
                return null;
            }

            var w = Num(o, "width") / TileSize;
            var h = Num(o, "height") / TileSize;
            var x = Num(o, "x") / TileSize;
            var y = Num(o, "y") / TileSize;
            if (Num(o, "rotation") != 0)
            {
                Error(path, "Retângulo girado não é suportado.");
                return null;
            }

            if (!(w > 0 && h > 0))
            {
                Error(path, "Largura e altura devem ser maiores que 0.");
                return null;
            }

            if (x < 0 || y < 0 || x + w > _width || y + h > _height)
            {
                Error(path, "Retângulo fora do mapa.");
                return null;
            }

            return new RectDefinition(x, y, w, h);
        }

        private string RequireName(string path, JsonElement o)
        {
            var name = Str(o, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                Error(path, "Objeto sem nome: dê um nome único na camada.");
                return "";
            }

            return name;
        }

        private void Unique(IEnumerable<string> names, string layer)
        {
            foreach (var g in names.Where(n => n.Length > 0).GroupBy(n => n, StringComparer.Ordinal).Where(g => g.Count() > 1))
            {
                Error($"layers[{layer}]", $"Nome '{g.Key}' repetido na camada.");
            }
        }
    }

    /// <summary>Classe do objeto: "type" no JSON do Tiled (1.8 e 1.10+) ou "class" (1.9).</summary>
    private static string? ClassOf(JsonElement o) =>
        Str(o, "type") is { Length: > 0 } t ? t : Str(o, "class");

    /// <summary>Propriedades personalizadas do Tiled como texto (bool/número no formato invariante).</summary>
    private static Dictionary<string, string> Properties(JsonElement o)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!o.TryGetProperty("properties", out var props) || props.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var p in props.EnumerateArray())
        {
            if (Str(p, "name") is not { } name || !p.TryGetProperty("value", out var v))
            {
                continue;
            }

            result[name] = v.ValueKind switch
            {
                JsonValueKind.String => v.GetString() ?? "",
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => v.GetRawText(),
            };
        }

        return result;
    }

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int Int(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n) ? n : 0;

    private static double Num(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;

    private static bool Bool(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;

    private static string Fmt(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}
