using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MMagnetic.ExogenaService.Models.Formato1019;

namespace MMagnetic.ExogenaService.Services.Formato1019;

/// <summary>
/// Valida un Formato 1019 (Movimiento en Cuenta Corriente y/o Ahorro) contra las reglas
/// estructurales del XSD y las validaciones de negocio de la Resolución 000162 de 2023,
/// Anexo No. 2, sección 3.
/// </summary>
public class Formato1019Validator : IFormato1019Validator
{
    private const int FormatoFijo = 1019;
    private const int VersionFija = 9;
    private const int MaxRegistros = 5000;

    private const decimal RangoSaldoMinimo = -99999999999999999999m;
    private const decimal RangoSaldoMaximo = 99999999999999999999m;

    private static readonly Regex PatronAlfanumerico = new(@"^[a-zA-Z0-9]+$", RegexOptions.Compiled);

    public Formato1019ValidationResult Validar(CargaMasiva1019 carga)
    {
        var resultado = new Formato1019ValidationResult();

        if (carga is null)
        {
            resultado.AgregarError("MAS-001", "El documento no contiene el elemento raíz 'mas'.");
            return resultado;
        }

        ValidarEncabezado(carga, resultado);
        ValidarRegistros(carga, resultado);

        return resultado;
    }

    public Formato1019ValidationResult ValidarRegistro(MovimientoCuenta movimiento)
    {
        var resultado = new Formato1019ValidationResult();
        ValidarMovimiento(movimiento, "movcta", resultado);
        return resultado;
    }

    private static void ValidarEncabezado(CargaMasiva1019 carga, Formato1019ValidationResult resultado)
    {
        var cab = carga.Cab;
        if (cab is null)
        {
            resultado.AgregarError("CAB-001", "El encabezado 'Cab' es obligatorio.");
            return;
        }

        var anoActual = DateTime.UtcNow.Year;
        if (cab.Ano < 2000 || cab.Ano > anoActual + 1)
            resultado.AgregarError("CAB-ANO", $"Año de envío inválido: {cab.Ano}.", "Cab.Ano");

        if (cab.CodCpt != 1 && cab.CodCpt != 2)
            resultado.AgregarError("CAB-CODCPT", "CodCpt debe ser 1 (Inserción) o 2 (Reemplazo).", "Cab.CodCpt");

        if (cab.Formato != FormatoFijo)
            resultado.AgregarError("CAB-FORMATO", $"Formato debe ser {FormatoFijo}.", "Cab.Formato");

        if (cab.Version != VersionFija)
            resultado.AgregarError("CAB-VERSION", $"Version debe ser {VersionFija}.", "Cab.Version");

        if (cab.NumEnvio <= 0 || cab.NumEnvio > 99_999_999)
            resultado.AgregarError("CAB-NUMENVIO", "NumEnvio debe ser un consecutivo positivo de máximo 8 dígitos.", "Cab.NumEnvio");

        if (cab.FecEnvio == default)
            resultado.AgregarError("CAB-FECENVIO", "FecEnvio es obligatoria y debe ser una fecha calendario válida.", "Cab.FecEnvio");

        if (cab.FecInicial == default || cab.FecFinal == default)
            resultado.AgregarError("CAB-FECHAS", "FecInicial y FecFinal son obligatorias y deben ser fechas calendario válidas.", "Cab.FecInicial/FecFinal");
        else if (cab.FecInicial > cab.FecFinal)
            resultado.AgregarError("CAB-RANGOFECHAS", "FecInicial no puede ser posterior a FecFinal.", "Cab.FecInicial/FecFinal");

        if (cab.CantReg != carga.Movimientos.Count)
            resultado.AgregarError(
                "CAB-CANTREG",
                $"CantReg ({cab.CantReg}) no coincide con la cantidad real de registros 'movcta' ({carga.Movimientos.Count}).",
                "Cab.CantReg");

        // ValorTotal = sumatoria del atributo "codex" de todos los "movcta" (Anexo 2, sección 2.1).
        var sumaCodex = carga.Movimientos.Sum(m => (decimal)m.Codex);
        if (cab.ValorTotal != sumaCodex)
            resultado.AgregarError(
                "CAB-VALORTOTAL",
                $"ValorTotal ({cab.ValorTotal}) no coincide con la sumatoria de 'codex' de todos los movcta ({sumaCodex}).",
                "Cab.ValorTotal");
    }

