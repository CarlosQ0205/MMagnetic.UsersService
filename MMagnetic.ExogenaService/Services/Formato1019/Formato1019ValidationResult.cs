using System.Collections.Generic;

namespace MMagnetic.ExogenaService.Services.Formato1019;

public record Formato1019ValidationError(string Codigo, string Mensaje, string? Contexto = null);

public class Formato1019ValidationResult
{
    private readonly List<Formato1019ValidationError> _errores = new();

    public IReadOnlyList<Formato1019ValidationError> Errores => _errores;

    public bool EsValido => _errores.Count == 0;

    public void AgregarError(string codigo, string mensaje, string? contexto = null)
        => _errores.Add(new Formato1019ValidationError(codigo, mensaje, contexto));
}
