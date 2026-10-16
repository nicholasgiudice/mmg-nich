using Game.Core;
using Godot;

namespace MmoRpgClient;

/// <summary>Cena principal do spike: mostra a versão do Core para provar a referência.</summary>
public partial class Main : Control
{
    public override void _Ready()
    {
        GetNode<Label>("CoreVersionLabel").Text = $"Core {CoreInfo.Version}";
    }
}
