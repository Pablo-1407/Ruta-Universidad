using System.Globalization;
using gestión_de_residuos_sólidos_en_campus_universitarios.Domain;

namespace gestión_de_residuos_sólidos_en_campus_universitarios.Views
{
    internal sealed class PuntoDialog : Form
    {
        private readonly double _x;
        private readonly double _y;
        private readonly TextBox _nombre = new();
        private readonly ComboBox _tipo = new();
        private readonly ComboBox _tipoResiduo = new();
        private readonly TextBox _capacidad = new();
        private readonly NumericUpDown _nivelLlenado = new();
        private readonly TextBox _generacion = new();
        private readonly TextBox _diasSinRecoleccion = new();
        private readonly TextBox _pesoZona = new();
        private readonly PuntoRecoleccion? _puntoOriginal;

        public PuntoRecoleccion Punto { get; private set; } = new();

        public PuntoDialog(double x, double y, PuntoRecoleccion? puntoOriginal)
        {
            _x = x;
            _y = y;
            _puntoOriginal = puntoOriginal;

            Text = puntoOriginal is null ? "Nuevo punto de recolección" : "Editar punto de recolección";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(460, 430);

            ConfigurarControles();
            Controls.Add(CrearContenido());
        }

        private void ConfigurarControles()
        {
            _nombre.MaxLength = 100;
            _nombre.Text = _puntoOriginal?.Nombre ?? string.Empty;

            _tipo.DropDownStyle = ComboBoxStyle.DropDownList;
            _tipo.Items.AddRange(["Punto de recolección", "Centro de acopio"]);
            _tipo.SelectedIndex = _puntoOriginal?.Tipo == TipoPunto.CentroAcopio ? 1 : 0;
            _tipo.SelectedIndexChanged += (_, _) => ActualizarControles();

            _tipoResiduo.DropDownStyle = ComboBoxStyle.DropDownList;
            _tipoResiduo.Items.AddRange(["Orgánico", "Reciclable", "No aprovechable", "Peligroso", "Sin clasificar"]);
            _tipoResiduo.SelectedIndex = _puntoOriginal is null ? 0 : (int)_puntoOriginal.TipoResiduo;
            _capacidad.Text = _puntoOriginal?.CapacidadLitros.ToString(CultureInfo.CurrentCulture) ?? "0";

            _nivelLlenado.Minimum = 0;
            _nivelLlenado.Maximum = 100;
            _nivelLlenado.DecimalPlaces = 1;
            _nivelLlenado.Increment = 1;
            _nivelLlenado.Value = Convert.ToDecimal(Math.Clamp(_puntoOriginal?.NivelLlenadoPct ?? 0, 0, 100));

            _generacion.Text = _puntoOriginal?.GeneracionKgDia.ToString(CultureInfo.CurrentCulture) ?? "0";
            _diasSinRecoleccion.Text = _puntoOriginal?.DiasSinRecoleccion.ToString(CultureInfo.CurrentCulture) ?? "0";
            _pesoZona.Text = _puntoOriginal?.PesoZona.ToString(CultureInfo.CurrentCulture) ?? "1";
        }

        private Control CrearContenido()
        {
            TableLayoutPanel contenido = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 2,
                RowCount = 10
            };
            contenido.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 165));
            contenido.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            AgregarFila(contenido, 0, "Nombre", _nombre);
            AgregarFila(contenido, 1, "Tipo", _tipo);
            AgregarFila(contenido, 2, "Residuo", _tipoResiduo);
            AgregarFila(contenido, 3, "Capacidad (litros)", _capacidad);
            AgregarFila(contenido, 4, "Nivel de llenado (%)", _nivelLlenado);
            AgregarFila(contenido, 5, "Generación (kg/día)", _generacion);
            AgregarFila(contenido, 6, "Días sin recolección", _diasSinRecoleccion);
            AgregarFila(contenido, 7, "Peso de zona", _pesoZona);

            Label ubicacion = new()
            {
                AutoSize = true,
                Text = $"Ubicación del mapa: {Math.Round(_x * 100, 1)} %, {Math.Round(_y * 100, 1)} %",
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 10, 3, 10)
            };
            contenido.Controls.Add(ubicacion, 0, 8);
            contenido.SetColumnSpan(ubicacion, 2);

            FlowLayoutPanel botones = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 8, 0, 0)
            };
            Button aceptar = new() { Text = "Guardar", AutoSize = true, DialogResult = DialogResult.None };
            Button cancelar = new() { Text = "Cancelar", AutoSize = true, DialogResult = DialogResult.Cancel };
            aceptar.Click += Aceptar;
            botones.Controls.Add(aceptar);
            botones.Controls.Add(cancelar);
            contenido.Controls.Add(botones, 0, 9);
            contenido.SetColumnSpan(botones, 2);

            AcceptButton = aceptar;
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
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(3, 5, 3, 5);
            panel.Controls.Add(control, 1, fila);
        }

        private void ActualizarControles()
        {
            bool esRecoleccion = _tipo.SelectedIndex != 1;
            _nivelLlenado.Enabled = esRecoleccion;
            _tipoResiduo.Enabled = esRecoleccion;
            _capacidad.Enabled = esRecoleccion;
            _generacion.Enabled = esRecoleccion;
            _diasSinRecoleccion.Enabled = esRecoleccion;
            _pesoZona.Enabled = esRecoleccion;
        }

        private void Aceptar(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_nombre.Text))
            {
                MessageBox.Show(this, "Escribe el nombre del punto.", "Dato requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _nombre.Focus();
                return;
            }

            bool esCentroAcopio = _tipo.SelectedIndex == 1;
            double generacion = 0;
            double capacidad = 0;
            int dias = 0;
            double pesoZona = 0;
            if (!esCentroAcopio
                && (!double.TryParse(_capacidad.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out capacidad)
                    || capacidad <= 0
                    || !double.TryParse(_generacion.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out generacion)
                    || !int.TryParse(_diasSinRecoleccion.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out dias)
                    || !double.TryParse(_pesoZona.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out pesoZona)))
            {
                MessageBox.Show(this, "Revisa los valores de generación, días y peso de zona.", "Datos no válidos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Punto = new PuntoRecoleccion
            {
                Id = _puntoOriginal?.Id ?? 0,
                Nombre = _nombre.Text.Trim(),
                Tipo = esCentroAcopio ? TipoPunto.CentroAcopio : TipoPunto.Recoleccion,
                TipoResiduo = esCentroAcopio ? TipoResiduo.SinClasificar : (TipoResiduo)_tipoResiduo.SelectedIndex,
                X = _x,
                Y = _y,
                CapacidadLitros = capacidad,
                NivelLlenadoPct = esCentroAcopio ? 0 : (double)_nivelLlenado.Value,
                GeneracionKgDia = generacion,
                DiasSinRecoleccion = dias,
                PesoZona = pesoZona
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
