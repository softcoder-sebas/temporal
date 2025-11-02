# 🛒 MyMarket ERP – Sprint 2

**MyMarket ERP** es una solución ERP (Enterprise Resource Planning) desarrollada en **.NET 9 (Windows Forms)** destinada a la administración integral de supermercados y tiendas de conveniencia.  
Centraliza todas las operaciones clave —ventas, inventario, facturación, contabilidad, clientes y recursos humanos— en una única interfaz coherente, modular y con control de acceso por roles.  

Este documento describe **la arquitectura, los módulos, la configuración, las dependencias, la base de datos, las exportaciones y los lineamientos técnicos** implementados en la versión **Sprint 2**, que consolida mejoras visuales, nuevas funcionalidades y optimizaciones de rendimiento.

---

## 📚 Tabla de Contenidos
1. [Resumen del Proyecto](#-resumen-del-proyecto)
2. [Arquitectura General](#-arquitectura-general)
3. [Roles y Permisos](#-roles-y-permisos)
4. [Estructura de Carpetas](#-estructura-de-carpetas)
5. [Módulos Principales](#-módulos-principales)
    - [Panel Central](#panel-central)
    - [Punto de Venta (POSCompras)](#punto-de-venta-poscompras)
    - [Gestión de Clientes](#gestión-de-clientes)
    - [Inventario](#inventario)
    - [Contabilidad](#contabilidad)
    - [Historial de Facturación](#historial-de-facturación)
    - [Administración de Empleados](#administración-de-empleados)
6. [Componentes de Interfaz (UI)](#-componentes-de-interfaz-ui)
7. [Servicios Internos](#-servicios-internos)
8. [Base de Datos](#-base-de-datos)
9. [Seguridad y Control de Acceso](#-seguridad-y-control-de-acceso)
10. [Exportaciones y Reportes](#-exportaciones-y-reportes)
11. [Configuración del Entorno](#-configuración-del-entorno)
12. [Ejecución del Proyecto](#-ejecución-del-proyecto)
13. [Usuarios de Ejemplo](#-usuarios-de-ejemplo)
14. [Buenas Prácticas](#-buenas-prácticas)
15. [Roadmap (Sprint 3 y futuro)](#-roadmap-sprint-3-y-futuro)
16. [Créditos Técnicos](#-créditos-técnicos)

---

## 🧭 Resumen del Proyecto

**MyMarket ERP Sprint 2** busca proporcionar una gestión integral de los procesos operativos y financieros en comercios minoristas.  
Esta versión amplía las funcionalidades del Sprint 1 con:

- Exportaciones nativas a **Excel (ClosedXML)** y **PDF**.  
- Generación automática de **facturas electrónicas con QR**.  
- Sistema de roles extendido y controlado desde la capa de sesión.  
- Rediseño del **panel lateral** y navegación con componentes reutilizables.  
- Integración total de **gráficos analíticos** (ventas, stock, contabilidad).  
- Refactorización de la capa de datos con **Microsoft.Data.SqlClient**.  

El código está organizado modularmente en carpetas según su responsabilidad: `Modules/`, `Components/`, `Reporting/`, `Services/`.

---

## 🏗️ Arquitectura General

El sistema se construye sobre una **arquitectura por capas**:

```
┌────────────────────────────┐
│     Presentación (UI)      │ ← Formularios Windows Forms (.cs / .Designer.cs)
├────────────────────────────┤
│        Lógica (BLL)        │ ← Clases de negocio: validaciones, control, eventos
├────────────────────────────┤
│   Acceso a datos (DAL)     │ ← Database.cs (SQL Server, CRUD, transacciones)
├────────────────────────────┤
│   Servicios de soporte     │ ← Navegación, sesión, reporting, exportaciones
└────────────────────────────┘
```

Cada capa comunica mediante **eventos y servicios**, evitando dependencias circulares.

---

## 👥 Roles y Permisos

Los permisos se definen en `AppSession` y `Permissions.cs`.  
Cada usuario posee un **rol activo** que determina los módulos visibles en la barra lateral y las acciones permitidas dentro de cada formulario.

| Rol | Descripción | Accesos |
|-----|--------------|----------|
| `admin` | Superusuario con acceso total al ERP. | Todos los módulos |
| `contable` | Control contable y generación de reportes financieros. | Contabilidad, Reportes |
| `caja` | Operaciones de punto de venta y facturación. | POS, Historial |
| `inventario` | Control de stock y órdenes de compra. | Inventario |
| `cliente` | Acceso a su propio historial de facturación. | Historial |

> El control de permisos ocurre en tiempo de ejecución mediante el enrutamiento de `NavigationService` y la inyección dinámica de botones en `SidebarControl`.

---

## 🗂️ Estructura de Carpetas

```
MyMarket_ERP_SPRINT_2/
│
├── Modules/
│   ├── Central.cs
│   ├── POSCompras.cs
│   ├── Clientes.cs
│   ├── Inventario.cs
│   ├── Contabilidad.cs
│   ├── Empleados.cs
│   └── Historial_Facturacion.cs
│
├── Components/
│   ├── SidebarControl.cs
│   ├── SidebarButton.cs
│   ├── ModernCard.cs
│   ├── IconGlyphs.cs
│   ├── ModernTheme.cs
│   └── NavigationService.cs
│
├── Reporting/
│   ├── ExcelExport.cs
│   ├── InvoicePdfGenerator.cs
│   ├── InvoiceDocumentGenerator.cs
│   └── SignatureQrGenerator.cs
│
├── Services/
│   ├── AppSession.cs
│   ├── Database.cs
│   ├── Logger.cs
│   └── DataEvents.cs
│
├── Program.cs
└── MyMarket_ERP.csproj
```

---

## 🧩 Módulos Principales

### Panel Central

- Muestra indicadores clave (ventas del día, stock, personal activo).
- Contiene widgets dinámicos con gráficos de barras y pastel.  
- Refresca datos cada intervalo configurable (`Timer` de 30 segundos).
- Permite cerrar sesión y acceder rápidamente a otros módulos.
- Utiliza **ModernCard** para estadísticas visuales.

### Punto de Venta (POSCompras)

- Interfaz completa para registrar compras/ventas.
- Cálculo automático de impuestos (IVA configurable).
- Búsqueda por código o nombre con `AutoCompleteBox`.
- Carrito de compra gestionado con `BindingList<CartItem>`.
- Actualiza existencias en inventario tras finalizar venta.
- Emite facturas con encabezado, detalle, totales y QR.
- Genera recibos **PDF** mediante `InvoicePdfGenerator.cs`.
- Control de métodos de pago (efectivo, tarjeta, transferencia).

### Gestión de Clientes

- CRUD de clientes con validaciones de correo, documento y teléfono.
- Búsqueda en tiempo real y ordenamiento dinámico.
- Eventos sincronizados con otros módulos mediante `DataEvents`.
- Exportación de la lista a Excel (`ExcelExport.Export(DataTable)`).
- Campo de notas adicionales persistente para marketing.

### Inventario

- Catálogo principal de productos y proveedores.
- Control de stock, unidades mínimas y reposiciones automáticas.
- Órdenes de compra con estado (“Pendiente”, “Aprobada”, “Recibida”).
- Filtros por categoría, proveedor, rango de fechas.
- Exportación completa del inventario a Excel.
- Alertas visuales para stock bajo (`DataGridViewRow.BackColor`).

### Contabilidad

- Libro diario y plan contable simplificado (activos, pasivos, ingresos, gastos).
- Asientos automáticos por ventas, compras y devoluciones.
- Filtrado por rango de fechas y tipo de movimiento.
- Exportación de reportes a Excel (`ClosedXML.XLWorkbook`).
- Generación de balances por período y tipo de cuenta.
- Interfaz con `DataGridView` y totales calculados en tiempo real.

### Historial de Facturación

- Tabla maestro-detalle de facturas emitidas.
- Filtros por cliente, estado de pago, método, fecha y texto libre.
- Carga diferida del detalle (`LazyLoad`) para optimizar rendimiento.
- Permite reimprimir facturas en PDF y reenviar por correo.
- Exportación masiva del historial completo en Excel.

### Administración de Empleados

- Gestión de personal: datos personales, contrato, nómina y seguridad social.
- Soporte para CRUD y validación de campos.
- Panel lateral con filtros por estado laboral y departamento.
- Exportación de listados a Excel y PDF.
- Control horario, licencias y marcaciones manuales.

---

## 🎨 Componentes de Interfaz (UI)

El sistema utiliza **componentes personalizados reutilizables**:

| Componente | Descripción |
|-------------|-------------|
| `SidebarControl` | Renderiza el panel lateral con botones según rol. |
| `SidebarButton` | Botón estilizado con icono e identificador de módulo. |
| `ModernCard` | Tarjetas visuales para estadísticas y panel central. |
| `ModernTheme` | Paleta de colores y estilos globales. |
| `IconGlyphs` | Fuente de iconos vectoriales unificada. |
| `NavigationService` | Controlador de navegación entre formularios. |

---

## ⚙️ Servicios Internos

### AppSession
- Gestiona la sesión activa del usuario (ID, correo, rol, permisos).
- Controla la expiración y persistencia de sesión.
- Implementa funciones como `IsInRole(string role)` y `Logout()`.

### Database
- Acceso centralizado a SQL Server mediante `Microsoft.Data.SqlClient`.
- Métodos: `ExecuteQuery()`, `ExecuteNonQuery()`, `ExecuteScalar()`.
- Manejo de transacciones y conexión por variable de entorno `MYMARKET_SQLSERVER_CS`.

### Logger
- Registra errores y eventos del sistema en `Database.log`.
- Modo “Verbose” opcional para diagnóstico avanzado.

### DataEvents
- Sistema interno de eventos que sincroniza formularios entre sí.
- Ejemplo: cuando se agrega un cliente, se actualiza la lista de ventas.

---

## 🗄️ Base de Datos

Tablas principales:

| Tabla | Descripción |
|--------|--------------|
| `Users` | Usuarios del sistema con hash de contraseña y rol. |
| `Customers` | Datos de clientes. |
| `Products` | Catálogo de productos con stock, precio y proveedor. |
| `Invoices` | Facturas emitidas. |
| `InvoiceItems` | Detalle de productos vendidos. |
| `Employees` | Datos del personal. |
| `AccountingEntries` | Movimientos contables. |

> La base se crea automáticamente al primer arranque (`Database.EnsureInitialized()`).  

Relaciones destacadas:
- `Invoices` ↔ `Customers` (N:1)
- `InvoiceItems` ↔ `Products` (N:1)
- `AccountingEntries` ↔ `Invoices` (1:1)

---

## 🔒 Seguridad y Control de Acceso

- **Autenticación:** formulario `Login.cs`, validación contra tabla `Users`.
- **Autorización:** basada en rol (`AppSession.Role`).
- **Encriptación:** contraseñas con SHA-256.
- **Timeout:** sesión expira tras 15 min de inactividad.
- **Logs:** intentos fallidos y accesos en `Database.log`.

---

## 📤 Exportaciones y Reportes

### Exportación a Excel

Usa **ClosedXML (v0.104.0)**:

```csharp
using ClosedXML.Excel;

public static void ExportToExcel(DataTable table, string path)
{
    using var wb = new XLWorkbook();
    var ws = wb.Worksheets.Add(table, "Reporte");
    ws.Columns().AdjustToContents();
    wb.SaveAs(path);
}
```

Exportaciones disponibles:
- Reporte contable completo.
- Listado de clientes y empleados.
- Catálogo de inventario.
- Historial de facturación.

### Exportación a PDF

- Generación de facturas y comprobantes con `InvoicePdfGenerator`.
- Incorporación de QR mediante `SignatureQrGenerator`.
- Compatible con impresoras A4 y térmicas.

### Reportes visuales
- Dashboards con `WinForms.DataVisualization.Charting`.
- Gráficos de barras, líneas y pastel.
- Actualización en vivo cada 60 segundos.

---

## 💻 Configuración del Entorno

1. Instalar **.NET 9 SDK**.  
2. Instalar **SQL Server 2019+**.  
3. Configurar cadena de conexión:
   ```powershell
   $env:MYMARKET_SQLSERVER_CS = "Server=localhost\\SQLEXPRESS;Database=MyMarketERP;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;"
   ```
4. Abrir `MyMarket_ERP_SPRINT_2/MyMarket_ERP.csproj` en Visual Studio 2022.
5. Restaurar dependencias (`dotnet restore`).
6. Compilar (`Ctrl + Shift + B`) y ejecutar (F5).

---

## ▶️ Ejecución del Proyecto

1. Clona el repositorio:
   ```bash
   git clone https://github.com/usuario/MyMarket_ERP_SPRINT_2.git
   ```
2. Restaura dependencias y compila:
   ```bash
   dotnet build --configuration Release
   ```
3. Ejecuta el proyecto:
   ```bash
   dotnet run --project MyMarket_ERP_SPRINT_2/MyMarket_ERP.csproj
   ```
4. Inicia sesión con credenciales de ejemplo (`admin@erp.local`, `Admin123`).

---

## 👤 Usuarios de Ejemplo

| Usuario | Contraseña | Rol |
|----------|-------------|-----|
| admin@erp.local | Admin123 | admin |
| conta@erp.local | 1234 | contable |
| caja@erp.local | 1234 | caja |
| inv@erp.local | 1234 | inventario |
| cli@erp.local | 1234 | cliente |

---

## 🧩 Buenas Prácticas

- Validar campos antes de guardar.  
- Manejar excepciones en capa BLL.  
- No compartir conexiones SQL entre hilos.  
- Usar `using` para liberar recursos.  
- Mantener logs limpios y versionados.  
- Aplicar patrón **MVVM simplificado** dentro de formularios.  

---

## 🚀 Roadmap (Sprint 3 y futuro)

- Exportaciones globales en PDF y CSV.  
- Módulo de proveedores y compras.  
- Sincronización multi-sucursal.  
- Encriptación de variables de entorno.  
- Integración con API DIAN para facturación electrónica real.  
- Pruebas unitarias automatizadas (xUnit).  

---

## 👨‍💻 Créditos Técnicos

**Desarrollado por el equipo de ingeniería MyMarket**  
Basado en .NET 9 y SQL Server  
Incluye soporte de librerías:
- ClosedXML
- Microsoft.Data.SqlClient
- WinForms.DataVisualization  

Versión actual: `Sprint_2 (Build 1.9.24)`

---

> © 2025 MyMarket ERP – Todos los derechos reservados.
