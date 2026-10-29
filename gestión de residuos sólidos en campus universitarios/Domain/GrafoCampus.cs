namespace gestión_de_residuos_sólidos_en_campus_universitarios.Domain
{
    internal sealed class GrafoCampus
    {
        private readonly Dictionary<int, Dictionary<int, double>> _adyacencias = [];

        public IReadOnlyCollection<int> Nodos => _adyacencias.Keys;

        public void AgregarNodo(int id)
        {
            if (!_adyacencias.TryAdd(id, []))
            {
                throw new InvalidOperationException($"El nodo {id} ya existe en el grafo.");
            }
        }

        public void AgregarArista(int origen, int destino, double metros)
        {
            if (origen == destino)
            {
                throw new ArgumentException("Una conexión debe unir dos puntos diferentes.");
            }

            if (!double.IsFinite(metros) || metros <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(metros), "La distancia debe ser mayor que cero.");
            }

            if (!_adyacencias.TryGetValue(origen, out Dictionary<int, double>? vecinosOrigen))
            {
                throw new ArgumentException($"El punto de origen {origen} no existe en el grafo.", nameof(origen));
            }

            if (!_adyacencias.TryGetValue(destino, out Dictionary<int, double>? vecinosDestino))
            {
                throw new ArgumentException($"El punto de destino {destino} no existe en el grafo.", nameof(destino));
            }

            if (vecinosOrigen.ContainsKey(destino))
            {
                throw new InvalidOperationException("La conexión entre esos puntos ya existe.");
            }

            vecinosOrigen.Add(destino, metros);
            vecinosDestino.Add(origen, metros);
        }

        public IReadOnlyDictionary<int, double> Vecinos(int id)
        {
            if (!_adyacencias.TryGetValue(id, out Dictionary<int, double>? vecinos))
            {
                throw new ArgumentException($"El punto {id} no existe en el grafo.", nameof(id));
            }

            return vecinos;
        }

        public void EliminarNodo(int id)
        {
            if (!_adyacencias.Remove(id))
            {
                return;
            }

            foreach (Dictionary<int, double> vecinos in _adyacencias.Values)
            {
                vecinos.Remove(id);
            }
        }
    }
}
