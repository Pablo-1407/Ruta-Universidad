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
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            Main vista = new();
            RecoleccionService servicio = new(
                new PriorizacionPonderada(),
                new VecinoMasCercano(),
                new RutaSinOptimizar(),
                new IndicadoresService());
            ManejadorErrores manejadorErrores = new((mensaje, error) => vista.MostrarMensaje(mensaje, error));
            Application.ThreadException += (_, args) =>
            {
                manejadorErrores.Manejar(args.Exception, "evento no controlado de la interfaz", registrarSiempre: true);
                Application.Exit();
            };
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                Exception excepcion = args.ExceptionObject as Exception
                    ?? new InvalidOperationException(Convert.ToString(args.ExceptionObject) ?? "Error no controlado.");
                manejadorErrores.Manejar(excepcion, "excepción no controlada del proceso", registrarSiempre: true);
            };
            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                args.SetObserved();
                manejadorErrores.Manejar(args.Exception.Flatten(), "tarea en segundo plano", registrarSiempre: true);
            };

            RutaPresenter presenter = new(vista, new TxtCampusRepository(), servicio, manejadorErrores);
            try
            {
                presenter.Inicializar();
                Application.Run(vista);
            }
            catch (Exception excepcion)
            {
                manejadorErrores.Manejar(excepcion, "iniciar la aplicación", registrarSiempre: true);
            }
        }
    }
}