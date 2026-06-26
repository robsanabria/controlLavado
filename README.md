# Control de Lavados

Aplicación web interna para **registrar lavados y asignar operarios**, calculando automáticamente los tiempos de cada etapa. Cubre cuatro circuitos:

- **Camiones** — proceso de 4 etapas (Atraco → Inicio lavado → Fin lavado → Desatraco), con hasta **2 dársenas en paralelo**.
- **Fábrica de Hielo**, **Hiel** y **Varias** — proceso de 2 etapas (Inicio lavado → Fin lavado), con sector fijo.

Cada lavado guarda los operarios asignados (de **Offal** o **Contrato**) e incidencias; en camiones además registra frigorífico, tambores y pallets. Una grilla muestra los lavados en curso y la pestaña de Reportes calcula las métricas operativas.

---

## Tecnologías

| Capa | Tecnología |
|------|------------|
| Framework | **.NET 9** |
| UI | **Blazor Web App** (render mode `InteractiveServer`) |
| ORM / Datos | **Entity Framework Core 9** |
| Base de datos | **SQL Server** (LocalDB en desarrollo) |
| Export Excel | **ClosedXML** |
| Lenguaje | **C#** + Razor |
| Estilos | CSS propio (tema oscuro, mobile-first, sin dependencias externas) |

> Esquema creado automáticamente con `EnsureCreated()` — no usa migraciones.

---

## Funcionalidades

- **Alta de lavados** con desplegables de patente/sector, dársena, frigorífico, tambores y pallets (según el circuito), tomados de catálogos editables.
- **Selección de operarios** por combo box, diferenciados en Offal / Contrato, con varios operarios por lavado.
- **Validación de obligatorios** y reglas de disponibilidad: no se puede reasignar una patente, dársena u operario que ya está en un lavado en curso (validado también en el servidor).
- **Botones de etapa secuenciales** que se habilitan en orden y estampan la hora actual; el botón cambia de color según el estadio (atracado/lavando/lavado terminado).
- **ABM de catálogos** (`/catalogos`): alta, baja y modificación de Operarios (Nombre, Apellido, DNI, tipo Offal/Contrato), Patentes y Frigoríficos desde la UI.

### Estados del proceso

```
Camión:               Pendiente → Atracado → Lavando → Lavado terminado → Finalizado
Hielo / Hiel / Varias: Pendiente → Lavando → Finalizado
```

---

## Reportería (`/reportes`)

Toda la reportería trabaja **solo sobre lavados finalizados** (los que tienen el ciclo de tiempos completo), filtrables por **circuito** (Camiones / Hielo / Hiel / Varias / Todos) y por **rango de fechas** (Desde–Hasta).

### Conceptos de cálculo

- **Tiempo total de lavado**
  - Camión: `Desatraco − Inicio Atraco`.
  - Hielo / Hiel / Varias: `Fin lavado − Inicio lavado`.
- **Turno** (se deduce de la hora de inicio): **Mañana** = 06:00–18:59; **Tarde** = el resto (19:00–05:59).
- **Semana**: número de **semana ISO** de la fecha del lavado.
- **Crédito de horas a operarios**: a cada operario presente en un lavado se le imputa la **duración total** de ese lavado. Si dos personas lavan un camión de 40 min, cada una suma 40 min.
- **Días trabajados**: cantidad de **días distintos** en que el operario participó de al menos un lavado (derivado de los lavados; la asistencia real es un módulo aparte, ver Pendientes).

### Reportes en pantalla

1. **Métricas semanales** (por turno) — agrupa por turno y semana:
   - `Lavados` = cantidad de lavados de esa semana/turno.
   - `Total hs` = suma de los tiempos totales.
   - `Prom. hs` = `Total hs ÷ Lavados`.
   - `Operarios` = suma de operarios usados; `Prom. op.` = `Operarios ÷ Lavados`.
   - `% Var. hs` y `% Var. lavados` = variación porcentual contra la **semana inmediatamente anterior (N−1)**, solo si esa semana tiene datos; si no, muestra `—`. Fórmula: `(valor_N − valor_(N−1)) / valor_(N−1)`.

2. **Horas por operario** — matriz por turno + operario:
   - Una columna por semana con las **horas trabajadas** (suma de tiempos totales de los lavados en que participó esa semana).
   - `Días trab.` = días distintos en que participó.
   - `Total hs` = suma de todas las semanas del rango.

3. **Resumen por operario** (todo el rango filtrado):
   - `Camiones` = lavados de tipo Camión en que participó.
   - `Lavados` = total de lavados (todos los circuitos).
   - `Días trab.` = días distintos.
   - `Hs trabajadas` = suma de tiempos totales.
   - `Prom. lavado` = `Hs trabajadas ÷ Lavados`.

4. **Resumen por mes** — igual al anterior, pero agrupado por **mes** (`AAAA-MM`).

### Exportación a Excel

El botón **Exportar a Excel** genera un `.xlsx` (ClosedXML) con **5 hojas**:

| Hoja | Contenido |
|------|-----------|
| Métricas | Las métricas semanales por turno. |
| Horas por operario | La matriz de horas por operario y semana + días + total. |
| Resumen operario | El resumen por operario del rango. |
| Resumen por mes | El resumen por operario y mes. |
| Detalle | Todos los registros con las columnas del Forms original (horas de cada etapa, duraciones, semana, operarios, etc.). |

### Pendiente

La columna de **Asistencia** (presente/ausente por día) requiere un módulo de carga aparte; hoy "Días trabajados" se deriva de los lavados, no de una asistencia real.

---

## Estructura

```
ControlLavados/
├── Models/
│   ├── Lavado.cs          # Entidad principal + columnas calculadas (duraciones, turno, semana…)
│   ├── Catalogos.cs       # Entidades de catálogo: Operario, Patente, Frigorifico
│   ├── Enums.cs           # TipoLavado (circuitos), TipoOperario (Offal/Contrato), estados, turnos
│   └── Catalogo.cs        # Valores fijos (dársenas, sectores) y datos de seed inicial
├── Data/
│   └── AppDbContext.cs    # DbContext de EF Core
├── Services/
│   ├── LavadoService.cs   # Crear, avanzar etapas, editar, disponibilidad
│   ├── CatalogoService.cs # ABM de operarios/patentes/frigoríficos
│   └── ReporteService.cs  # Cálculo de métricas y generación del Excel
├── Components/
│   ├── PanelLavado.razor  # Pantalla reutilizable de lavado (según el circuito)
│   ├── Pages/             # Camiones (/), Hielo, Hiel, Varias, Reportes, Catálogos
│   └── Layout/            # Navegación y layout principal
└── wwwroot/app.css        # Estilos
```

---

## Cómo correrlo (desarrollo)

Requisitos: **.NET 9 SDK** y **SQL Server LocalDB** (incluido con Visual Studio / SQL Server Express).

```bash
dotnet run --project ControlLavados.csproj
```

Luego abrir `http://localhost:5136`. La base `ControlLavados` se crea sola en el primer arranque y se siembra con datos iniciales de operarios, patentes y frigoríficos.

### Catálogos

Se editan desde la UI en **`/catalogos`** (quedan guardados en la base). El seed inicial está en [`Models/Catalogo.cs`](Models/Catalogo.cs).
