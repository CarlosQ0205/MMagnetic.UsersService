using FluentAssertions;
using MMagnetic.ExogenaService.Models.Formato1019;
using MMagnetic.ExogenaService.Services.Formato1019;

namespace MMagnetic.ExogenaService.XunitTests;

public class Formato1019ValidatorTests
{
    private readonly Formato1019Validator _validator = new();

    private static CargaMasiva1019 CargaValida()
    {
        var mov1 = new MovimientoCuenta
        {
            Tdoc = 13,
            Nid = "123456789",
            Apl1 = "Perez",
            Nom1 = "Juan",
            Cta = "1234567890",
            TipCta = 1,
            Codex = 5,
            Sal = 1_000_000,
            PSaldoF = 900_000,
            MedDia = 850_000,
            SMax = 1_200_000,
            SMin = 500_000,
            VCred = 200_000,
            MovCre = 10,
            ProCre = 20_000,
            MedCre = 15_000,
            VMovDeb = 150_000,
            NMovDeb = 8,
            PorDeb = 18_000,
        };

        var mov2 = new MovimientoCuenta
        {
            Tdoc = 31,
            Nid = "900123456",
            Dv = 7,
            DvSpecified = true,
            Raz = "Empresa SAS",
            Cta = "9876543210",
            TipCta = 2,
            Codex = 10,
            Sal = -50_000,
            PSaldoF = -40_000,
            MedDia = -30_000,
            SMax = 10_000,
            SMin = -60_000,
            VCred = 300_000,
            MovCre = 5,
            ProCre = 60_000,
            MedCre = 50_000,
            VMovDeb = 100_000,
            NMovDeb = 3,
            PorDeb = 33_000,
        };

        return new CargaMasiva1019
        {
            Cab = new CabeceraFormato1019
            {
                Ano = 2024,
                CodCpt = 1,
                NumEnvio = 1,
                FecEnvio = new DateTime(2024, 1, 15, 10, 30, 0),
                FecInicial = new DateTime(2024, 1, 1),
                FecFinal = new DateTime(2024, 1, 31),
                ValorTotal = 15, // 5 + 10 (suma de "codex")
                CantReg = 2,
            },
            Movimientos = [mov1, mov2],
        };
    }

    [Fact]
    public void Carga_valida_no_genera_errores()
    {
        var resultado = _validator.Validar(CargaValida());

        resultado.EsValido.Should().BeTrue(because: string.Join("; ", resultado.Errores.Select(e => e.Mensaje)));
    }

    [Fact]
    public void CantReg_que_no_coincide_con_la_cantidad_real_de_registros_genera_error()
    {
        var carga = CargaValida();
        carga.Cab.CantReg = 5;

        var resultado = _validator.Validar(carga);

        resultado.Errores.Should().Contain(e => e.Codigo == "CAB-CANTREG");
    }

    [Fact]
    public void ValorTotal_que_no_coincide_con_la_suma_de_codex_genera_error()
    {
        var carga = CargaValida();
        carga.Cab.ValorTotal = 999;

        var resultado = _validator.Validar(carga);

        resultado.Errores.Should().Contain(e => e.Codigo == "CAB-VALORTOTAL");
    }

    [Fact]
    public void Tdoc_fuera_de_rango_genera_error()
    {
        var carga = CargaValida();
        carga.Movimientos[0].Tdoc = -1;

        var resultado = _validator.Validar(carga);

        resultado.Errores.Should().Contain(e => e.Codigo == "MOV-TDOC");
    }

    [Fact]
    public void Nid_vacio_genera_error()
    {
        var carga = CargaValida();
        carga.Movimientos[0].Nid = "";

        var resultado = _validator.Validar(carga);

        resultado.Errores.Should().Contain(e => e.Codigo == "MOV-NID");
    }

    [Fact]
    public void Llave_duplicada_tdoc_nid_cta_tipcta_genera_error()
    {
        var carga = CargaValida();
        carga.Movimientos[1].Tdoc = carga.Movimientos[0].Tdoc;
        carga.Movimientos[1].Nid = carga.Movimientos[0].Nid;
        carga.Movimientos[1].Cta = carga.Movimientos[0].Cta;
        carga.Movimientos[1].TipCta = carga.Movimientos[0].TipCta;
        // Debe recalcularse CantReg/ValorTotal para que ese error no oculte el que probamos.
        carga.Cab.CantReg = carga.Movimientos.Count;
        carga.Cab.ValorTotal = carga.Movimientos.Sum(m => m.Codex);

        var resultado = _validator.Validar(carga);

        resultado.Errores.Should().Contain(e => e.Codigo == "MOV-LLAVE-DUP");
    }

    [Fact]
    public void Vcred_con_decimales_genera_error_porque_el_patron_exige_solo_digitos()
    {
        var carga = CargaValida();
        carga.Movimientos[0].VCred = 123.45m;

        var resultado = _validator.Validar(carga);

        resultado.Errores.Should().Contain(e => e.Codigo == "MOV-VCRED");
    }

    [Fact]
    public void Vcred_negativo_genera_error()
    {
        var carga = CargaValida();
        carga.Movimientos[0].VCred = -100;

        var resultado = _validator.Validar(carga);

        resultado.Errores.Should().Contain(e => e.Codigo == "MOV-VCRED");
    }

    [Fact]
    public void Saldo_negativo_no_genera_error_porque_si_esta_permitido()
    {
        var carga = CargaValida();
        carga.Movimientos[0].Sal = -1_000_000;

        var resultado = _validator.Validar(carga);

        resultado.Errores.Should().NotContain(e => e.Codigo == "MOV-SAL");
    }

    [Fact]
    public void Titular_sin_raz_ni_apl1_nom1_genera_error()
    {
        var carga = CargaValida();
        carga.Movimientos[0].Apl1 = null;
        carga.Movimientos[0].Nom1 = null;
        carga.Movimientos[0].Raz = null;

        var resultado = _validator.Validar(carga);

        resultado.Errores.Should().Contain(e => e.Codigo == "MOV-TITULAR");
    }

    [Fact]
    public void Mas_de_5000_registros_genera_error()
    {
        var carga = CargaValida();
        var plantilla = carga.Movimientos[0];
        for (var i = 0; i < 5000; i++)
        {
            carga.Movimientos.Add(new MovimientoCuenta
            {
                Tdoc = plantilla.Tdoc,
                Nid = $"NID{i:D10}",
                Apl1 = plantilla.Apl1,
                Nom1 = plantilla.Nom1,
                Cta = $"CTA{i:D10}",
                TipCta = plantilla.TipCta,
                Codex = plantilla.Codex,
                Sal = plantilla.Sal,
                PSaldoF = plantilla.PSaldoF,
                MedDia = plantilla.MedDia,
                SMax = plantilla.SMax,
                SMin = plantilla.SMin,
                VCred = plantilla.VCred,
                MovCre = plantilla.MovCre,
                ProCre = plantilla.ProCre,
                MedCre = plantilla.MedCre,
                VMovDeb = plantilla.VMovDeb,
                NMovDeb = plantilla.NMovDeb,
                PorDeb = plantilla.PorDeb,
            });
        }

        var resultado = _validator.Validar(carga);

        resultado.Errores.Should().Contain(e => e.Codigo == "MOV-MAXREG");
    }
}
