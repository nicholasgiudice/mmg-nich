namespace MmoRpgClient.Hud;

/// <summary>O que o mestre das classes faz ao ser clicado (T-541).</summary>
public enum MasterTalk
{
    /// <summary>Abaixo do nível da segunda classe: o mestre manda voltar mais tarde.</summary>
    TooLow,

    /// <summary>No nível, sem segunda classe: abre a tela de habilidades já na escolha.</summary>
    ChooseSecondClass,

    /// <summary>Já com segunda classe: abre a tela de habilidades.</summary>
    OpenSkills,
}

/// <summary>
/// Mestres das classes (T-541, <c>design/mapas-16-40-v0.1.md</c> §8 decisão 5): os NPCs que abrem a escolha da segunda
/// classe. A lista fica só aqui; o resto do cliente pergunta <see cref="IsMaster"/>. A escolha em si continua sendo a da
/// tela de habilidades (T-403/T-410), validada pelo servidor como sempre.
/// </summary>
public static class ClassMasters
{
    /// <summary>Ids de definição dos NPCs mestres: o Salão de Treinamento da Cidade e o Forte do Passo (T-531).</summary>
    private static readonly HashSet<string> Ids = ["npc.trainer", "npc.class_master"];

    public static bool IsMaster(string npcDefinitionId) => Ids.Contains(npcDefinitionId);

    public static MasterTalk Talk(int level, int secondClassLevel, bool hasSecondClass) =>
        hasSecondClass ? MasterTalk.OpenSkills
        : level >= secondClassLevel ? MasterTalk.ChooseSecondClass
        : MasterTalk.TooLow;

    /// <summary>A fala do mestre a quem ainda não chegou no nível (sai sobre o personagem).</summary>
    public static string TooLowText(string masterName, int secondClassLevel) =>
        $"{masterName}: volte no nível {secondClassLevel}, quando a segunda classe libera.";
}
