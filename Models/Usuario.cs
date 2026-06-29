using System.ComponentModel.DataAnnotations;

namespace ControlLavados.Models;

/// <summary>Usuario de la aplicación. El acceso es por Microsoft 365 (Entra ID);
/// esta tabla solo define quién es administrador.</summary>
public class Usuario
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Email { get; set; } = "";

    [MaxLength(120)]
    public string? Nombre { get; set; }

    /// <summary>Admin = accede a Reportes, Configuración y gestión de usuarios.
    /// Si es false, es operario (solo pantallas de carga).</summary>
    public bool EsAdmin { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime? UltimoAcceso { get; set; }
}
