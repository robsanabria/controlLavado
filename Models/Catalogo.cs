namespace ControlLavados.Models;

public record OperarioCatalogo(string Nombre, TipoOperario Tipo);

/// <summary>
/// Listas fijas de la aplicación. Editá estos arrays para dar de alta/baja
/// patentes, operarios, frigoríficos, etc. (no requiere base de datos).
/// </summary>
public static class Catalogo
{
    // -- Operarios. Marcá cada uno como Offal o Agencia --
    public static readonly OperarioCatalogo[] Operarios =
    {
        new("TENAGLIA RODRIGO", TipoOperario.Agencia),
        new("FERNANDEZ NICOLAS", TipoOperario.Agencia),
        new("BARRIENTOS AGUSTIN", TipoOperario.Agencia),
        new("GOMEZ MARTIN", TipoOperario.Offal),
        new("PEREZ JUAN", TipoOperario.Offal),
        new("LOPEZ DIEGO", TipoOperario.Offal),
    };

    // -- Patentes de camiones --
    public static readonly string[] PatentesCamion =
    {
        "AC356EI", "AB186BD", "AD742JK", "AE091LM", "AF553NP",
    };

    // -- Identificadores para fábrica de hielo (equipos/sectores) --
    public static readonly string[] EquiposHielo =
    {
        "Cámara 1", "Cámara 2", "Planta Hielo",
    };

    // -- Dársenas (solo camiones) --
    public static readonly string[] Darsenas =
    {
        "Dársena 1", "Dársena 2",
    };

    // -- Frigoríficos --
    public static readonly string[] Frigorificos =
    {
        "Offal", "Frigorífico Norte", "Frigorífico Sur", "Externo",
    };

    public static TipoOperario TipoDe(string nombre)
        => Operarios.FirstOrDefault(o => o.Nombre == nombre)?.Tipo ?? TipoOperario.Agencia;
}
