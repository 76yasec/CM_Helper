using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

internal static class Program
{
  [STAThread]
  private static void Main()
  {
    RunStartupFormatChecks();
    ApplicationConfiguration.Initialize();
    Application.Run(new ProjectEditorForm());
  }

  private static void RunStartupFormatChecks()
  {
    const double litersPerMinute = 1.0;
    const double expectedM3PerSecond = litersPerMinute * 0.001 / 60.0;

    string formatted = expectedM3PerSecond.ToString("0.00e+00", CultureInfo.InvariantCulture);

    if (!formatted.Contains('e') || formatted.Contains("e+00", StringComparison.OrdinalIgnoreCase))
      throw new InvalidOperationException($"Format check failed for scientific notation: '{formatted}'");

    if (Math.Abs(expectedM3PerSecond - 1.6666666666666667e-05) > 1e-12)
      throw new InvalidOperationException($"Expected conversion value mismatch: {expectedM3PerSecond}");
  }
}

public class ProjectEditorForm : Form
{
  private readonly string appFolder = AppContext.BaseDirectory;
  private const string StandardProjectFileName = "standard_project.txt";
