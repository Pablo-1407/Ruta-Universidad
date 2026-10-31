using gestión_de_residuos_sólidos_en_campus_universitarios.Algorithms.Priorizacion;
using gestión_de_residuos_sólidos_en_campus_universitarios.Algorithms.Rutas;
using gestión_de_residuos_sólidos_en_campus_universitarios.Data;
using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;
using gestión_de_residuos_sólidos_en_campus_universitarios.Presenters;
using gestión_de_residuos_sólidos_en_campus_universitarios.Services;
using gestión_de_residuos_sólidos_en_campus_universitarios.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Campus.Tests;

[TestClass]
public sealed class ErrorHandlingTests
{
    [TestMethod]
    public void ManejadorMuestraExcepcionConocidaComoAviso()
    {
        string? mensajeMostrado = null;
        bool esError = false;
        ManejadorErrores manejador = new((mensaje, error) =>
        {
            mensajeMostrado = mensaje;
            esError = error;
        });

        manejador.Manejar(new InvalidOperationException("Falta el centro de acopio."), "programar");

        Assert.IsTrue(esError);
        StringAssert.Contains(mensajeMostrado, "Falta el centro de acopio.");
    }

    [TestMethod]
    public void ManejadorRegistraExcepcionInesperadaEnTxt()
    {
        string marcador = $"error-{Guid.NewGuid():N}";
        string? mensajeMostrado = null;
        ManejadorErrores manejador = new((mensaje, _) => mensajeMostrado = mensaje);

        manejador.Manejar(new FormatException(marcador), "prueba");

        StringAssert.Contains(mensajeMostrado, "Referencia:");
        string rutaRegistro = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Ruta-Universidad",
            "errores.txt");
        Assert.IsTrue(File.Exists(rutaRegistro));
        StringAssert.Contains(File.ReadAllText(rutaRegistro), marcador);
    }

    [TestMethod]
    public void FalloAlActualizarVistaDescartaCambioTemporal()
    {
        VistaPruebas vista = new()
        {
            FallarAlMostrarPuntos = true,
            PuntoSolicitado = new PuntoRecoleccion
            {
                Nombre = "Punto temporal",
                TipoResiduo = TipoResiduo.Organico,
                CapacidadLitros = 100
            }
        };
        RutaPresenter presenter = CrearPresenter(vista, new RepositorioPruebas());
        presenter.Inicializar();

        vista.PulsarMapa(0.25, 0.5);

        Assert.AreEqual(0, vista.CampusMostrado.Puntos.Count);
        StringAssert.Contains(vista.UltimoMensaje, "falló al mostrar");
    }

    [TestMethod]
    public async Task ExcepcionDeTareaMuestraMensajeYLiberaInterfaz()
    {
        VistaPruebas vista = new();
        RutaPresenter presenter = CrearPresenter(vista, new RepositorioPruebas());
        presenter.Inicializar();

        vista.SolicitarProgramacion();
        await vista.EsperarMensaje();

        Assert.IsFalse(vista.Ocupada);
        StringAssert.Contains(vista.UltimoMensaje, "exactamente un centro de acopio");
        Assert.AreEqual(0, vista.CampusMostrado.Puntos.Count);
    }

    [TestMethod]
    public async Task FalloAlGuardarTxtSeNotificaYLiberaInterfaz()
    {
        VistaPruebas vista = new()
        {
            RutaCampus = Path.Combine(Path.GetTempPath(), $"campus-{Guid.NewGuid():N}.txt")
        };
        RepositorioPruebas repositorio = new() { ErrorAlGuardar = new IOException("archivo bloqueado") };
        RutaPresenter presenter = CrearPresenter(vista, repositorio);
        presenter.Inicializar();

        vista.SolicitarGuardado();
        await vista.EsperarMensaje();

        Assert.IsFalse(vista.Ocupada);
        StringAssert.Contains(vista.UltimoMensaje, "No se pudo acceder al archivo.");
        Assert.IsFalse(File.Exists(vista.RutaCampus));
    }

    [TestMethod]
    public void RepositorioRechazaTxtMalformadoYElManejadorLoPresenta()
    {
        string ruta = Path.Combine(Path.GetTempPath(), $"campus-{Guid.NewGuid():N}.txt");
        File.WriteAllText(ruta, "contenido no válido");
        try
        {
            InvalidDataException excepcion = Assert.ThrowsException<InvalidDataException>(
                () => new TxtCampusRepository().Cargar(ruta));
            string? mensajeMostrado = null;
            ManejadorErrores manejador = new((mensaje, _) => mensajeMostrado = mensaje);

            manejador.Manejar(excepcion, "cargar campus");

            StringAssert.Contains(mensajeMostrado, "formato de campus esperado");
        }
        finally
        {
            File.Delete(ruta);
        }
    }

    [TestMethod]
    public async Task ProgramarConDensidadIncompleta_AlCancelarConservaElCampus()
    {
        CampusData campus = CrearCampusConReciclable();
        VistaPruebas vista = new();
        RutaPresenter presenter = CrearPresenter(vista, new RepositorioPruebas { CampusAlCargar = campus });
        presenter.Inicializar();
        vista.SolicitarCargaCampus();

        vista.SolicitarProgramacion();
        await vista.EsperarAvisoConfiguracion();

        StringAssert.Contains(vista.MensajeConfiguracion, "Reciclable");
        StringAssert.Contains(vista.MensajeConfiguracion, "fuente documental");
        Assert.AreEqual(0, vista.SolicitudesConfiguracion);
        Assert.AreSame(campus, vista.CampusMostrado);
        Assert.AreEqual(0, vista.CampusMostrado.Programaciones.Count);
    }

    [TestMethod]
    public async Task ProgramarConDensidadIncompleta_AlConfigurarPasaTiposYNoReintenta()
    {
        CampusData campus = CrearCampusConReciclable();
        VistaPruebas vista = new()
        {
            RespuestaConfigurarResiduos = true,
            AceptarDialogoConfiguracion = true,
            ResiduosConfigurados =
            [
                new ParametrosResiduo
                {
                    Tipo = TipoResiduo.Reciclable,
                    DensidadKgPorLitro = 0.42,
                    FuenteDensidad = "Estudio técnico"
                }
            ]
        };
        RutaPresenter presenter = CrearPresenter(vista, new RepositorioPruebas { CampusAlCargar = campus });
        presenter.Inicializar();
        vista.SolicitarCargaCampus();

        vista.SolicitarProgramacion();
        await vista.EsperarCampusConDensidades();

        CollectionAssert.AreEqual(new[] { TipoResiduo.Reciclable }, vista.TiposDensidadPendiente?.ToArray());
        Assert.AreEqual(0, vista.CampusMostrado.Programaciones.Count);
        Assert.IsNull(vista.UltimoMensaje);
    }

    private static CampusData CrearCampusConReciclable()
    {
        CampusData campus = new();
        campus.Puntos.Add(new PuntoRecoleccion
        {
            Id = 1,
            Nombre = "Centro de acopio",
            Tipo = TipoPunto.CentroAcopio
        });
        campus.Puntos.Add(new PuntoRecoleccion
        {
            Id = 2,
            Nombre = "Punto reciclable",
            Tipo = TipoPunto.Recoleccion,
            TipoResiduo = TipoResiduo.Reciclable,
            X = 0.5,
            Y = 0.5,
            CapacidadLitros = 100,
            NivelLlenadoPct = 10,
            GeneracionKgDia = 1
        });
        return campus;
    }

    private static RutaPresenter CrearPresenter(VistaPruebas vista, IPuntoRepository repositorio)
    {
        RecoleccionService servicio = new(
            new PriorizacionPonderada(),
            new VecinoMasCercano(),
            new RutaSinOptimizar(),
            new IndicadoresService());
        ManejadorErrores manejador = new(vista.MostrarMensaje);
        return new RutaPresenter(vista, repositorio, servicio, manejador);
    }

    private sealed class RepositorioPruebas : IPuntoRepository
    {
        public Exception? ErrorAlGuardar { get; init; }
        public CampusData? CampusAlCargar { get; init; }

        public CampusData Cargar(string rutaArchivo) => CampusAlCargar ?? new CampusData();

        public Task<CampusData> CargarAsync(string rutaArchivo, CancellationToken cancellationToken = default) =>
            Task.FromResult(Cargar(rutaArchivo));

        public void Guardar(CampusData campus, string rutaArchivo)
        {
            if (ErrorAlGuardar is not null)
            {
                throw ErrorAlGuardar;
            }
        }

        public Task GuardarAsync(CampusData campus, string rutaArchivo, CancellationToken cancellationToken = default) =>
            ErrorAlGuardar is null ? Task.CompletedTask : Task.FromException(ErrorAlGuardar);
    }

    private sealed class VistaPruebas : IRutaView
    {
        private readonly TaskCompletionSource _mensajeRecibido = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _avisoConfiguracion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _campusConDensidades = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public event Action<double, double>? MapaPulsado;
        event Action? IRutaView.ImagenMapaSolicitada { add { } remove { } }
        public event Action? CargarCampusSolicitado;
        public event Action? GuardarCampusSolicitado;
        event Action? IRutaView.GenerarRutaSolicitada { add { } remove { } }
        event Action? IRutaView.ConfigurarOperacionSolicitada { add { } remove { } }
        public event Action? GenerarProgramacionSolicitada;
        event Action? IRutaView.RegistrarRecoleccionSolicitada { add { } remove { } }
        event Action? IRutaView.EditarPuntoSolicitado { add { } remove { } }
        event Action? IRutaView.EliminarPuntoSolicitado { add { } remove { } }
        event Action? IRutaView.AgregarConexionSolicitada { add { } remove { } }
        event Action? IRutaView.EditarConexionSolicitada { add { } remove { } }
        event Action? IRutaView.EliminarConexionSolicitada { add { } remove { } }

        public int? PuntoSeleccionadoId => null;
        public int CantidadPuntosSeleccionados => 1;
        public ConexionCampus? ConexionSeleccionada => null;
        public PuntoRecoleccion? PuntoSolicitado { get; init; }
        public bool FallarAlMostrarPuntos { get; set; }
        public bool Ocupada { get; private set; }
        public bool RespuestaConfigurarResiduos { get; set; }
        public bool AceptarDialogoConfiguracion { get; set; }
        public IReadOnlyList<ParametrosResiduo> ResiduosConfigurados { get; init; } = [];
        public IReadOnlyCollection<TipoResiduo>? TiposDensidadPendiente { get; private set; }
        public int SolicitudesConfiguracion { get; private set; }
        public string RutaCampus { get; init; } = Path.Combine(Path.GetTempPath(), "campus.txt");
        public CampusData CampusMostrado { get; private set; } = new();
        public string? UltimoMensaje { get; private set; }
        public string? MensajeConfiguracion { get; private set; }

        public bool SolicitarDatosPunto(double x, double y, PuntoRecoleccion? puntoOriginal, out PuntoRecoleccion? punto)
        {
            punto = PuntoSolicitado;
            return punto is not null;
        }

        public bool SolicitarDatosConexion(IReadOnlyList<PuntoRecoleccion> puntos, ConexionCampus? conexionOriginal, out ConexionCampus? conexion)
        {
            conexion = null;
            return false;
        }

        public bool SolicitarConfiguracion(
            ParametrosOperacion parametrosOperacion,
            IReadOnlyList<ParametrosResiduo> parametrosResiduos,
            out ParametrosOperacion? parametrosActualizados,
            out IReadOnlyList<ParametrosResiduo>? residuosActualizados,
            IReadOnlyCollection<TipoResiduo>? tiposDensidadPendiente)
        {
            SolicitudesConfiguracion++;
            TiposDensidadPendiente = tiposDensidadPendiente;
            parametrosActualizados = parametrosOperacion;
            residuosActualizados = ResiduosConfigurados;
            return AceptarDialogoConfiguracion;
        }

        public bool ConfirmarAccion(string mensaje) => true;
        public bool ConfirmarConfiguracionResiduos(string mensaje)
        {
            MensajeConfiguracion = mensaje;
            _avisoConfiguracion.TrySetResult();
            return RespuestaConfigurarResiduos;
        }

        public string? SeleccionarArchivoCampus(bool guardar) => RutaCampus;
        public string? SeleccionarImagenMapa() => null;

        public void MostrarCampus(CampusData campus)
        {
            if (FallarAlMostrarPuntos && campus.Puntos.Count > 0)
            {
                FallarAlMostrarPuntos = false;
                throw new InvalidOperationException("falló al mostrar el campus temporal");
            }

            CampusMostrado = campus;
            if (campus.ParametrosResiduos.Count > 0)
            {
                _campusConDensidades.TrySetResult();
            }
        }

        public void MostrarResultado(ResultadoRecoleccion resultado)
        {
        }

        public void EstablecerOcupado(bool ocupado, string mensaje) => Ocupada = ocupado;

        public void MostrarMensaje(string mensaje, bool error = false)
        {
            UltimoMensaje = mensaje;
            _mensajeRecibido.TrySetResult();
        }

        public Task EsperarMensaje() => _mensajeRecibido.Task.WaitAsync(TimeSpan.FromSeconds(5));
        public Task EsperarAvisoConfiguracion() => _avisoConfiguracion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        public Task EsperarCampusConDensidades() => _campusConDensidades.Task.WaitAsync(TimeSpan.FromSeconds(5));
        public void PulsarMapa(double x, double y) => MapaPulsado?.Invoke(x, y);
        public void SolicitarCargaCampus() => CargarCampusSolicitado?.Invoke();
        public void SolicitarProgramacion() => GenerarProgramacionSolicitada?.Invoke();
        public void SolicitarGuardado() => GuardarCampusSolicitado?.Invoke();
    }
}
