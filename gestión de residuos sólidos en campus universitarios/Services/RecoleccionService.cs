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

            campus.ParametrosOperacion.Validar();

            foreach (PuntoRecoleccion punto in puntosSeleccionados)
            {
                punto.ValidarDatosOperativos();
            }

            Ruta rutaOptimizadaSinCapacidad = _estrategiaOptimizada.Calcular(
                grafo,
                campus.Puntos,
                centros[0],
                puntosSeleccionados);
            Ruta rutaBaseSinCapacidad = _estrategiaBase.Calcular(
                grafo,
                campus.Puntos,
                centros[0],
                puntosSeleccionados);
            Dijkstra dijkstra = new();
            Ruta rutaOptimizada = ConstructorRuta.CrearConCapacidad(
                grafo,
                campus.Puntos,
                centros[0],
                rutaOptimizadaSinCapacidad.PuntosVisitados,
                campus.ParametrosOperacion.CapacidadVehiculoLitros,
                dijkstra);
            Ruta rutaBase = ConstructorRuta.CrearConCapacidad(
                grafo,
                campus.Puntos,
                centros[0],
                rutaBaseSinCapacidad.PuntosVisitados,
                campus.ParametrosOperacion.CapacidadVehiculoLitros,
                dijkstra);
            double kilosEstimados = _indicadores.EstimarKilos(puntosSeleccionados);
            double volumenEstimado = _indicadores.EstimarVolumenActualLitros(puntosSeleccionados);
            double duracionDesplazamiento = _indicadores.CalcularDuracionDesplazamientoMinutos(
                rutaOptimizada.DistanciaTotal,
                campus.ParametrosOperacion.VelocidadMediaKmHora);
            double duracionServicio = puntosSeleccionados.Length * campus.ParametrosOperacion.MinutosServicioPorContenedor
                + Math.Max(0, rutaOptimizada.PuntosRecorrido.Count(p => p.Id == centros[0].Id) - 2)
                * campus.ParametrosOperacion.MinutosDescarga;
            double duracionTotal = duracionDesplazamiento + duracionServicio;

            return new ResultadoRecoleccion
            {
                PuntosPrioritarios = priorizados,
                RutaOptimizada = rutaOptimizada,
                RutaBase = rutaBase,
                AhorroPorcentual = _indicadores.CalcularAhorroPorcentual(rutaBase, rutaOptimizada),
                KilosEstimados = kilosEstimados,
                VolumenEstimadoLitros = volumenEstimado,
                VolumenGeneradoEstimadoLitros = _indicadores.EstimarVolumenGeneradoLitros(puntosSeleccionados, campus.ParametrosResiduos),
                UtilizacionVehiculoPct = _indicadores.CalcularUtilizacionVehiculoPct(
                    volumenEstimado,
                    campus.ParametrosOperacion.CapacidadVehiculoLitros,
                    Math.Max(1, rutaOptimizada.PuntosRecorrido.Count(p => p.Id == centros[0].Id))),
                DuracionDesplazamientoMinutos = duracionDesplazamiento,
                DuracionServicioMinutos = duracionServicio,
                DuracionTotalMinutos = duracionTotal,
                CostoEstimado = _indicadores.CalcularCosto(rutaOptimizada.DistanciaTotal, duracionTotal, campus.ParametrosOperacion),
                KilosAprovechables = _indicadores.EstimarKilosAprovechables(puntosSeleccionados, campus.ParametrosResiduos),
                CO2eEvitadoKg = _indicadores.EstimarCO2eEvitadoKg(puntosSeleccionados, campus.ParametrosResiduos),
                DiasHastaUmbralEstimados = _indicadores.EstimarDiasHastaUmbral(
                    campus.Puntos.Where(p => p.Tipo == TipoPunto.Recoleccion),
                    campus.ParametrosResiduos,
                    campus.ParametrosOperacion.UmbralProgramacionPct)
            };
        }
    }
}
