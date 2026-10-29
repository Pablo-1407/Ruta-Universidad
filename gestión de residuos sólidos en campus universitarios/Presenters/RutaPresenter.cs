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
        private CampusData _campus = new();
        private string? _rutaArchivoActual;

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
            _vista.EditarPuntoSolicitado += EditarPunto;
            _vista.EliminarPuntoSolicitado += EliminarPunto;
            _vista.AgregarConexionSolicitada += AgregarConexion;
            _vista.EditarConexionSolicitada += EditarConexion;
            _vista.EliminarConexionSolicitada += EliminarConexion;
        }

        public void Inicializar() => _vista.MostrarCampus(_campus);

        private void GenerarRuta()
        {
            try
            {
                ResultadoRecoleccion resultado = _recoleccionService.GenerarRuta(
                    _campus,
                    _vista.CantidadPuntosSeleccionados);
                _vista.MostrarResultado(resultado);
            }
            catch (Exception ex)
            {
                _vista.MostrarMensaje(ex.Message, error: true);
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

        private void CargarCampus()
        {
            string? ruta = _vista.SeleccionarArchivoCampus(guardar: false);
            if (string.IsNullOrWhiteSpace(ruta))
            {
                return;
            }

            try
            {
                CampusData campus = _repositorio.Cargar(ruta);
                ValidarCampus(campus);
                _campus = campus;
                _rutaArchivoActual = Path.GetFullPath(ruta);
                ActualizarVista();
            }
            catch (Exception ex)
            {
                _vista.MostrarMensaje($"No se pudo cargar el campus: {ex.Message}", error: true);
            }
        }

        private void GuardarCampus()
        {
            string? ruta = _rutaArchivoActual ?? _vista.SeleccionarArchivoCampus(guardar: true);
            if (string.IsNullOrWhiteSpace(ruta))
            {
                return;
            }

            try
            {
                ValidarCampus(_campus);
                _repositorio.Guardar(_campus, ruta);
                _rutaArchivoActual = Path.GetFullPath(ruta);
                ActualizarVista();
                _vista.MostrarMensaje("El campus se guardó correctamente.");
            }
            catch (Exception ex)
            {
                _vista.MostrarMensaje($"No se pudo guardar el campus: {ex.Message}", error: true);
            }
        }

        private PuntoRecoleccion? ObtenerPuntoSeleccionado()
        {
            int? id = _vista.PuntoSeleccionadoId;
            return id is null ? null : _campus.Puntos.FirstOrDefault(p => p.Id == id.Value);
        }

        private void ActualizarVista() => _vista.MostrarCampus(_campus);

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
                || punto.DiasSinRecoleccion < 0 || !double.IsFinite(punto.PesoZona) || punto.PesoZona < 0)
            {
                throw new ArgumentException("La generación, los días y el peso de zona no pueden ser negativos.");
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
