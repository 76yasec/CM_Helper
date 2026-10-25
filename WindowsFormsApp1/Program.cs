using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

class Program
{
  [STAThread]
  static void Main()
  {
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Application.Run(new CirclePointerForm());
  }
}

class CirclePointerForm : Form
{
  TextBox angleTextBox;
  Button updateButton;
  CheckBox degreesCheck;
  Label infoLabel;
  double angleRad = 0.0; // Winkel in Radiant, 0 = links (9 Uhr), positiv = Uhrzeigersinn
  double lastValidAngleRad = 0.0;

  // Controls für Liter/min -> m³/s
  Label lpmInputLabel;
  TextBox lpmTextBox;
  Label lpmResultLabel;

  public CirclePointerForm()
  {
    Text = "Kreis mit Zeiger (0 = 9 Uhr) - Live-Update";
    ClientSize = new Size(420, 540);

    angleTextBox = new TextBox { Location = new Point(12, 370), Width = 120, Text = "0" };
    updateButton = new Button { Location = new Point(140, 368), Text = "Aktualisieren", AutoSize = true };
    degreesCheck = new CheckBox { Location = new Point(260, 370), Text = "Grad eingeben", AutoSize = true };
    infoLabel = new Label { Location = new Point(12, 400), Width = 390, Height = 40, Text = "Gib Winkel (Radiant) ein. 0 = links (9 Uhr). Positiv = Uhrzeigersinn." };

    updateButton.Click += UpdateButton_Click;
    angleTextBox.TextChanged += AngleTextBox_TextChanged;
    degreesCheck.CheckedChanged += DegreesCheck_CheckedChanged;

    Controls.Add(angleTextBox);
    Controls.Add(updateButton);
    Controls.Add(degreesCheck);
    Controls.Add(infoLabel);

    // ----- Abschnitt: Liter/min -> m³/s -----
    lpmInputLabel = new Label { Location = new Point(12, 450), Text = "Volumenstrom (L/min):", AutoSize = true };
    lpmTextBox = new TextBox { Location = new Point(140, 446), Width = 100, Text = "0" };
    lpmResultLabel = new Label { Location = new Point(12, 480), Width = 390, Height = 40, Text = "m³/s: 0" };

    lpmTextBox.TextChanged += LpmTextBox_TextChanged;

    Controls.Add(lpmInputLabel);
    Controls.Add(lpmTextBox);
    Controls.Add(lpmResultLabel);
    // -----------------------------------------

    DoubleBuffered = true;
    Paint += CirclePointerForm_Paint;
  }

  private void UpdateButton_Click(object sender, EventArgs e)
  {
    // Manuelle Aktualisierung - identisch zu Live-Update (nur als Alternative)
    TryParseAndUpdate(angleTextBox.Text);
  }

  private void DegreesCheck_CheckedChanged(object sender, EventArgs e)
  {
    // Wenn zwischen Grad/Radian gewechselt wird, neu parsen mit der neuen Interpretation
    TryParseAndUpdate(angleTextBox.Text);
  }

  private void AngleTextBox_TextChanged(object sender, EventArgs e)
  {
    // Live-Update beim Tippen: nur übernehmen, wenn gültig
    TryParseAndUpdate(angleTextBox.Text, showError: false);
  }

