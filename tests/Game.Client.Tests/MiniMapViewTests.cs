using System.Numerics;
using MmoRpgClient.Hud;

namespace Game.Client.Tests;

/// <summary>T-392: recorte do mini mapa, escala do mapa grande e marca presa na borda.</summary>
public class MiniMapViewTests
{
    [Fact]
    public void Big_map_shows_64_tiles_around_the_player_at_3_pixels_per_tile()
    {
        // Floresta Baixa (200 × 160), jogador no meio.
        var frame = MiniMapView.Mini(200, 160, new Vector2(100, 80));

        Assert.Equal(3, frame.Scale);
        Assert.Equal(new Vector2(64, 64), frame.Size);
        Assert.Equal(new Vector2(68, 48), frame.Origin);
        Assert.Equal(new Vector2(96, 96), frame.ToPixels(new Vector2(100, 80))); // jogador no centro do painel
    }

    [Fact]
    public void Window_stops_at_the_map_edges()
    {
        var corner = MiniMapView.Mini(200, 160, new Vector2(3, 2));
        Assert.Equal(Vector2.Zero, corner.Origin);

        var farCorner = MiniMapView.Mini(200, 160, new Vector2(199, 159));
        Assert.Equal(new Vector2(136, 96), farCorner.Origin);
        Assert.Equal(new Vector2(189, 189), farCorner.ToPixels(new Vector2(199, 159))); // jogador fora do centro, perto da borda
    }

    [Fact]
    public void Small_map_appears_whole_and_enlarged()
    {
        // Campos (32 × 20): 192 / 32 = 6 pixels por tile, mapa inteiro.
        var frame = MiniMapView.Mini(32, 20, new Vector2(16, 10));

        Assert.Equal(6, frame.Scale);
        Assert.Equal(Vector2.Zero, frame.Origin);
        Assert.Equal(new Vector2(32, 20), frame.Size);
        Assert.Equal(new Vector2(0, 36), MiniMapView.CenterOffset(frame, new Vector2(192, 192)));
    }

    [Fact]
    public void Map_wider_than_the_window_but_short_keeps_its_full_height()
    {
        // Cidade (80 × 60): recorte de 64 tiles na largura, altura inteira.
        var frame = MiniMapView.Mini(80, 60, new Vector2(70, 30));

        Assert.Equal(3, frame.Scale);
        Assert.Equal(new Vector2(64, 60), frame.Size);
        Assert.Equal(new Vector2(16, 0), frame.Origin);
    }

    [Theory]
    [InlineData(200, 160, 3)] // Floresta: 1000 / 200 = 5 e 560 / 160 = 3,5 → 3
    [InlineData(140, 76, 7)] // Cripta: 1000 / 140 = 7,1 e 560 / 76 = 7,4 → 7
    [InlineData(32, 20, 10)] // Campos: caberia 28, mas o teto é 10
    public void Whole_map_uses_the_largest_integer_scale_that_fits(int width, int height, int expected)
    {
        var available = new Vector2(1000, 560);
        var frame = MiniMapView.Whole(width, height, available);

        Assert.Equal(expected, frame.Scale);
        Assert.Equal(Vector2.Zero, frame.Origin);
        Assert.True(frame.PixelSize.X <= available.X && frame.PixelSize.Y <= available.Y);
    }

    [Fact]
    public void Mark_inside_stays_where_it_is()
    {
        var (pixel, inside) = MiniMapView.PinToEdge(new Vector2(50, 120), new Vector2(192, 192), 6);

        Assert.True(inside);
        Assert.Equal(new Vector2(50, 120), pixel);
    }

    [Fact]
    public void Mark_outside_is_pinned_to_the_edge_pointing_to_the_member()
    {
        var area = new Vector2(192, 192);

        // Muito à direita, na altura do centro: borda direita, no meio.
        var (right, inside) = MiniMapView.PinToEdge(new Vector2(500, 96), area, 6);
        Assert.False(inside);
        Assert.Equal(new Vector2(186, 96), right);

        // Na diagonal, acima e à esquerda: fica no canto, na mesma direção.
        var (corner, _) = MiniMapView.PinToEdge(new Vector2(-300, -300), area, 6);
        Assert.Equal(6, corner.X, 3);
        Assert.Equal(6, corner.Y, 3);

        // Abaixo e um pouco à direita: borda de baixo, ainda à direita do centro.
        var (below, _) = MiniMapView.PinToEdge(new Vector2(150, 600), area, 6);
        Assert.Equal(186, below.Y, 3);
        Assert.InRange(below.X, 96.1f, 186f);
    }

    [Fact]
    public void Tiles_and_pixels_convert_back_and_forth()
    {
        var frame = new MapFrame(new Vector2(10, 20), new Vector2(64, 64), 3);
        var tile = new Vector2(30.5f, 41.25f);

        var back = frame.ToTiles(frame.ToPixels(tile));
        Assert.Equal(tile.X, back.X, 4);
        Assert.Equal(tile.Y, back.Y, 4);
    }
}
