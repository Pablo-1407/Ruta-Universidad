using System.Diagnostics;
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
        private readonly ManejadorErrores _manejadorErrores;
        private readonly ProgramacionService _programacionService = new();
        private readonly IndicadoresService _indicadoresService = new();
        private CampusData _campus = new();
        private string? _rutaArchivoActual;
        private int _operacionEnCurso;

        public RutaPresenter(
            IRutaView vista,
            IPuntoRepository repositorio,
            RecoleccionService recoleccionService,
            ManejadorErrores manejadorErrores)
        {
            _vista = vista;
            _repositorio = repositorio;
            _recoleccionService = recoleccionService;
            _manejadorErrores = manejadorErrores;

            _vista.MapaPulsado += (x, y) => EjecutarSeguro(() => CrearPunto(x, y), "crear punto");
            _vista.ImagenMapaSolicitada += () => EjecutarSeguro(CargarImagenMapa, "cargar imagen");
            _vista.CargarCampusSolicitado += () => EjecutarSeguroAsync(CargarCampus, "cargar campus");
            _vista.GuardarCampusSolicitado += () => EjecutarSeguroAsync(GuardarCampus, "guardar campus");
            _vista.GenerarRutaSolicitada += () => EjecutarSeguroAsync(GenerarRuta, "generar ruta");
            _vista.ConfigurarOperacionSolicitada += () => EjecutarSeguro(ConfigurarOperacion, "configurar operación");
            _vista.GenerarProgramacionSolicitada += () => EjecutarSeguroAsync(GenerarProgramacion, "generar programación");
            _vista.RegistrarRecoleccionSolicitada += () => EjecutarSeguroAsync(RegistrarRecoleccion, "registrar recolección");
            _vista.EditarPuntoSolicitado += () => EjecutarSeguro(EditarPunto, "editar punto");
            _vista.EliminarPuntoSolicitado += () => EjecutarSeguro(EliminarPunto, "eliminar punto");
            _vista.AgregarConexionSolicitada += () => EjecutarSeguro(AgregarConexion, "agregar conexión");
            _vista.EditarConexionSolicitada += () => EjecutarSeguro(EditarConexion, "editar conexión");
            _vista.EliminarConexionSolicitada += () => EjecutarSeguro(EliminarConexion, "eliminar conexión");
        }

        private void ConfigurarOperacion()
        {
            ConfigurarOperacion(null);
        }

        private void ConfigurarOperacion(IReadOnlyCollection<TipoResiduo>? tiposDensidadPendiente)
        {
            if (!_vista.SolicitarConfiguracion(
                    _campus.ParametrosOperacion,
                    _campus.ParametrosResiduos,
                    out ParametrosOperacion? parametrosOperacion,
                    out IReadOnlyList<ParametrosResiduo>? parametrosResiduos,
                    tiposDensidadPendiente)
                || parametrosOperacion is null
                || parametrosResiduos is null)
            {
                return;
            }

            CampusData campusTemporal = CopiarCampus(_campus);
            campusTemporal.ParametrosOperacion = parametrosOperacion;
            campusTemporal.ParametrosResiduos = parametrosResiduos.ToList();
            ConfirmarCampus(campusTemporal);
        }

        private async Task GenerarProgramacion()
        {
            if (!IniciarOperacion("Calculando programación…"))
            {
                return;
            }

            try
            {
                CampusData campusTemporal = CopiarCampus(_campus);
                IReadOnlyList<ProgramacionRecoleccion> generadas = await Task.Run(
                    () => _programacionService.GenerarProgramacion(campusTemporal, DateTimeOffset.Now));
                _programacionService.GuardarProgramacion(campusTemporal, generadas);
                ConfirmarCampus(campusTemporal);
                _vista.MostrarMensaje($"Se generaron {generadas.Count} recolecciones programadas.");
            }
            catch (DensidadesResiduosIncompletasException excepcion)
            {
                TipoResiduo[] tiposPendientes = excepcion.Problemas
                    .Select(problema => problema.Tipo)
                    .Distinct()
                    .ToArray();
                if (_vista.ConfirmarConfiguracionResiduos(CrearMensajeDensidadesPendientes(excepcion)))
                {
                    ConfigurarOperacion(tiposPendientes);
                }
            }
            finally
            {
                FinalizarOperacion();
            }
        }

        private static string CrearMensajeDensidadesPendientes(DensidadesResiduosIncompletasException excepcion)
        {
            string detalle = string.Join(
                Environment.NewLine,
                excepcion.Problemas.Select(problema =>
                    $"• {problema.Tipo}: {string.Join("; ", problema.InformacionPendiente)}."));
            return $"No se puede generar la programación porque faltan datos documentados de densidad:{Environment.NewLine}{detalle}{Environment.NewLine}{Environment.NewLine}¿Quieres abrir ahora la configuración de residuos?";
        }

        private async Task RegistrarRecoleccion()
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
                CampusData campusTemporal = CopiarCampus(_campus);
                ProgramacionRecoleccion programacionTemporal = campusTemporal.Programaciones
                    .Single(p => p.Id == programacion.Id);
                PuntoRecoleccion[] puntosTemporales = programacionTemporal.PuntosIds
                    .Select(id => campusTemporal.Puntos.Single(p => p.Id == id))
                    .ToArray();
                ResultadoRecoleccion resultado = await Task.Run(
                    () => _recoleccionService.GenerarRuta(campusTemporal, puntosTemporales.Select(p => p.Id).ToArray()));
                double volumen = _indicadoresService.EstimarVolumenActualLitros(puntosTemporales);
                RegistroHistorialRecoleccion registro = new()
                {
                    Id = campusTemporal.HistorialRecolecciones.Count == 0 ? 1 : campusTemporal.HistorialRecolecciones.Max(r => r.Id) + 1,
                    FechaCompletada = DateTimeOffset.Now,
                    CentroAcopioId = centros[0].Id,
                    PuntosIds = puntosTemporales.Select(p => p.Id).ToList(),
                    VolumenLitros = volumen,
                    KilosRecolectados = _indicadoresService.EstimarKilos(puntosTemporales),
                    DistanciaMetros = resultado.RutaOptimizada.DistanciaTotal,
                    DuracionMinutos = resultado.DuracionTotalMinutos,
                    Costo = resultado.CostoEstimado,
                    KilosAprovechados = resultado.KilosAprovechablesDisponibles ?? 0,
                    CO2eEvitadoKg = resultado.CO2eEvitadoKgDisponible ?? 0
                };
                registro.Validar();

                foreach (PuntoRecoleccion punto in puntosTemporales)
                {
                    punto.NivelLlenadoPct = 0;
                    punto.DiasSinRecoleccion = 0;
                }

                programacionTemporal.Estado = EstadoProgramacion.Completada;
                programacionTemporal.FechaCompletada = registro.FechaCompletada;
                campusTemporal.HistorialRecolecciones.Add(registro);
                ConfirmarCampus(campusTemporal);
                _vista.MostrarMensaje(_rutaArchivoActual is null
                    ? "La recolección se registró en el historial en memoria. Guarda el campus en TXT para conservarla."
                    : "La recolección se registró en el historial. Guarda el campus para conservar los cambios en TXT.");
            }
            finally
            {
                FinalizarOperacion();
            }
        }

        public void Inicializar() => _vista.MostrarCampus(_campus);

        private async Task GenerarRuta()
        {
            _campus.ParametrosOperacion.Validar();

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

            CampusData campusTemporal = CopiarCampus(_campus);
            punto.Id = campusTemporal.Puntos.Count == 0 ? 1 : campusTemporal.Puntos.Max(p => p.Id) + 1;
            punto.X = x;
            punto.Y = y;

            ValidarPunto(campusTemporal, punto, null);
            campusTemporal.Puntos.Add(punto);
            ConfirmarCampus(campusTemporal);
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

            CampusData campusTemporal = CopiarCampus(_campus);
            ValidarPunto(campusTemporal, actualizado, original.Id);
            int indice = campusTemporal.Puntos.FindIndex(p => p.Id == original.Id);
            campusTemporal.Puntos[indice] = actualizado;
            ConfirmarCampus(campusTemporal);
        }

        private void EliminarPunto()
        {
            PuntoRecoleccion? punto = ObtenerPuntoSeleccionado();
            if (punto is null)
            {
                _vista.MostrarMensaje("Selecciona un punto para eliminarlo.", error: true);
                return;
            }

            CampusData campusTemporal = CopiarCampus(_campus);
            campusTemporal.Puntos.RemoveAll(p => p.Id == punto.Id);
            campusTemporal.Conexiones.RemoveAll(c => c.OrigenId == punto.Id || c.DestinoId == punto.Id);
            ConfirmarCampus(campusTemporal);
        }

        private void AgregarConexion()
        {
            if (!_vista.SolicitarDatosConexion(_campus.Puntos, null, out ConexionCampus? conexion)
                || conexion is null)
            {
                return;
            }

            CampusData campusTemporal = CopiarCampus(_campus);
            ValidarConexion(campusTemporal, conexion, null);
            campusTemporal.Conexiones.Add(conexion);
            ConfirmarCampus(campusTemporal);
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

            CampusData campusTemporal = CopiarCampus(_campus);
            ConexionCampus originalTemporal = campusTemporal.Conexiones
                .Single(c => c.OrigenId == original.OrigenId && c.DestinoId == original.DestinoId);
            ValidarConexion(campusTemporal, actualizada, originalTemporal);
            int indice = campusTemporal.Conexiones.IndexOf(originalTemporal);
            campusTemporal.Conexiones[indice] = actualizada;
            ConfirmarCampus(campusTemporal);
        }

        private void EliminarConexion()
        {
            ConexionCampus? conexion = _vista.ConexionSeleccionada;
            if (conexion is null)
            {
                _vista.MostrarMensaje("Selecciona una conexión para eliminarla.", error: true);
                return;
            }

            CampusData campusTemporal = CopiarCampus(_campus);
            ConexionCampus conexionTemporal = campusTemporal.Conexiones
                .Single(c => c.OrigenId == conexion.OrigenId && c.DestinoId == conexion.DestinoId);
            campusTemporal.Conexiones.Remove(conexionTemporal);
            ConfirmarCampus(campusTemporal);
        }

        private void CargarImagenMapa()
        {
            string? ruta = _vista.SeleccionarImagenMapa();
            if (string.IsNullOrWhiteSpace(ruta))
            {
                return;
            }

            CampusData campusTemporal = CopiarCampus(_campus);
            campusTemporal.RutaImagenMapa = ruta;
            ConfirmarCampus(campusTemporal);
        }

        private async Task CargarCampus()
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
                ConfirmarCampus(campus);
                _rutaArchivoActual = Path.GetFullPath(ruta);
            }
            finally
            {
                FinalizarOperacion();
            }
        }

        private async Task GuardarCampus()
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
                CampusData campusTemporal = CopiarCampus(_campus);
                ValidarCampus(campusTemporal);
                string rutaCompleta = Path.GetFullPath(ruta);
                await _repositorio.GuardarAsync(campusTemporal, rutaCompleta);
                _campus = campusTemporal;
                _rutaArchivoActual = rutaCompleta;
                _vista.MostrarMensaje("El campus se guardó correctamente.");
            }
            finally
            {
                FinalizarOperacion();
            }
        }

        private void EjecutarSeguro(Action accion, string nombreOperacion)
        {
            try
            {
                accion();
            }
            catch (Exception excepcion)
            {
                _manejadorErrores.Manejar(excepcion, nombreOperacion);
            }
        }

        private async void EjecutarSeguroAsync(Func<Task> accion, string nombreOperacion)
        {
            try
            {
                await accion();
            }
            catch (Exception excepcion)
            {
                _manejadorErrores.Manejar(excepcion, nombreOperacion);
            }
        }

        private PuntoRecoleccion? ObtenerPuntoSeleccionado()
        {
            int? id = _vista.PuntoSeleccionadoId;
            return id is null ? null : _campus.Puntos.FirstOrDefault(p => p.Id == id.Value);
        }

        private void ConfirmarCampus(CampusData campusTemporal)
        {
            CampusData campusAnterior = _campus;
            try
            {
                _vista.MostrarCampus(campusTemporal);
                _campus = campusTemporal;
            }
            catch
            {
                try
                {
                    _vista.MostrarCampus(campusAnterior);
                }
                catch (Exception excepcionRestauracion)
                {
                    Trace.TraceError($"No se pudo restaurar la vista tras cancelar una operación: {excepcionRestauracion}");
                }

                throw;
            }
        }

        private static CampusData CopiarCampus(CampusData campus) => new()
        {
            Puntos = campus.Puntos.Select(p => new PuntoRecoleccion
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Tipo = p.Tipo,
                TipoResiduo = p.TipoResiduo,
                X = p.X,
                Y = p.Y,
                CapacidadLitros = p.CapacidadLitros,
                NivelLlenadoPct = p.NivelLlenadoPct,
                GeneracionKgDia = p.GeneracionKgDia,
                DiasSinRecoleccion = p.DiasSinRecoleccion,
                PesoZona = p.PesoZona
            }).ToList(),
            Conexiones = campus.Conexiones.Select(c => new ConexionCampus
            {
                OrigenId = c.OrigenId,
                DestinoId = c.DestinoId,
                Metros = c.Metros
            }).ToList(),
            ParametrosOperacion = new ParametrosOperacion
            {
                CapacidadVehiculoLitros = campus.ParametrosOperacion.CapacidadVehiculoLitros,
                VelocidadMediaKmHora = campus.ParametrosOperacion.VelocidadMediaKmHora,
                CostoPorKm = campus.ParametrosOperacion.CostoPorKm,
                CostoPorHora = campus.ParametrosOperacion.CostoPorHora,
                MinutosServicioPorContenedor = campus.ParametrosOperacion.MinutosServicioPorContenedor,
                MinutosDescarga = campus.ParametrosOperacion.MinutosDescarga,
                UmbralProgramacionPct = campus.ParametrosOperacion.UmbralProgramacionPct
            },
            ParametrosResiduos = campus.ParametrosResiduos.Select(p => new ParametrosResiduo
            {
                Tipo = p.Tipo,
                DensidadKgPorLitro = p.DensidadKgPorLitro,
                PorcentajeAprovechablePct = p.PorcentajeAprovechablePct,
                FactorCO2eEvitadoKgPorKg = p.FactorCO2eEvitadoKgPorKg,
                FuenteDensidad = p.FuenteDensidad,
                FuenteAprovechamiento = p.FuenteAprovechamiento,
                FuenteEmisiones = p.FuenteEmisiones
            }).ToList(),
            Programaciones = campus.Programaciones.Select(p => new ProgramacionRecoleccion
            {
                Id = p.Id,
                FechaProgramada = p.FechaProgramada,
                CentroAcopioId = p.CentroAcopioId,
                PuntosIds = p.PuntosIds.ToList(),
                VolumenEstimadoLitros = p.VolumenEstimadoLitros,
                Estado = p.Estado,
                FechaCompletada = p.FechaCompletada
            }).ToList(),
            HistorialRecolecciones = campus.HistorialRecolecciones.Select(r => new RegistroHistorialRecoleccion
            {
                Id = r.Id,
                FechaCompletada = r.FechaCompletada,
                CentroAcopioId = r.CentroAcopioId,
                PuntosIds = r.PuntosIds.ToList(),
                VolumenLitros = r.VolumenLitros,
                KilosRecolectados = r.KilosRecolectados,
                DistanciaMetros = r.DistanciaMetros,
                DuracionMinutos = r.DuracionMinutos,
                Costo = r.Costo,
                KilosAprovechados = r.KilosAprovechados,
                CO2eEvitadoKg = r.CO2eEvitadoKg
            }).ToList(),
            RutaImagenMapa = campus.RutaImagenMapa
        };

        private bool IniciarOperacion(string mensaje)
        {
            if (Interlocked.CompareExchange(ref _operacionEnCurso, 1, 0) != 0)
            {
                return false;
            }

            try
            {
                _vista.EstablecerOcupado(true, mensaje);
                return true;
            }
            catch
            {
                Interlocked.Exchange(ref _operacionEnCurso, 0);
                throw;
            }
        }

        private void FinalizarOperacion()
        {
            try
            {
                _vista.EstablecerOcupado(false, "Operación finalizada.");
            }
            finally
            {
                Interlocked.Exchange(ref _operacionEnCurso, 0);
            }
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

        private static void ValidarPunto(CampusData campus, PuntoRecoleccion punto, int? idIgnorado)
        {
            ValidarDatosPunto(punto);

            if (campus.Puntos.Any(p => p.Id != idIgnorado
                && string.Equals(p.Nombre, punto.Nombre, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Ya existe un punto con ese nombre.");
            }

            if (punto.Tipo == TipoPunto.CentroAcopio
                && campus.Puntos.Any(p => p.Id != idIgnorado && p.Tipo == TipoPunto.CentroAcopio))
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

        private static void ValidarConexion(CampusData campus, ConexionCampus conexion, ConexionCampus? ignorada)
        {
            if (conexion.OrigenId == conexion.DestinoId)
            {
                throw new ArgumentException("Una conexión debe unir dos puntos diferentes.");
            }

            if (!campus.Puntos.Any(p => p.Id == conexion.OrigenId)
                || !campus.Puntos.Any(p => p.Id == conexion.DestinoId))
            {
                throw new ArgumentException("Selecciona dos puntos existentes para crear la conexión.");
            }

            if (!double.IsFinite(conexion.Metros) || conexion.Metros <= 0)
            {
                throw new ArgumentException("La distancia debe ser mayor que cero.");
            }

            bool duplicada = campus.Conexiones.Any(c => !ReferenceEquals(c, ignorada)
                && ((c.OrigenId == conexion.OrigenId && c.DestinoId == conexion.DestinoId)
                    || (c.OrigenId == conexion.DestinoId && c.DestinoId == conexion.OrigenId)));
            if (duplicada)
            {
                throw new ArgumentException("Ya existe una conexión entre esos puntos.");
            }
        }
    }
}