  private void TryParseAndUpdate(string text, bool showError = true)
  {
    text = text.Trim();
    if (text.Length == 0)
    {
      // leere Eingabe: ignoriere (behalte letzten gültigen Wert)
      if (showError) ShowInfo("Eingabe leer", Color.OrangeRed);
      return;
    }

    if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out double val))
    {
      double rad = degreesCheck.Checked ? val * Math.PI / 180.0 : val;
      angleRad = rad;
      lastValidAngleRad = angleRad;
      Invalidate(); // neu zeichnen
      ShowInfo(string.Format(CultureInfo.InvariantCulture, "θ = {0:0.###} rad ({1:0.##}°)", angleRad, angleRad * 180.0 / Math.PI), Color.Black);
    }
    else
    {
      if (showError)
        ShowInfo("Ungültige Zahl. Nutze z.B. 0, 1.57 oder bei Grad '90' (mit Grad-Checkbox).", Color.Red);
      else
        ShowInfo("Eingabe nicht zahlbar", Color.OrangeRed);
      // kein Invalidate: behalte sichtbaren letzten gültigen Zustand
    }
  }

  private void ShowInfo(string text, Color color)
  {
    infoLabel.Text = text;
    infoLabel.ForeColor = color;
  }

  private void LpmTextBox_TextChanged(object sender, EventArgs e)
  {
    // Live-Update der Umrechnung L/min -> m³/s, zusätzlich wissenschaftliche Darstellung
    string txt = lpmTextBox.Text.Trim();
    if (txt.Length == 0)
    {
      lpmResultLabel.Text = "m³/s: (Eingabe leer)";
      lpmResultLabel.ForeColor = Color.OrangeRed;
      return;
    }

    if (double.TryParse(txt, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out double lpm))
    {
      double m3s = ConvertLpmToM3PerS(lpm);

      // Normale Darstellung (hier: 6 signifikante Stellen) und wissenschaftliche Darstellung (E-Format)
      string normal = m3s.ToString("G6", CultureInfo.InvariantCulture);
      string sci = m3s.ToString("E6", CultureInfo.InvariantCulture); // z.B. "1.666667E-05"

      lpmResultLabel.Text = $"m³/s: {normal}    (wissenschaftlich: {sci})";
      lpmResultLabel.ForeColor = Color.Black;
    }
    else
    {
      lpmResultLabel.Text = "m³/s: Ungültige Zahl";
      lpmResultLabel.ForeColor = Color.Red;
    }
  }

  private static double ConvertLpmToM3PerS(double litersPerMinute)
  {
    // 1 Liter = 0.001 m^3
    // L/min -> m^3/s : litersPerMinute * 0.001 / 60
    return litersPerMinute * 0.001 / 60.0;
  }

  private void CirclePointerForm_Paint(object sender, PaintEventArgs e)
  {
    Graphics g = e.Graphics;
    g.SmoothingMode = SmoothingMode.AntiAlias;

    // Kreis Bereich
    int padding = 40;
    int size = Math.Min(ClientSize.Width, 340) - padding;
    Rectangle rect = new Rectangle((ClientSize.Width - size) / 2, 20, size, size);

    // Kreis zeichnen
    using (Pen pen = new Pen(Color.Black, 2))
    {
      g.DrawEllipse(pen, rect);
    }

    // Zentrum und Radius
    float cx = rect.Left + rect.Width / 2f;
    float cy = rect.Top + rect.Height / 2f;
    float radius = rect.Width / 2f - 8f;

    // Mapping wie erklärt:
    // x = cx - r * cos(theta)
    // y = cy - r * sin(theta)
    double theta = angleRad;
    float x = (float)(cx - radius * Math.Cos(theta));
    float y = (float)(cy - radius * Math.Sin(theta));

    // Zeiger zeichnen
    using (Pen pen = new Pen(Color.Red, 3))
    {
      g.DrawLine(pen, cx, cy, x, y);
    }

    // Mittelpunkt markieren
    using (Brush b = new SolidBrush(Color.Black))
    {
      float r = 4f;
      g.FillEllipse(b, cx - r, cy - r, 2 * r, 2 * r);
    }

    // Beschriftung: Winkel in Radiant und Grad (letzter gültiger Wert)
    string txt = string.Format(CultureInfo.InvariantCulture, "θ = {0:0.###} rad  ({1:0.##}°)", lastValidAngleRad, lastValidAngleRad * 180.0 / Math.PI);
    g.DrawString(txt, SystemFonts.DefaultFont, Brushes.Black, new PointF(10, rect.Bottom + 10));
  }
}