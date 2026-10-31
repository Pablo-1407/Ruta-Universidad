using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;
using gestión_de_residuos_sólidos_en_campus_universitarios.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Campus.Tests;

[TestClass]
public sealed class ProgramacionServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void GenerarProgramacion_SinDensidadReciclable_IndicaLosDatosPendientes()
    {
        CampusData campus = CrearCampus(CrearPunto(TipoResiduo.Reciclable));

        DensidadesResiduosIncompletasException excepcion = Assert.ThrowsException<DensidadesResiduosIncompletasException>(
            () => new ProgramacionService().GenerarProgramacion(campus, Ahora));

        Assert.AreEqual(1, excepcion.Problemas.Count);
        Assert.AreEqual(TipoResiduo.Reciclable, excepcion.Problemas[0].Tipo);
        StringAssert.Contains(string.Join("; ", excepcion.Problemas[0].InformacionPendiente), "configuración para este residuo");
        StringAssert.Contains(string.Join("; ", excepcion.Problemas[0].InformacionPendiente), "fuente documental");
    }

    [TestMethod]
    public void GenerarProgramacion_ConDensidadesDuplicadas_IndicaQueDebeQuedarUna()
    {
        CampusData campus = CrearCampus(CrearPunto(TipoResiduo.Reciclable));
        AgregarDensidad(campus, TipoResiduo.Reciclable, 0.4, "Fuente A");
        AgregarDensidad(campus, TipoResiduo.Reciclable, 0.5, "Fuente B");

        DensidadesResiduosIncompletasException excepcion = Assert.ThrowsException<DensidadesResiduosIncompletasException>(
            () => new ProgramacionService().GenerarProgramacion(campus, Ahora));

        StringAssert.Contains(excepcion.Problemas[0].InformacionPendiente[0], "hay 2 configuraciones");
        StringAssert.Contains(excepcion.Problemas[0].InformacionPendiente[0], "debe quedar una sola");
    }

    [TestMethod]
    public void GenerarProgramacion_ConDensidadNoPositiva_IndicaElCampoPendiente()
    {
        CampusData campus = CrearCampus(CrearPunto(TipoResiduo.Reciclable));
        AgregarDensidad(campus, TipoResiduo.Reciclable, 0, "Fuente documentada");

        DensidadesResiduosIncompletasException excepcion = Assert.ThrowsException<DensidadesResiduosIncompletasException>(
            () => new ProgramacionService().GenerarProgramacion(campus, Ahora));

        StringAssert.Contains(excepcion.Problemas[0].InformacionPendiente[0], "densidad positiva y finita");
    }

    [TestMethod]
    public void GenerarProgramacion_ConDensidadNoFinita_IndicaElCampoPendiente()
    {
        CampusData campus = CrearCampus(CrearPunto(TipoResiduo.Reciclable));
        AgregarDensidad(campus, TipoResiduo.Reciclable, double.NaN, "Fuente documentada");

        DensidadesResiduosIncompletasException excepcion = Assert.ThrowsException<DensidadesResiduosIncompletasException>(
            () => new ProgramacionService().GenerarProgramacion(campus, Ahora));

        StringAssert.Contains(excepcion.Problemas[0].InformacionPendiente[0], "densidad positiva y finita");
    }

    [TestMethod]
    public void GenerarProgramacion_SinFuenteDensidad_IndicaLaFuentePendiente()
    {
        CampusData campus = CrearCampus(CrearPunto(TipoResiduo.Reciclable));
        AgregarDensidad(campus, TipoResiduo.Reciclable, 0.4, " ");

        DensidadesResiduosIncompletasException excepcion = Assert.ThrowsException<DensidadesResiduosIncompletasException>(
            () => new ProgramacionService().GenerarProgramacion(campus, Ahora));

        StringAssert.Contains(excepcion.Problemas[0].InformacionPendiente[0], "fuente documental");
    }

    [TestMethod]
    public void GenerarProgramacion_ConVariosTiposIncompletos_AcumulaLosProblemas()
    {
        CampusData campus = CrearCampus(
            CrearPunto(TipoResiduo.Reciclable, id: 2),
            CrearPunto(TipoResiduo.Organico, id: 3));
        AgregarDensidad(campus, TipoResiduo.Organico, 0, " ");

        DensidadesResiduosIncompletasException excepcion = Assert.ThrowsException<DensidadesResiduosIncompletasException>(
            () => new ProgramacionService().GenerarProgramacion(campus, Ahora));

        Assert.AreEqual(2, excepcion.Problemas.Count);
        CollectionAssert.AreEquivalent(
            new[] { TipoResiduo.Reciclable, TipoResiduo.Organico },
            excepcion.Problemas.Select(problema => problema.Tipo).ToArray());
        Assert.AreEqual(2, excepcion.Problemas.Single(problema => problema.Tipo == TipoResiduo.Organico).InformacionPendiente.Count);
    }

    [TestMethod]
    public void GenerarProgramacion_ConDensidadDocumentada_GeneraLaProgramacion()
    {
        CampusData campus = CrearCampus(CrearPunto(TipoResiduo.Reciclable));
        AgregarDensidad(campus, TipoResiduo.Reciclable, 0.5, "Fuente documentada");

        IReadOnlyList<ProgramacionRecoleccion> programaciones = new ProgramacionService().GenerarProgramacion(campus, Ahora);

        Assert.AreEqual(1, programaciones.Count);
        Assert.AreEqual(Ahora.AddDays(35), programaciones[0].FechaProgramada);
        Assert.AreEqual(80, programaciones[0].VolumenEstimadoLitros, 0.001);
    }

    [TestMethod]
    public void GenerarProgramacion_SinGeneracionYPorDebajoDelUmbral_NoExigeDensidad()
    {
        CampusData campus = CrearCampus(CrearPunto(TipoResiduo.Reciclable, generacionKgDia: 0));

        IReadOnlyList<ProgramacionRecoleccion> programaciones = new ProgramacionService().GenerarProgramacion(campus, Ahora);

        Assert.AreEqual(0, programaciones.Count);
    }

    [TestMethod]
    public void GenerarProgramacion_ConPuntoSinClasificar_MantieneErrorDeClasificacion()
    {
        CampusData campus = CrearCampus(CrearPunto(TipoResiduo.SinClasificar));

        ArgumentException excepcion = Assert.ThrowsException<ArgumentException>(
            () => new ProgramacionService().GenerarProgramacion(campus, Ahora));

        Assert.AreEqual(nameof(PuntoRecoleccion.TipoResiduo), excepcion.ParamName);
    }

    private static CampusData CrearCampus(params PuntoRecoleccion[] puntos)
    {
        CampusData campus = new();
        campus.Puntos.Add(new PuntoRecoleccion
        {
            Id = 1,
            Nombre = "Centro de acopio",
            Tipo = TipoPunto.CentroAcopio
        });
        campus.Puntos.AddRange(puntos);
        return campus;
    }

    private static PuntoRecoleccion CrearPunto(
        TipoResiduo tipo,
        double nivelLlenadoPct = 10,
        double generacionKgDia = 1,
        int id = 2) => new()
    {
        Id = id,
        Nombre = $"Punto {id}",
        Tipo = TipoPunto.Recoleccion,
        TipoResiduo = tipo,
        CapacidadLitros = 100,
        NivelLlenadoPct = nivelLlenadoPct,
        GeneracionKgDia = generacionKgDia
    };

    private static void AgregarDensidad(CampusData campus, TipoResiduo tipo, double densidadKgPorLitro, string fuente) =>
        campus.ParametrosResiduos.Add(new ParametrosResiduo
        {
            Tipo = tipo,
            DensidadKgPorLitro = densidadKgPorLitro,
            FuenteDensidad = fuente
        });
}
