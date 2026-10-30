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

            return GenerarRuta(campus, priorizados, centros[0], priorizados.Take(cantidadPuntos).Select(p => p.Punto).ToArray());
        }

        public ResultadoRecoleccion GenerarRuta(CampusData campus, IReadOnlyCollection<int> puntosIds)
        {
            ArgumentNullException.ThrowIfNull(campus);
            ArgumentNullException.ThrowIfNull(puntosIds);
            if (puntosIds.Count == 0 || puntosIds.Distinct().Count() != puntosIds.Count)
            {
                throw new ArgumentException("Selecciona identificadores de punto válidos y no duplicados.", nameof(puntosIds));
            }

            PuntoRecoleccion[] centros = campus.Puntos.Where(p => p.Tipo == TipoPunto.CentroAcopio).ToArray();
            if (centros.Length != 1)
            {
                throw new InvalidOperationException("Registra exactamente un centro de acopio antes de generar la ruta.");
            }

            IReadOnlyList<PuntoPriorizado> priorizados = _estrategiaPriorizacion.Priorizar(campus.Puntos);
            Dictionary<int, PuntoRecoleccion> puntosPorId = campus.Puntos
                .Where(p => p.Tipo == TipoPunto.Recoleccion)
                .ToDictionary(p => p.Id);
            if (puntosIds.Any(id => !puntosPorId.ContainsKey(id)))
            {
                throw new ArgumentException("La selección contiene puntos inexistentes o no recolectables.", nameof(puntosIds));
            }

            PuntoRecoleccion[] seleccionados = priorizados
                .Where(p => puntosIds.Contains(p.Punto.Id))
                .Select(p => p.Punto)
                .ToArray();
            return GenerarRuta(campus, priorizados, centros[0], seleccionados);
        }

        private ResultadoRecoleccion GenerarRuta(
            CampusData campus,
            IReadOnlyList<PuntoPriorizado> priorizados,
            PuntoRecoleccion centro,
            PuntoRecoleccion[] puntosSeleccionados)
        {
            GrafoCampus grafo = new();
            foreach (PuntoRecoleccion punto in campus.Puntos)
            {
                grafo.AgregarNodo(punto.Id);
            }

            foreach (ConexionCampus conexion in campus.Conexiones)
            {
                grafo.AgregarArista(conexion.OrigenId, conexion.DestinoId, conexion.Metros);
            }

            campus.ParametrosOperacion.Validar();

            foreach (PuntoRecoleccion punto in puntosSeleccionados)
            {
                punto.ValidarDatosOperativos();
            }

            Ruta rutaOptimizadaSinCapacidad = _estrategiaOptimizada.Calcular(
                grafo,
                campus.Puntos,
                centro,
                puntosSeleccionados);
            Ruta rutaBaseSinCapacidad = _estrategiaBase.Calcular(
                grafo,
                campus.Puntos,
                centro,
                puntosSeleccionados);
            Dijkstra dijkstra = new();
            Ruta rutaOptimizada = ConstructorRuta.CrearConCapacidad(
                grafo,
                campus.Puntos,
                centro,
                rutaOptimizadaSinCapacidad.PuntosVisitados,
                campus.ParametrosOperacion.CapacidadVehiculoLitros,
                dijkstra);
            Ruta rutaBase = ConstructorRuta.CrearConCapacidad(
                grafo,
                campus.Puntos,
                centro,
                rutaBaseSinCapacidad.PuntosVisitados,
                campus.ParametrosOperacion.CapacidadVehiculoLitros,
                dijkstra);
            double kilosEstimados = _indicadores.EstimarKilos(puntosSeleccionados);
            double volumenEstimado = _indicadores.EstimarVolumenActualLitros(puntosSeleccionados);
            double duracionDesplazamiento = _indicadores.CalcularDuracionDesplazamientoMinutos(
                rutaOptimizada.DistanciaTotal,
                campus.ParametrosOperacion.VelocidadMediaKmHora);
            double duracionServicio = puntosSeleccionados.Length * campus.ParametrosOperacion.MinutosServicioPorContenedor
                + Math.Max(0, rutaOptimizada.PuntosRecorrido.Count(p => p.Id == centro.Id) - 2)
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
                VolumenGeneradoEstimadoLitrosDisponible = IntentarCalcular(
                    () => _indicadores.EstimarVolumenGeneradoLitros(puntosSeleccionados, campus.ParametrosResiduos)),
                UtilizacionVehiculoPct = _indicadores.CalcularUtilizacionVehiculoPct(
                    volumenEstimado,
                    campus.ParametrosOperacion.CapacidadVehiculoLitros,
                    Math.Max(1, rutaOptimizada.PuntosRecorrido.Count(p => p.Id == centro.Id))),
                DuracionDesplazamientoMinutos = duracionDesplazamiento,
                DuracionServicioMinutos = duracionServicio,
                DuracionTotalMinutos = duracionTotal,
                CostoEstimado = _indicadores.CalcularCosto(rutaOptimizada.DistanciaTotal, duracionTotal, campus.ParametrosOperacion),
                KilosAprovechablesDisponibles = IntentarCalcular(
                    () => _indicadores.EstimarKilosAprovechables(puntosSeleccionados, campus.ParametrosResiduos)),
                CO2eEvitadoKgDisponible = IntentarCalcular(
                    () => _indicadores.EstimarCO2eEvitadoKg(puntosSeleccionados, campus.ParametrosResiduos)),
                DiasHastaUmbralEstimados = _indicadores.EstimarDiasHastaUmbral(
                    campus.Puntos.Where(p => p.Tipo == TipoPunto.Recoleccion),
                    campus.ParametrosResiduos,
                    campus.ParametrosOperacion.UmbralProgramacionPct)
            };
        }

        private static double? IntentarCalcular(Func<double> calculo)
        {
            try
            {
                return calculo();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                return null;
            }
        }
    }
}
