using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;

namespace MMagnetic.ExogenaService.Services.Clientes;

public class ResolutorCatalogoDianService : IResolutorCatalogoDianService
{
    private readonly DianDbContext _dian;

    public ResolutorCatalogoDianService(DianDbContext dian)
    {
        _dian = dian;
    }

    public async Task<ResultadoResolucionCatalogo> ResolverAsync(
        string? codigoPais,
        string? codigoDepartamento,
        string? codigoMunicipio,
        CancellationToken cancellationToken = default)
    {
        var errores = new List<string>();
        int? paisId = null;
        int? departamentoId = null;
        int? municipioId = null;

        if (!string.IsNullOrWhiteSpace(codigoPais))
        {
            var pais = await _dian.Paises.AsNoTracking()
                .FirstOrDefaultAsync(p => p.CodigoPais == codigoPais, cancellationToken);

            if (pais is null)
                errores.Add($"El código de país '{codigoPais}' no existe en el catálogo DIAN.");
            else
                paisId = pais.PaisId;
        }

        if (!string.IsNullOrWhiteSpace(codigoDepartamento))
        {
            var query = _dian.Departamentos.AsNoTracking().Where(d => d.CodigoDepartamento == codigoDepartamento);
            if (paisId.HasValue)
                query = query.Where(d => d.PaisId == paisId.Value);

            var departamento = await query.FirstOrDefaultAsync(cancellationToken);

            if (departamento is null)
                errores.Add($"El código de departamento '{codigoDepartamento}' no existe en el catálogo DIAN.");
            else
                departamentoId = departamento.DepartamentoId;
        }

        if (!string.IsNullOrWhiteSpace(codigoMunicipio))
        {
            var query = _dian.Municipios.AsNoTracking().Where(m => m.CodigoMunicipio == codigoMunicipio);
            if (departamentoId.HasValue)
                query = query.Where(m => m.DepartamentoId == departamentoId.Value);
            else if (paisId.HasValue)
                query = query.Where(m => m.PaisId == paisId.Value);

            var municipios = await query.ToListAsync(cancellationToken);

            if (municipios.Count == 0)
                errores.Add($"El código de municipio '{codigoMunicipio}' no existe en el catálogo DIAN para el departamento indicado.");
            else if (municipios.Count > 1)
                errores.Add($"El código de municipio '{codigoMunicipio}' es ambiguo sin especificar el departamento (existe en varios departamentos).");
            else
                municipioId = municipios[0].MunicipioId;
        }

        return new ResultadoResolucionCatalogo(paisId, departamentoId, municipioId, errores);
    }
}
