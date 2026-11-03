using System.Buffers.Binary;
using MmoRpgClient.Presentation;

namespace Game.Client.Tests;

/// <summary>
/// T-080: <c>client/assets/art/icons/icons.json</c> tem ícone para tudo que as telas mostram (habilidades e consumíveis
/// das classes, itens, atributos, item no chão) e as regiões cabem na folha, sem sobreposição.
/// </summary>
public class IconIndexTests
{
    private static readonly string IconDir = Path.Combine(RepoRoot(), "client", "assets", "art", "icons");

    private static IconIndex LoadIndex() => IconIndex.Parse(File.ReadAllText(Path.Combine(IconDir, "icons.json")));

    [Fact]
    public void EveryShownIdHasItsOwnIcon()
    {
        var index = LoadIndex();
        Assert.Empty(index.Missing(SkillPlanTests.LoadContent()));
        Assert.True(index.Icons.ContainsKey(index.Fallback));
    }

    [Fact]
    public void RegionsFitTheSheetOnTheGridWithoutOverlap()
    {
        var index = LoadIndex();
        var (width, height) = PngSize(Path.Combine(IconDir, index.Sheet));
        foreach (var (id, r) in index.Icons)
        {
            Assert.True(r.X >= 0 && r.Y >= 0 && r.X + index.Size <= width && r.Y + index.Size <= height, $"{id} fora da folha");
            Assert.True(r.X % index.Size == 0 && r.Y % index.Size == 0, $"{id} fora da grade de {index.Size} px");
        }

        Assert.Equal(index.Icons.Count, index.Icons.Values.Distinct().Count());
    }

    /// <summary>Largura e altura do cabeçalho IHDR do PNG (bytes 16–23, big-endian).</summary>
    private static (int Width, int Height) PngSize(string path)
    {
        var header = new byte[24];
        using (var file = File.OpenRead(path))
        {
            file.ReadExactly(header);
        }

        return (BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(16)), BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(20)));
    }

    private static string RepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "data", "classes.json")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        return dir ?? throw new DirectoryNotFoundException("raiz do repositório não encontrada");
    }
}
