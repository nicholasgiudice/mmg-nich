namespace Game.Core.Simulation;

/// <summary>
/// Ordem explícita de execução (menor primeiro), com folga de 1000 entre blocos para inserir sistemas.
/// Duas registrações com a mesma ordem são erro (evita dependência de ordem de registro).
/// Ao criar um sistema: adicione uma constante aqui e uma linha em <see cref="SystemsRegistry"/>.
/// </summary>
public static class SystemOrder
{
    public const int Movement = 1000;   // T-007 substitui por movimento com colisão
    public const int Abilities = 2000;  // T-008
    // T-012: telegráfos, áreas e fases resolvem ANTES de Status (3000), Recursos (4000) e IA (5000). Esses sistemas só leem os
    // eventos do tick corrente; se o dano de telegráfo/área fosse resolvido depois (ex.: 6000), não geraria recurso nem ameaça.
    public const int Telegraphs = 2500; // T-012: resolve telegráfos (dano/efeitos a quem está na forma)
    public const int Areas = 2600;      // T-012: aplica/expira áreas persistentes (poça, teia, armadilha)
    public const int Phases = 2700;     // T-012: fases de boss (vida -> fase; efeitos de entrada)
    public const int Status = 3000;     // T-009
    public const int StatusResourceBridge = 3500; // integração T-009 -> T-010 (StatusRemoved -> status_removed)
    public const int Resources = 4000;  // T-010
    public const int Ai = 5000;         // T-011
    public const int Death = 9000;      // converte vida <= 0 em EntityDied; deve ser quase o último
    public const int CombatLog = 9900;  // T-013 (apenas lê eventos)
}
