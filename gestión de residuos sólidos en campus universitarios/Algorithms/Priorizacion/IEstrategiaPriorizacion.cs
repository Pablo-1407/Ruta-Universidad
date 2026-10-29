using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Algorithms.Priorizacion
{
    internal interface IEstrategiaPriorizacion
    {
        IReadOnlyList<PuntoPriorizado> Priorizar(IEnumerable<PuntoRecoleccion> puntos);
    }
}
