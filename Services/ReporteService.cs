using System.Globalization;
using ClosedXML.Excel;
using ControlLavados.Data;
using ControlLavados.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlLavados.Services;

public record MetricaSemana(
    int Semana,
    int Lavados,
    TimeSpan TotalHoras,
    TimeSpan PromedioHoras,
    int TotalOperarios,
    double PromedioOperarios,
    double? VarHoras,
    double? VarLavados);

public record MetricasTurno(string Turno, List<MetricaSemana> Semanas);

public class ReporteService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-AR");
    private static readonly string[] OrdenTurnos = { Turnos.Mañana, Turnos.Tarde, Turnos.Noche };

    public ReporteService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    /// <summary>Lavados finalizados (los que tienen tiempos completos) para el rango/tipo dado.</summary>
    public async Task<List<Lavado>> ObtenerAsync(TipoLavado? tipo, DateOnly? desde, DateOnly? hasta)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var q = db.Lavados.Include(l => l.Operarios)
            .Where(l => l.Estado == EstadoLavado.Finalizado);

        if (tipo.HasValue) q = q.Where(l => l.Tipo == tipo.Value);
        if (desde.HasValue) q = q.Where(l => l.Fecha >= desde.Value);
        if (hasta.HasValue) q = q.Where(l => l.Fecha <= hasta.Value);

        return await q.OrderBy(l => l.Id).ToListAsync();
    }

    /// <summary>Agrupa por turno y semana, calculando totales, promedios y variación semana a semana.</summary>
    public List<MetricasTurno> CalcularMetricas(List<Lavado> lavados)
    {
        var resultado = new List<MetricasTurno>();

        foreach (var turno in OrdenTurnos)
        {
            var delTurno = lavados.Where(l => l.Turno == turno).ToList();
            if (delTurno.Count == 0) continue;

            var semanas = new List<MetricaSemana>();
            MetricaSemana? previa = null;

            foreach (var grupo in delTurno.GroupBy(l => l.Semana).OrderBy(g => g.Key))
            {
                var items = grupo.ToList();
                var totalHoras = items.Aggregate(TimeSpan.Zero, (acc, l) => acc + (l.TiempoTotal ?? TimeSpan.Zero));
                var cant = items.Count;
                var totalOp = items.Sum(l => l.OperariosUsados);

                double? varHoras = previa is { TotalHoras.Ticks: > 0 }
                    ? (totalHoras.TotalSeconds - previa.TotalHoras.TotalSeconds) / previa.TotalHoras.TotalSeconds
                    : null;
                double? varLav = previa is { Lavados: > 0 }
                    ? (double)(cant - previa.Lavados) / previa.Lavados
                    : null;

                var m = new MetricaSemana(
                    grupo.Key, cant, totalHoras,
                    TimeSpan.FromSeconds(totalHoras.TotalSeconds / cant),
                    totalOp, (double)totalOp / cant, varHoras, varLav);

                semanas.Add(m);
                previa = m;
            }

            resultado.Add(new MetricasTurno(turno, semanas));
        }

        return resultado;
    }

    // ---------- Excel ----------

    public byte[] GenerarExcel(List<Lavado> lavados, List<MetricasTurno> metricas, TipoLavado? tipo)
    {
        using var wb = new XLWorkbook();
        EscribirMetricas(wb.AddWorksheet("Métricas"), metricas);
        EscribirDetalle(wb.AddWorksheet("Detalle"), lavados, tipo);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void EscribirMetricas(IXLWorksheet ws, List<MetricasTurno> metricas)
    {
        int row = 1;
        foreach (var t in metricas)
        {
            ws.Cell(row, 1).Value = $"TURNO: {t.Turno}";
            ws.Range(row, 1, row, 8).Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);
            row++;

            string[] headers = { "Semana", "Lavados", "Total hs", "Prom. hs", "Operarios", "Prom. op.", "% Var. hs", "% Var. lavados" };
            for (int c = 0; c < headers.Length; c++)
            {
                ws.Cell(row, c + 1).Value = headers[c];
                ws.Cell(row, c + 1).Style.Font.SetBold();
            }
            row++;

            foreach (var s in t.Semanas)
            {
                ws.Cell(row, 1).Value = s.Semana;
                ws.Cell(row, 2).Value = s.Lavados;
                ws.Cell(row, 3).Value = FmtDur(s.TotalHoras);
                ws.Cell(row, 4).Value = FmtDur(s.PromedioHoras);
                ws.Cell(row, 5).Value = s.TotalOperarios;
                ws.Cell(row, 6).Value = Math.Round(s.PromedioOperarios, 1);
                ws.Cell(row, 7).Value = s.VarHoras.HasValue ? s.VarHoras.Value.ToString("P1", Es) : "—";
                ws.Cell(row, 8).Value = s.VarLavados.HasValue ? s.VarLavados.Value.ToString("P1", Es) : "—";
                row++;
            }
            row++; // fila en blanco entre turnos
        }
        ws.Columns().AdjustToContents();
    }

    private static void EscribirDetalle(IXLWorksheet ws, List<Lavado> lavados, TipoLavado? tipo)
    {
        var soloCamion = tipo == TipoLavado.Camion;
        string[] headers = soloCamion
            ? new[] { "Marca temporal", "Fecha", "Turno", "N° Offal", "N° Agencia", "Patente", "Dársena", "Frigorífico",
                      "Tambores", "Pallets", "Operarios", "Inicio Atraco", "Inicio Lavado", "Fin Lavado", "Desatraco",
                      "Atraco→Lavado", "Lavado", "Fin→Desatraco", "Total", "Semana", "Op. usados", "Incidencias", "Estado" }
            : new[] { "Marca temporal", "Fecha", "Turno", "N° Offal", "N° Agencia", "Tipo", "Equipo/Patente",
                      "Operarios", "Inicio Lavado", "Fin Lavado", "Total", "Semana", "Op. usados", "Incidencias", "Estado" };

        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(1, c + 1).Value = headers[c];
            ws.Cell(1, c + 1).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);
        }

        int row = 2;
        foreach (var l in lavados)
        {
            int c = 1;
            ws.Cell(row, c++).Value = FmtFechaHora(l.MarcaTemporal);
            ws.Cell(row, c++).Value = l.Fecha.ToString("dd/MM/yyyy");
            ws.Cell(row, c++).Value = l.Turno;
            ws.Cell(row, c++).Value = l.NumOffal;
            ws.Cell(row, c++).Value = l.NumAgencia;
            if (soloCamion)
            {
                ws.Cell(row, c++).Value = l.Patente;
                ws.Cell(row, c++).Value = l.Darsena ?? "";
                ws.Cell(row, c++).Value = l.Frigorifico ?? "";
                ws.Cell(row, c++).Value = l.Tambores;
                ws.Cell(row, c++).Value = l.Pallets;
                ws.Cell(row, c++).Value = l.OperariosTexto;
                ws.Cell(row, c++).Value = FmtHora(l.InicioAtraco);
                ws.Cell(row, c++).Value = FmtHora(l.InicioLavado);
                ws.Cell(row, c++).Value = FmtHora(l.FinLavado);
                ws.Cell(row, c++).Value = FmtHora(l.Desatraco);
                ws.Cell(row, c++).Value = FmtDur(l.DurAtracoLavado);
                ws.Cell(row, c++).Value = FmtDur(l.DurLavado);
                ws.Cell(row, c++).Value = FmtDur(l.DurFinDesatraco);
                ws.Cell(row, c++).Value = FmtDur(l.TiempoTotal);
            }
            else
            {
                ws.Cell(row, c++).Value = l.Tipo.ToString();
                ws.Cell(row, c++).Value = l.Patente;
                ws.Cell(row, c++).Value = l.OperariosTexto;
                ws.Cell(row, c++).Value = FmtHora(l.InicioLavado);
                ws.Cell(row, c++).Value = FmtHora(l.FinLavado);
                ws.Cell(row, c++).Value = FmtDur(l.TiempoTotal);
            }
            ws.Cell(row, c++).Value = l.Semana;
            ws.Cell(row, c++).Value = l.OperariosUsados;
            ws.Cell(row, c++).Value = l.Incidencias ?? "";
            ws.Cell(row, c++).Value = l.Estado;
            row++;
        }
        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();
    }

    private static string FmtHora(DateTime? d) => d?.ToString("HH:mm:ss", Es) ?? "";
    private static string FmtFechaHora(DateTime? d) => d?.ToString("dd/MM/yyyy HH:mm:ss", Es) ?? "";
    private static string FmtDur(TimeSpan? t) =>
        t.HasValue ? $"{(int)t.Value.TotalHours}:{t.Value.Minutes:00}:{t.Value.Seconds:00}" : "";
}
