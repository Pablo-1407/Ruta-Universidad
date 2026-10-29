using gestión_de_residuos_sólidos_en_campus_universitarios.Data;
using gestión_de_residuos_sólidos_en_campus_universitarios.Algorithms.Priorizacion;
using gestión_de_residuos_sólidos_en_campus_universitarios.Algorithms.Rutas;
using gestión_de_residuos_sólidos_en_campus_universitarios.Presenters;
using gestión_de_residuos_sólidos_en_campus_universitarios.Services;

namespace gestión_de_residuos_sólidos_en_campus_universitarios
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            Main vista = new();
            RecoleccionService servicio = new(
                new PriorizacionPonderada(),
                new VecinoMasCercano(),
                new RutaSinOptimizar(),
                new IndicadoresService());
            RutaPresenter presenter = new(vista, new TxtCampusRepository(), servicio);
            presenter.Inicializar();

            Application.Run(vista);
        }
    }
}