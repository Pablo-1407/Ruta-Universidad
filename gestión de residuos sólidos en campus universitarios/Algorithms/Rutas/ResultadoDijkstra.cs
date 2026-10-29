namespace gestión_de_residuos_sólidos_en_campus_universitarios.Algorithms.Rutas
{
    internal sealed class ResultadoDijkstra
    {
        private readonly int _origen;
        private readonly IReadOnlyDictionary<int, double> _distancias;
        private readonly IReadOnlyDictionary<int, int> _predecesores;

        public ResultadoDijkstra(
            int origen,
            IReadOnlyDictionary<int, double> distancias,
            IReadOnlyDictionary<int, int> predecesores)
        {
            _origen = origen;
            _distancias = distancias;
            _predecesores = predecesores;
        }

        public double DistanciaHasta(int destino) =>
            _distancias.TryGetValue(destino, out double distancia) ? distancia : double.PositiveInfinity;

        public IReadOnlyList<int> CaminoHasta(int destino)
        {
            if (double.IsPositiveInfinity(DistanciaHasta(destino)))
            {
                return [];
            }

            List<int> camino = [destino];
            int actual = destino;
            while (actual != _origen)
            {
                if (!_predecesores.TryGetValue(actual, out actual))
                {
                    return [];
                }

                camino.Add(actual);
            }

            camino.Reverse();
            return camino;
        }
    }
}
