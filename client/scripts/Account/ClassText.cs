namespace MmoRpgClient.Account;

/// <summary>Descrição curta de cada classe na criação de personagem (resumo de <c>design/classes-v0.1.md</c>).</summary>
public static class ClassText
{
    public static string Describe(string classId) => classId switch
    {
        "class.warrior" => "Combate corpo a corpo, resistência e proteção do grupo. Segura os monstros com Provocação e "
            + "Golpe Circular e aguenta o dano com Guarda Firme. Atributos: Força e Vitalidade. "
            + "Fraqueza: pouco alcance e pouca mobilidade.",
        "class.archer" => "Dano à distância, posicionamento e eliminação de alvos prioritários. Marca o alvo, recua e "
            + "prende inimigos com armadilhas. Atributos: Destreza e Agilidade. "
            + "Fraqueza: pouca resistência; sofre quando cercado.",
        "class.mage" => "Dano mágico em área e controle: congela, explode e protege com a Barreira Arcana. "
            + "Atributos: Inteligência. Fraqueza: frágil e dependente de Mana; precisa de espaço para conjurar.",
        "class.cleric" => "Cura, proteção e remoção de efeitos: mantém o grupo vivo e também luta com a maça. "
            + "Atributos: Inteligência e Vitalidade. Fraqueza: dano baixo; o grupo depende do seu posicionamento.",
        _ => "",
    };
}
