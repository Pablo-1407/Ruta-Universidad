using gestión_de_residuos_sólidos_en_campus_universitarios.Algorithms.Priorizacion;
using gestión_de_residuos_sólidos_en_campus_universitarios.Algorithms.Rutas;
using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Services
{
    internal sealed class RecoleccionService
    {
        private readonly IEstrategiaPriorizacion _estrategiaPriorizacion;
        private readonly IEstrategiaRuta _estrategiaOptimizada;
        private readonly IEstrategiaRuta _estrategiaBase;
        private readonly IndicadoresService _indicadores;

        public RecoleccionService(
            IEstrategiaPriorizacion estrategiaPriorizacion,
            IEstrategiaRuta estrategiaOptimizada,
            IEstrategiaRuta estrategiaBase,
            IndicadoresService indicadores)
        {
            _estrategiaPriorizacion = estrategiaPriorizacion;
            _estrategiaOptimizada = estrategiaOptimizada;
            _estrategiaBase = estrategiaBase;
            _indicadores = indicadores;
        }

        public ResultadoRecoleccion GenerarRuta(CampusData campus, int cantidadPuntos)
        {
            ArgumentNullException.ThrowIfNull(campus);

            PuntoRecoleccion[] centros = campus.Puntos
                .Where(p => p.Tipo == TipoPunto.CentroAcopio)
                .ToArray();
            if (centros.Length != 1)
            {
                throw new InvalidOperationException("Registra exactamente un centro de acopio antes de generar la ruta.");
            }

            IReadOnlyList<PuntoPriorizado> priorizados = _estrategiaPriorizacion.Priorizar(campus.Puntos);
            if (priorizados.Count == 0)
            {
                throw new InvalidOperationException("Registra al menos un punto de recolección antes de generar la ruta.");
            }

            if (cantidadPuntos < 1 || cantidadPuntos > priorizados.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(cantidadPuntos), "La cantidad debe estar entre 1 y el total de puntos de recolección.");
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

            PuntoRecoleccion[] puntosSeleccionados = priorizados
                .Take(cantidadPuntos)
                .Select(p => p.Punto)
                .ToArray();

            Ruta rutaOptimizada = _estrategiaOptimizada.Calcular(
                grafo,
                campus.Puntos,
                centros[0],
                puntosSeleccionados);
            Ruta rutaBase = _estrategiaBase.Calcular(
                grafo,
                campus.Puntos,
                centros[0],
                puntosSeleccionados);

            return new ResultadoRecoleccion
            {
                PuntosPrioritarios = priorizados,
                RutaOptimizada = rutaOptimizada,
                RutaBase = rutaBase,
                AhorroPorcentual = _indicadores.CalcularAhorroPorcentual(rutaBase, rutaOptimizada),
                KilosEstimados = _indicadores.EstimarKilos(puntosSeleccionados)
            };
        }
    }
}
