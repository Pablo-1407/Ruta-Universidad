using gestión_de_residuos_sólidos_en_campus_universitarios.Data;
using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;
using gestión_de_residuos_sólidos_en_campus_universitarios.Services;
using gestión_de_residuos_sólidos_en_campus_universitarios.Views;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Presenters
{
    internal sealed class RutaPresenter
    {
        private readonly IRutaView _vista;
        private readonly IPuntoRepository _repositorio;
        private readonly RecoleccionService _recoleccionService;
        private readonly ProgramacionService _programacionService = new();
        private readonly IndicadoresService _indicadoresService = new();
        private CampusData _campus = new();
        private string? _rutaArchivoActual;
        private int _operacionEnCurso;

        public RutaPresenter(IRutaView vista, IPuntoRepository repositorio, RecoleccionService recoleccionService)
        {
            _vista = vista;
            _repositorio = repositorio;
            _recoleccionService = recoleccionService;

            _vista.MapaPulsado += CrearPunto;
            _vista.ImagenMapaSolicitada += CargarImagenMapa;
            _vista.CargarCampusSolicitado += CargarCampus;
            _vista.GuardarCampusSolicitado += GuardarCampus;
            _vista.GenerarRutaSolicitada += GenerarRuta;
            _vista.ConfigurarOperacionSolicitada += ConfigurarOperacion;
            _vista.GenerarProgramacionSolicitada += GenerarProgramacion;
            _vista.RegistrarRecoleccionSolicitada += RegistrarRecoleccion;
            _vista.EditarPuntoSolicitado += EditarPunto;
            _vista.EliminarPuntoSolicitado += EliminarPunto;
            _vista.AgregarConexionSolicitada += AgregarConexion;
            _vista.EditarConexionSolicitada += EditarConexion;
            _vista.EliminarConexionSolicitada += EliminarConexion;
        }

        private void ConfigurarOperacion()
        {
            if (!_vista.SolicitarConfiguracion(
                    _campus.ParametrosOperacion,
                    _campus.ParametrosResiduos,
                    out ParametrosOperacion? parametrosOperacion,
                    out IReadOnlyList<ParametrosResiduo>? parametrosResiduos)
                || parametrosOperacion is null
                || parametrosResiduos is null)
            {
                return;
            }

            _campus.ParametrosOperacion = parametrosOperacion;
            _campus.ParametrosResiduos = parametrosResiduos.ToList();
            ActualizarVista();
        }

        private async void GenerarProgramacion()
        {
            if (!IniciarOperacion("Calculando programación…"))
            {
                return;
            }

            try
            {
                IReadOnlyList<ProgramacionRecoleccion> generadas = await Task.Run(
                    () => _programacionService.GenerarProgramacion(_campus, DateTimeOffset.Now));
                _programacionService.GuardarProgramacion(_campus, generadas);
                ActualizarVista();
                _vista.MostrarMensaje($"Se generaron {generadas.Count} recolecciones programadas.");
            }
            catch (Exception ex)
            {
                _vista.MostrarMensaje(ex.Message, error: true);
            }
            finally
            {
                FinalizarOperacion();
            }
        }

        private async void RegistrarRecoleccion()
        {
            ProgramacionRecoleccion? programacion = _campus.Programaciones
                .Where(p => p.Estado == EstadoProgramacion.Programada)
                .OrderBy(p => p.FechaProgramada)
                .FirstOrDefault();
            PuntoRecoleccion[] centros = _campus.Puntos.Where(p => p.Tipo == TipoPunto.CentroAcopio).ToArray();
            if (programacion is null || centros.Length != 1)
            {
                _vista.MostrarMensaje("Genera una programación y registra exactamente un centro de acopio antes de completar una recolección.", error: true);
                return;
            }

            PuntoRecoleccion[] puntos = programacion.PuntosIds
                .Select(id => _campus.Puntos.FirstOrDefault(p => p.Id == id))
                .Where(p => p is not null)
                .Cast<PuntoRecoleccion>()
                .ToArray();
            if (puntos.Length != programacion.PuntosIds.Count)
            {
                _vista.MostrarMensaje("La programación contiene puntos que ya no están en el campus.", error: true);
                return;
            }

            if (programacion.CentroAcopioId != centros[0].Id)
            {
                _vista.MostrarMensaje("El centro de acopio de la programación ya no coincide con el centro actual.", error: true);
                return;
            }

            if (!_vista.ConfirmarAccion($"¿Registrar como completada la recolección de {puntos.Length} punto(s)?"))
            {
                return;
            }

            if (!IniciarOperacion("Calculando y registrando recolección…"))
            {
                return;
            }

            try
            {
                ResultadoRecoleccion resultado = await Task.Run(
                    () => _recoleccionService.GenerarRuta(_campus, puntos.Select(p => p.Id).ToArray()));
                double volumen = _indicadoresService.EstimarVolumenActualLitros(puntos);
                RegistroHistorialRecoleccion registro = new()
                {
                    Id = _campus.HistorialRecolecciones.Count == 0 ? 1 : _campus.HistorialRecolecciones.Max(r => r.Id) + 1,
                    FechaCompletada = DateTimeOffset.Now,
                    CentroAcopioId = centros[0].Id,
                    PuntosIds = puntos.Select(p => p.Id).ToList(),
                    VolumenLitros = volumen,
                    KilosRecolectados = _indicadoresService.EstimarKilos(puntos),
                    DistanciaMetros = resultado.RutaOptimizada.DistanciaTotal,
                    DuracionMinutos = resultado.DuracionTotalMinutos,
                    Costo = resultado.CostoEstimado,
                    KilosAprovechados = resultado.KilosAprovechablesDisponibles ?? 0,
                    CO2eEvitadoKg = resultado.CO2eEvitadoKgDisponible ?? 0
                };
                registro.Validar();

                foreach (PuntoRecoleccion punto in puntos)
                {
                    punto.NivelLlenadoPct = 0;
                    punto.DiasSinRecoleccion = 0;
                }

                programacion.Estado = EstadoProgramacion.Completada;
                programacion.FechaCompletada = registro.FechaCompletada;
                _campus.HistorialRecolecciones.Add(registro);
                ActualizarVista();
                _vista.MostrarMensaje(_rutaArchivoActual is null
                    ? "La recolección se registró en el historial en memoria. Guarda el campus en TXT para conservarla."
                    : "La recolección se registró en el historial. Guarda el campus para conservar los cambios en TXT.");
            }
            catch (Exception ex)
            {
                _vista.MostrarMensaje(ex.Message, error: true);
            }
            finally
            {
                FinalizarOperacion();
            }
        }

        public void Inicializar() => _vista.MostrarCampus(_campus);

        private async void GenerarRuta()
        {
            if (!IniciarOperacion("Calculando rutas e indicadores…"))
            {
                return;
            }

            try
            {
                CampusData campus = _campus;
                int cantidadPuntos = _vista.CantidadPuntosSeleccionados;
                ResultadoRecoleccion resultado = await Task.Run(
                    () => _recoleccionService.GenerarRuta(campus, cantidadPuntos));
                _vista.MostrarResultado(resultado);
            }
            catch (Exception ex)
            {
                _vista.MostrarMensaje(ex.Message, error: true);
            }
            finally
            {
                FinalizarOperacion();
            }
        }

        private void CrearPunto(double x, double y)
        {
            if (!_vista.SolicitarDatosPunto(x, y, null, out PuntoRecoleccion? punto) || punto is null)
            {
                return;
            }

            punto.Id = _campus.Puntos.Count == 0 ? 1 : _campus.Puntos.Max(p => p.Id) + 1;
            punto.X = x;
            punto.Y = y;

            try
            {
                ValidarPunto(punto, null);
                _campus.Puntos.Add(punto);
                ActualizarVista();
            }
            catch (Exception ex)
            {
                _vista.MostrarMensaje(ex.Message, error: true);
            }
        }

        private void EditarPunto()
        {
            PuntoRecoleccion? original = ObtenerPuntoSeleccionado();
            if (original is null)
            {
                _vista.MostrarMensaje("Selecciona un punto para editarlo.", error: true);
                return;
            }

            if (!_vista.SolicitarDatosPunto(original.X, original.Y, original, out PuntoRecoleccion? actualizado)
                || actualizado is null)
            {
                return;
            }

            actualizado.Id = original.Id;
            actualizado.X = original.X;
            actualizado.Y = original.Y;

            try
            {
                ValidarPunto(actualizado, original.Id);
                int indice = _campus.Puntos.IndexOf(original);
                _campus.Puntos[indice] = actualizado;
                ActualizarVista();
            }
            catch (Exception ex)
            {
                _vista.MostrarMensaje(ex.Message, error: true);
            }
        }

        private void EliminarPunto()
        {
            PuntoRecoleccion? punto = ObtenerPuntoSeleccionado();
            if (punto is null)
            {
                _vista.MostrarMensaje("Selecciona un punto para eliminarlo.", error: true);
                return;
            }

            _campus.Puntos.Remove(punto);
            _campus.Conexiones.RemoveAll(c => c.OrigenId == punto.Id || c.DestinoId == punto.Id);
            ActualizarVista();
        }

        private void AgregarConexion()
        {
            if (!_vista.SolicitarDatosConexion(_campus.Puntos, null, out ConexionCampus? conexion)
                || conexion is null)
            {
                return;
            }

            try
            {
                ValidarConexion(conexion, null);
                _campus.Conexiones.Add(conexion);
                ActualizarVista();
            }
            catch (Exception ex)
            {
                _vista.MostrarMensaje(ex.Message, error: true);
            }
        }

        private void EditarConexion()
        {
            ConexionCampus? original = _vista.ConexionSeleccionada;
            if (original is null)
            {
                _vista.MostrarMensaje("Selecciona una conexión para editarla.", error: true);
                return;
            }

            if (!_vista.SolicitarDatosConexion(_campus.Puntos, original, out ConexionCampus? actualizada)
                || actualizada is null)
            {
                return;
            }

            try
            {
                ValidarConexion(actualizada, original);
                int indice = _campus.Conexiones.IndexOf(original);
                _campus.Conexiones[indice] = actualizada;
                ActualizarVista();
            }
            catch (Exception ex)
            {
                _vista.MostrarMensaje(ex.Message, error: true);
            }
        }

        private void EliminarConexion()
        {
            ConexionCampus? conexion = _vista.ConexionSeleccionada;
            if (conexion is null)
            {
                _vista.MostrarMensaje("Selecciona una conexión para eliminarla.", error: true);
                return;
            }

            _campus.Conexiones.Remove(conexion);
            ActualizarVista();
        }

        private void CargarImagenMapa()
        {
            string? ruta = _vista.SeleccionarImagenMapa();
            if (string.IsNullOrWhiteSpace(ruta))
            {
                return;
            }

            _campus.RutaImagenMapa = ruta;
            ActualizarVista();
        }

        private async void CargarCampus()
        {
            string? ruta = _vista.SeleccionarArchivoCampus(guardar: false);
            if (string.IsNullOrWhiteSpace(ruta))
            {
                return;
            }

            if (!IniciarOperacion("Cargando campus desde TXT…"))
            {
                return;
            }

            try
            {
                CampusData campus = await _repositorio.CargarAsync(ruta);
                ValidarCampus(campus);
                _campus = campus;
                _rutaArchivoActual = Path.GetFullPath(ruta);
                ActualizarVista();
            }
            catch (Exception ex)
            {
                _vista.MostrarMensaje($"No se pudo cargar el campus: {ex.Message}", error: true);
            }
            finally
            {
                FinalizarOperacion();
            }
        }

        private async void GuardarCampus()
        {
            string? ruta = _rutaArchivoActual ?? _vista.SeleccionarArchivoCampus(guardar: true);
            if (string.IsNullOrWhiteSpace(ruta))
            {
                return;
            }

            if (!IniciarOperacion("Guardando campus en TXT…"))
            {
                return;
            }

            try
            {
                ValidarCampus(_campus);
                await _repositorio.GuardarAsync(_campus, ruta);
                _rutaArchivoActual = Path.GetFullPath(ruta);
                ActualizarVista();
                _vista.MostrarMensaje("El campus se guardó correctamente.");
            }
            catch (Exception ex)
            {
                _vista.MostrarMensaje($"No se pudo guardar el campus: {ex.Message}", error: true);
            }
            finally
            {
                FinalizarOperacion();
            }
        }

        private PuntoRecoleccion? ObtenerPuntoSeleccionado()
        {
            int? id = _vista.PuntoSeleccionadoId;
            return id is null ? null : _campus.Puntos.FirstOrDefault(p => p.Id == id.Value);
        }

        private void ActualizarVista() => _vista.MostrarCampus(_campus);

        private bool IniciarOperacion(string mensaje)
        {
            if (Interlocked.CompareExchange(ref _operacionEnCurso, 1, 0) != 0)
            {
                return false;
            }

            _vista.EstablecerOcupado(true, mensaje);
            return true;
        }

        private void FinalizarOperacion()
        {
            _vista.EstablecerOcupado(false, "Operación finalizada.");
            Interlocked.Exchange(ref _operacionEnCurso, 0);
        }

        private void ValidarCampus(CampusData campus)
        {
            HashSet<int> ids = [];
            HashSet<string> nombres = new(StringComparer.OrdinalIgnoreCase);
            int centrosAcopio = 0;
            foreach (PuntoRecoleccion punto in campus.Puntos)
            {
                ValidarDatosPunto(punto);
                if (punto.Tipo == TipoPunto.CentroAcopio)
                {
                    centrosAcopio++;
                }
                if (!ids.Add(punto.Id))
                {
                    throw new InvalidDataException("Hay puntos con identificadores duplicados.");
                }

                if (!nombres.Add(punto.Nombre.Trim()))
                {
                    throw new InvalidDataException("Hay puntos con nombres duplicados.");
                }
            }

            if (centrosAcopio > 1)
            {
                throw new InvalidDataException("El campus solo puede tener un centro de acopio.");
            }

            GrafoCampus grafo = new();
            foreach (PuntoRecoleccion punto in campus.Puntos)
            {
                grafo.AgregarNodo(punto.Id);
            }

            foreach (ConexionCampus conexion in campus.Conexiones)
            {
                grafo.AgregarArista(conexion.OrigenId, conexion.DestinoId, conexion.Metros);
            }
        }

        private void ValidarPunto(PuntoRecoleccion punto, int? idIgnorado)
        {
            ValidarDatosPunto(punto);

            if (_campus.Puntos.Any(p => p.Id != idIgnorado
                && string.Equals(p.Nombre, punto.Nombre, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Ya existe un punto con ese nombre.");
            }

            if (punto.Tipo == TipoPunto.CentroAcopio
                && _campus.Puntos.Any(p => p.Id != idIgnorado && p.Tipo == TipoPunto.CentroAcopio))
            {
                throw new ArgumentException("El campus ya tiene un centro de acopio registrado.");
            }
        }

        private static void ValidarDatosPunto(PuntoRecoleccion punto)
        {
            if (!Enum.IsDefined(punto.Tipo))
            {
                throw new ArgumentException("El tipo de punto no es válido.");
            }

            if (string.IsNullOrWhiteSpace(punto.Nombre))
            {
                throw new ArgumentException("El nombre del punto no puede estar vacío.");
            }

            if (punto.Id < 0 || !double.IsFinite(punto.X) || !double.IsFinite(punto.Y)
                || punto.X is < 0 or > 1 || punto.Y is < 0 or > 1)
            {
                throw new ArgumentException("La ubicación del punto no es válida.");
            }

            if (!double.IsFinite(punto.NivelLlenadoPct) || punto.NivelLlenadoPct is < 0 or > 100)
            {
                throw new ArgumentException("El nivel de llenado debe estar entre 0 y 100.");
            }

            if (!double.IsFinite(punto.GeneracionKgDia) || punto.GeneracionKgDia < 0
                || punto.DiasSinRecoleccion < 0 || !double.IsFinite(punto.PesoZona) || punto.PesoZona < 0
                || !double.IsFinite(punto.CapacidadLitros) || punto.CapacidadLitros < 0
                || !Enum.IsDefined(punto.TipoResiduo))
            {
                throw new ArgumentException("Los datos operativos del punto no son válidos.");
            }
        }

        private void ValidarConexion(ConexionCampus conexion, ConexionCampus? ignorada)
        {
            if (conexion.OrigenId == conexion.DestinoId)
            {
                throw new ArgumentException("Una conexión debe unir dos puntos diferentes.");
            }

            if (!_campus.Puntos.Any(p => p.Id == conexion.OrigenId)
                || !_campus.Puntos.Any(p => p.Id == conexion.DestinoId))
            {
                throw new ArgumentException("Selecciona dos puntos existentes para crear la conexión.");
            }

            if (!double.IsFinite(conexion.Metros) || conexion.Metros <= 0)
            {
                throw new ArgumentException("La distancia debe ser mayor que cero.");
            }

            bool duplicada = _campus.Conexiones.Any(c => !ReferenceEquals(c, ignorada)
                && ((c.OrigenId == conexion.OrigenId && c.DestinoId == conexion.DestinoId)
                    || (c.OrigenId == conexion.DestinoId && c.DestinoId == conexion.OrigenId)));
            if (duplicada)
            {
                throw new ArgumentException("Ya existe una conexión entre esos puntos.");
            }
        }
    }
}
