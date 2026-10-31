using gestión_de_residuos_sólidos_en_campus_universitarios.Data;
using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Campus.Tests;

[TestClass]
public sealed class TxtCampusRepositoryTests
{
    [TestMethod]
    public void GuardarYCargar_ConservaDensidadYFuenteEnFormatoTxt()
    {
        string ruta = Path.Combine(Path.GetTempPath(), $"campus-{Guid.NewGuid():N}.txt");
        CampusData campus = new();
        campus.ParametrosResiduos.Add(new ParametrosResiduo
        {
            Tipo = TipoResiduo.Reciclable,
            DensidadKgPorLitro = 0.42,
            FuenteDensidad = "Estudio técnico"
        });

        try
        {
            TxtCampusRepository repositorio = new();
            repositorio.Guardar(campus, ruta);

            CampusData cargado = repositorio.Cargar(ruta);
            ParametrosResiduo densidad = cargado.ParametrosResiduos.Single();

            Assert.AreEqual(TipoResiduo.Reciclable, densidad.Tipo);
            Assert.AreEqual(0.42, densidad.DensidadKgPorLitro, 0.0001);
            Assert.AreEqual("Estudio técnico", densidad.FuenteDensidad);
            Assert.IsTrue(File.ReadLines(ruta).Contains("PARAMETROS_RESIDUOS"));
        }
        finally
        {
            File.Delete(ruta);
        }
    }
}
