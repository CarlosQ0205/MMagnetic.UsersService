using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;

namespace MMagnetic.ExogenaService.Services.Formato1019;

/// <summary>
/// Traduce los Id internos de Cliente (PaisId/DepartamentoId/MunicipioId) a los
/// códigos numéricos DIAN reales, contra el catálogo de MM_DIAN.
/// </summary>
public class HomologadorDianService : IHomologadorDianService
{
    private const string NombrePaisColombia = "colombia";

    private readonly DianDbContext _dian;

    public HomologadorDianService(DianDbContext dian)
    {
        _dian = dian;
    }

    public async Task<ResultadoHomologacion> HomologarAsync(
        int? paisId,
        int? departamentoId,
        int? municipioId,
        CancellationToken cancellationToken = default)
    {
        var resultado = new ResultadoHomologacion();

        var esColombia = false;

        if (paisId.HasValue)
        {
            var pais = await _dian.Paises.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PaisId == paisId.Value, cancellationToken);

            if (pais is null)
            {
                resultado.AgregarError($"El PaisId {paisId} no existe en el catálogo DIAN_Paises.");
            }
            else if (!int.TryParse(pais.CodigoPais, out var codigoPais))
            {
                resultado.AgregarError($"El país '{pais.NombrePais}' no tiene un CodigoPais numérico válido en el catálogo.");
            }
            else
            {
                resultado.CodigoPais = codigoPais;
                esColombia = string.Equals(pais.NombrePais?.Trim(), NombrePaisColombia, StringComparison.OrdinalIgnoreCase);
            }
        }

        if (departamentoId.HasValue)
        {
            var departamento = await _dian.Departamentos.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DepartamentoId == departamentoId.Value, cancellationToken);

            if (departamento is null)
                resultado.AgregarError($"El DepartamentoId {departamentoId} no existe en el catálogo DIAN_Departamentos.");
            else if (paisId.HasValue && departamento.PaisId != paisId.Value)
                resultado.AgregarError($"El DepartamentoId {departamentoId} no pertenece al PaisId {paisId} informado en el cliente.");
            else if (!int.TryParse(departamento.CodigoDepartamento, out var codigoDepartamento))
                resultado.AgregarError($"El departamento '{departamento.NombreDepartamento}' no tiene un CodigoDepartamento numérico válido en el catálogo.");
            else
                resultado.CodigoDepartamento = codigoDepartamento;
        }

        if (municipioId.HasValue)
        {
            var municipio = await _dian.Municipios.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MunicipioId == municipioId.Value, cancellationToken);

            if (municipio is null)
                resultado.AgregarError($"El MunicipioId {municipioId} no existe en el catálogo DIAN_Municipios.");
            else if (departamentoId.HasValue && municipio.DepartamentoId != departamentoId.Value)
                resultado.AgregarError($"El MunicipioId {municipioId} no pertenece al DepartamentoId {departamentoId} informado en el cliente.");
            else if (!int.TryParse(municipio.CodigoMunicipio, out var codigoMunicipio))
                resultado.AgregarError($"El municipio '{municipio.NombreMunicipio}' no tiene un CodigoMunicipio numérico válido en el catálogo.");
            else
                resultado.CodigoMunicipio = codigoMunicipio;
        }

        // Regla del Anexo 2: si el país de residencia es Colombia, dpto/mun siempre
        // deben diligenciarse. Antes no podíamos validar esto sin adivinar el código
        // DIAN de Colombia; ahora se resuelve por nombre contra el catálogo real.
        if (esColombia && (!departamentoId.HasValue || !municipioId.HasValue))
            resultado.AgregarError("El país de residencia es Colombia: dpto y mun son obligatorios.");

        return resultado;
    }
}
