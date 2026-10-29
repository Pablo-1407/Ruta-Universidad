using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Algorithms.Rutas
{
    internal interface IEstrategiaRuta
    {
        Ruta Calcular(
            GrafoCampus grafo,
            IReadOnlyList<PuntoRecoleccion> puntosCampus,
            PuntoRecoleccion centroAcopio,
            IReadOnlyList<PuntoRecoleccion> puntosAVisitar);
    }
}
