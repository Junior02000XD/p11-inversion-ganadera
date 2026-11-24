namespace P11.Api.Models;

/// <summary>Roles del sistema (RF-06). Solo Inversionista y Productor se registran solos.</summary>
public static class Roles
{
    public const string Inversionista = "Inversionista";
    public const string Productor = "Productor";
    public const string Analista = "Analista";
    public const string Administrador = "Administrador";

    public static readonly string[] Todos = [Inversionista, Productor, Analista, Administrador];
    public static readonly string[] Registrables = [Inversionista, Productor];
}
