using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Views
{
    internal interface IRutaView
    {
        event Action<double, double>? MapaPulsado;
        event Action? ImagenMapaSolicitada;
        event Action? CargarCampusSolicitado;
        event Action? GuardarCampusSolicitado;
        event Action? GenerarRutaSolicitada;
        event Action? ConfigurarOperacionSolicitada;
        event Action? GenerarProgramacionSolicitada;
        event Action? RegistrarRecoleccionSolicitada;
        event Action? EditarPuntoSolicitado;
        event Action? EliminarPuntoSolicitado;
        event Action? AgregarConexionSolicitada;
        event Action? EditarConexionSolicitada;
        event Action? EliminarConexionSolicitada;

        int? PuntoSeleccionadoId { get; }
        int CantidadPuntosSeleccionados { get; }
        ConexionCampus? ConexionSeleccionada { get; }

        bool SolicitarDatosPunto(
            double x,
            double y,
            PuntoRecoleccion? puntoOriginal,
            out PuntoRecoleccion? punto);

        bool SolicitarDatosConexion(
            IReadOnlyList<PuntoRecoleccion> puntos,
            ConexionCampus? conexionOriginal,
            out ConexionCampus? conexion);

        bool SolicitarConfiguracion(
            ParametrosOperacion parametrosOperacion,
            IReadOnlyList<ParametrosResiduo> parametrosResiduos,
            out ParametrosOperacion? parametrosActualizados,
            out IReadOnlyList<ParametrosResiduo>? residuosActualizados,
            IReadOnlyCollection<TipoResiduo>? tiposDensidadPendiente);

        bool ConfirmarAccion(string mensaje);
        bool ConfirmarConfiguracionResiduos(string mensaje);

        string? SeleccionarArchivoCampus(bool guardar);
        string? SeleccionarImagenMapa();
        void MostrarCampus(CampusData campus);
        void MostrarResultado(ResultadoRecoleccion resultado);
        void EstablecerOcupado(bool ocupado, string mensaje);
        void MostrarMensaje(string mensaje, bool error = false);
    }
}