    private static void ValidarRegistros(CargaMasiva1019 carga, Formato1019ValidationResult resultado)
    {
        if (carga.Movimientos.Count == 0)
        {
            resultado.AgregarError("MOV-000", "El archivo debe contener al menos un registro 'movcta'.");
            return;
        }

        if (carga.Movimientos.Count > MaxRegistros)
            resultado.AgregarError(
                "MOV-MAXREG",
                $"El archivo supera el máximo de {MaxRegistros} registros 'movcta' permitidos por envío.");

        var llavesVistas = new HashSet<string>();

        for (var i = 0; i < carga.Movimientos.Count; i++)
        {
            var mov = carga.Movimientos[i];
            var contexto = $"movcta[{i}]";

            ValidarMovimiento(mov, contexto, resultado);

            // Llave única del formato: tdoc + nid + cta + tipcta no debe repetirse en el envío.
            var llave = $"{mov.Tdoc}|{mov.Nid}|{mov.Cta}|{mov.TipCta}";
            if (!llavesVistas.Add(llave))
                resultado.AgregarError(
                    "MOV-LLAVE-DUP",
                    $"La combinación tdoc+nid+cta+tipcta ({llave}) está duplicada.",
                    contexto);
        }
    }

    private static void ValidarMovimiento(MovimientoCuenta mov, string contexto, Formato1019ValidationResult resultado)
    {
        if (mov.Tdoc < 0 || mov.Tdoc > 99)
            resultado.AgregarError("MOV-TDOC", "tdoc es obligatorio y debe estar entre 0 y 99.", contexto);

        if (string.IsNullOrWhiteSpace(mov.Nid) || mov.Nid.Length > 20 || !PatronAlfanumerico.IsMatch(mov.Nid))
            resultado.AgregarError("MOV-NID", "nid es obligatorio, alfanumérico, sin guiones/puntos/espacios, de máximo 20 caracteres.", contexto);

        if (mov.DvSpecified && (mov.Dv < 0 || mov.Dv > 9))
            resultado.AgregarError("MOV-DV", "dv debe estar entre 0 y 9 cuando se informa.", contexto);

        if (mov.Apl1?.Length > 60 || mov.Apl2?.Length > 60 || mov.Nom1?.Length > 60 || mov.Nom2?.Length > 60)
            resultado.AgregarError("MOV-NOMBRES", "apl1/apl2/nom1/nom2 no pueden superar 60 caracteres.", contexto);

        if (mov.Raz?.Length > 450)
            resultado.AgregarError("MOV-RAZ", "raz no puede superar 450 caracteres.", contexto);

        // El esquema no distingue persona natural/jurídica explícitamente; se infiere por
        // la presencia de "raz" (jurídica) o "apl1"/"nom1" (natural), tal como exige el
        // Anexo 2. Si más adelante se conoce el catálogo real de tdoc, esto se puede
        // reemplazar por una regla exacta basada en el tipo de documento.
        var esPersonaJuridica = !string.IsNullOrWhiteSpace(mov.Raz);
        var esPersonaNatural = !string.IsNullOrWhiteSpace(mov.Apl1) || !string.IsNullOrWhiteSpace(mov.Nom1);
        if (!esPersonaJuridica && !esPersonaNatural)
            resultado.AgregarError(
                "MOV-TITULAR",
                "Debe diligenciarse 'raz' (persona jurídica) o 'apl1'/'nom1' (persona natural) del titular.",
                contexto);

        if (mov.Dir?.Length > 200)
            resultado.AgregarError("MOV-DIR", "dir no puede superar 200 caracteres.", contexto);

        if (mov.DptoSpecified && (mov.Dpto < 0 || mov.Dpto > 99))
            resultado.AgregarError("MOV-DPTO", "dpto debe estar entre 0 y 99 cuando se informa.", contexto);

        if (mov.MunSpecified && (mov.Mun < 0 || mov.Mun > 999))
            resultado.AgregarError("MOV-MUN", "mun debe estar entre 0 y 999 cuando se informa.", contexto);

        if (mov.PaisSpecified && (mov.Pais < 0 || mov.Pais > 9999))
            resultado.AgregarError("MOV-PAIS", "pais debe estar entre 0 y 9999 cuando se informa.", contexto);

        if (string.IsNullOrWhiteSpace(mov.Cta) || mov.Cta.Length > 20)
            resultado.AgregarError("MOV-CTA", "cta es obligatorio y de máximo 20 caracteres.", contexto);

        if (mov.TipCta < 0 || mov.TipCta > 9)
            resultado.AgregarError("MOV-TIPCTA", "tipcta es obligatorio y debe estar entre 0 y 9.", contexto);

        if (mov.Codex < 0 || mov.Codex > 99)
            resultado.AgregarError("MOV-CODEX", "codex es obligatorio y debe estar entre 0 y 99.", contexto);

        // sal/psaldof/meddia/smax/smin son saldos: pueden ser negativos (llevan signo "-").
        ValidarRangoSaldo(mov.Sal, "sal", contexto, resultado);
        ValidarRangoSaldo(mov.PSaldoF, "psaldof", contexto, resultado);
        ValidarRangoSaldo(mov.MedDia, "meddia", contexto, resultado);
        ValidarRangoSaldo(mov.SMax, "smax", contexto, resultado);
        ValidarRangoSaldo(mov.SMin, "smin", contexto, resultado);

        // vcred/procre/medcre/vmovdeb/pordeb: enteros positivos, máx. 20 dígitos, sin signo ni decimales.
        ValidarEnteroPositivo(mov.VCred, 20, "vcred", contexto, resultado);
        ValidarEnteroPositivo(mov.ProCre, 20, "procre", contexto, resultado);
        ValidarEnteroPositivo(mov.MedCre, 20, "medcre", contexto, resultado);
        ValidarEnteroPositivo(mov.VMovDeb, 20, "vmovdeb", contexto, resultado);
        ValidarEnteroPositivo(mov.PorDeb, 20, "pordeb", contexto, resultado);

        // movcre/nmovdeb: conteos de movimientos, enteros positivos de máx. 7 dígitos.
        ValidarEnteroPositivo(mov.MovCre, 7, "movcre", contexto, resultado);
        ValidarEnteroPositivo(mov.NMovDeb, 7, "nmovdeb", contexto, resultado);

        if (mov.TitularesSecundarios.Count > 0)
            ValidarTitularesSecundarios(mov, contexto, resultado);
    }

