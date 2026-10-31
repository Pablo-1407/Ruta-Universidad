using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Services
{
    internal sealed record ProblemaDensidadResiduo(
        TipoResiduo Tipo,
        IReadOnlyList<string> InformacionPendiente);

    internal sealed class DensidadesResiduosIncompletasException : InvalidOperationException
    {
        public IReadOnlyList<ProblemaDensidadResiduo> Problemas { get; }

        public DensidadesResiduosIncompletasException(IEnumerable<ProblemaDensidadResiduo> problemas)
            : base("La configuración de densidades por residuo está incompleta.")
        {
            Problemas = Array.AsReadOnly(problemas
                .Select(problema => new ProblemaDensidadResiduo(
                    problema.Tipo,
                    Array.AsReadOnly(problema.InformacionPendiente.ToArray())))
                .ToArray());
        }
    }
}
