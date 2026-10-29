using System.Globalization;
using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Views
{
    internal sealed class ConexionDialog : Form
    {
        private readonly ComboBox _origen = new();
        private readonly ComboBox _destino = new();
        private readonly TextBox _metros = new();

        public ConexionCampus Conexion { get; private set; } = new();

        public ConexionDialog(IReadOnlyList<PuntoRecoleccion> puntos, ConexionCampus? original)
        {
            Text = original is null ? "Nueva conexión" : "Editar conexión";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(410, 220);

            ConfigurarCombo(_origen, puntos);
            ConfigurarCombo(_destino, puntos);
            Controls.Add(CrearContenido());

            if (original is null)
            {
                _origen.SelectedIndex = puntos.Count > 0 ? 0 : -1;
                _destino.SelectedIndex = puntos.Count > 1 ? 1 : -1;
                _metros.Text = "1";
            }
            else
            {
                SeleccionarPunto(_origen, original.OrigenId);
                SeleccionarPunto(_destino, original.DestinoId);
                _metros.Text = original.Metros.ToString(CultureInfo.CurrentCulture);
            }
        }

        private static void ConfigurarCombo(ComboBox combo, IReadOnlyList<PuntoRecoleccion> puntos)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.DisplayMember = nameof(PuntoRecoleccion.Nombre);
            combo.ValueMember = nameof(PuntoRecoleccion.Id);
            combo.DataSource = puntos.ToList();
            combo.Dock = DockStyle.Fill;
        }

        private static void SeleccionarPunto(ComboBox combo, int id)
        {
            for (int indice = 0; indice < combo.Items.Count; indice++)
            {
                if (combo.Items[indice] is PuntoRecoleccion punto && punto.Id == id)
                {
                    combo.SelectedIndex = indice;
                    return;
                }
            }
        }

        private Control CrearContenido()
        {
            TableLayoutPanel contenido = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 2,
                RowCount = 4
            };
            contenido.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
            contenido.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

            AgregarFila(contenido, 0, "Punto de origen", _origen);
            AgregarFila(contenido, 1, "Punto de destino", _destino);
            AgregarFila(contenido, 2, "Distancia (metros)", _metros);

            FlowLayoutPanel botones = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };
            Button guardar = new() { Text = "Guardar", AutoSize = true };
            Button cancelar = new() { Text = "Cancelar", AutoSize = true, DialogResult = DialogResult.Cancel };
            guardar.Click += Guardar;
            botones.Controls.Add(guardar);
            botones.Controls.Add(cancelar);
            contenido.Controls.Add(botones, 0, 3);
            contenido.SetColumnSpan(botones, 2);

            AcceptButton = guardar;
            CancelButton = cancelar;
            return contenido;
        }

        private static void AgregarFila(TableLayoutPanel panel, int fila, string etiqueta, Control control)
        {
            panel.Controls.Add(new Label
            {
                Text = etiqueta,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 8, 3, 8)
            }, 0, fila);
            control.Margin = new Padding(3, 5, 3, 5);
            panel.Controls.Add(control, 1, fila);
        }

        private void Guardar(object? sender, EventArgs e)
        {
            if (_origen.SelectedItem is not PuntoRecoleccion origen
                || _destino.SelectedItem is not PuntoRecoleccion destino
                || !double.TryParse(_metros.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out double metros))
            {
                MessageBox.Show(this, "Selecciona dos puntos y escribe una distancia válida.", "Datos no válidos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Conexion = new ConexionCampus
            {
                OrigenId = origen.Id,
                DestinoId = destino.Id,
                Metros = metros
            };
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
