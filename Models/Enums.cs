namespace ControlLavados.Models;

/// <summary>Tipo de proceso de lavado.</summary>
public enum TipoLavado
{
    Camion = 0,
    Hielo = 1
}

/// <summary>Origen del operario, usado para los contadores de la grilla.</summary>
public enum TipoOperario
{
    Offal = 0,
    Agencia = 1
}

/// <summary>
/// Estados del proceso.
/// Camión:  Pendiente -> Atracado -> Lavando -> LavadoTerminado -> Finalizado
/// Hielo:   Pendiente -> Lavando -> Finalizado
/// </summary>
public static class EstadoLavado
{
    public const string Pendiente = "Pendiente";
    public const string Atracado = "Atracado";
    public const string Lavando = "Lavando";
    public const string LavadoTerminado = "Lavado terminado";
    public const string Finalizado = "Finalizado";
}

/// <summary>Turno calculado automáticamente según la hora de inicio.</summary>
public static class Turnos
{
    public const string Mañana = "Mañana";
    public const string Tarde = "Tarde";
    public const string Noche = "Noche";

    /// <summary>Antes de 14:00 = Mañana, 14:00–20:59 = Tarde, resto = Noche.</summary>
    public static string DesdeHora(DateTime hora)
    {
        var h = hora.Hour;
        if (h < 14) return Mañana;
        if (h < 21) return Tarde;
        return Noche;
    }
}