    private static void ValidarTitularesSecundarios(MovimientoCuenta mov, string contextoPadre, Formato1019ValidationResult resultado)
    {
        var llavesVistas = new HashSet<string>();

        for (var i = 0; i < mov.TitularesSecundarios.Count; i++)
        {
            var tit = mov.TitularesSecundarios[i];
            var contexto = $"{contextoPadre}.titSec[{i}]";

            if (tit.Cpts < 0 || tit.Cpts > 9)
                resultado.AgregarError("TITSEC-CPTS", "cpts es obligatorio y debe estar entre 0 y 9.", contexto);

            if (tit.Tdocs < 0 || tit.Tdocs > 99)
                resultado.AgregarError("TITSEC-TDOCS", "tdocs es obligatorio y debe estar entre 0 y 99.", contexto);

            if (string.IsNullOrWhiteSpace(tit.Nids) || tit.Nids.Length > 20 || !PatronAlfanumerico.IsMatch(tit.Nids))
                resultado.AgregarError("TITSEC-NIDS", "nids es obligatorio, alfanumérico y de máximo 20 caracteres.", contexto);

            if (tit.DvsSpecified && (tit.Dvs < 0 || tit.Dvs > 9))
                resultado.AgregarError("TITSEC-DVS", "dvs debe estar entre 0 y 9 cuando se informa.", contexto);

            if (tit.Apl1s?.Length > 60 || tit.Apl2s?.Length > 60 || tit.Nom1s?.Length > 60 || tit.Nom2s?.Length > 60)
                resultado.AgregarError("TITSEC-NOMBRES", "apl1s/apl2s/nom1s/nom2s no pueden superar 60 caracteres.", contexto);

            if (tit.Razs?.Length > 450)
                resultado.AgregarError("TITSEC-RAZS", "razs no puede superar 450 caracteres.", contexto);

            // Llave del titular secundario: cpts + tdocs + nids no debe repetirse dentro del mismo movcta.
            var llave = $"{tit.Cpts}|{tit.Tdocs}|{tit.Nids}";
            if (!llavesVistas.Add(llave))
                resultado.AgregarError(
                    "TITSEC-LLAVE-DUP",
                    $"La combinación cpts+tdocs+nids ({llave}) está duplicada dentro del mismo movcta.",
                    contexto);
        }
    }

    private static void ValidarRangoSaldo(decimal valor, string atributo, string contexto, Formato1019ValidationResult resultado)
    {
        if (valor < RangoSaldoMinimo || valor > RangoSaldoMaximo)
            resultado.AgregarError(
                $"MOV-{atributo.ToUpperInvariant()}",
                $"{atributo} está fuera del rango permitido.",
                contexto);
    }

    private static void ValidarEnteroPositivo(decimal valor, int longitudMaxima, string atributo, string contexto, Formato1019ValidationResult resultado)
    {
        if (!EsEnteroPositivoConLongitud(valor, longitudMaxima))
            resultado.AgregarError(
                $"MOV-{atributo.ToUpperInvariant()}",
                $"{atributo} debe ser un entero positivo (sin signos ni decimales) de máximo {longitudMaxima} dígitos.",
                contexto);
    }

    private static bool EsEnteroPositivoConLongitud(decimal valor, int longitudMaxima)
    {
        if (valor < 0 || valor != decimal.Truncate(valor))
            return false;

        var digitos = decimal.Truncate(valor).ToString(CultureInfo.InvariantCulture);
        return digitos.Length <= longitudMaxima;
    }
}
