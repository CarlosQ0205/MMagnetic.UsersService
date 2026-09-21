namespace MMagnetic.ExogenaService.Models.Dto;

public record FilaErrorCarga(int Fila, string Mensaje);

public class ResultadoCargaMasiva
{
    public int TotalFilas { get; init; }
    public int Exitosas { get; init; }
    public int ConErrores => TotalFilas - Exitosas;
    public List<FilaErrorCarga> Errores { get; init; } = new();
}
