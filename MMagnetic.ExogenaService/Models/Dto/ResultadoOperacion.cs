namespace MMagnetic.ExogenaService.Models.Dto;

public class ResultadoOperacion<T>
{
    public bool Exitoso { get; }
    public T? Dato { get; }
    public IReadOnlyList<string> Errores { get; }

    private ResultadoOperacion(bool exitoso, T? dato, IReadOnlyList<string> errores)
    {
        Exitoso = exitoso;
        Dato = dato;
        Errores = errores;
    }

    public static ResultadoOperacion<T> Ok(T dato) => new(true, dato, Array.Empty<string>());
    public static ResultadoOperacion<T> Fallo(params string[] errores) => new(false, default, errores);
    public static ResultadoOperacion<T> Fallo(IReadOnlyList<string> errores) => new(false, default, errores);
}
