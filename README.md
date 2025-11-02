# 🛒 MyMarket ERP – Sprint 2

**MyMarket ERP** es una aplicación de escritorio en **.NET 9 (Windows Forms)** para gestionar operaciones integrales de un supermercado o tienda de conveniencia. Centraliza **ventas (POS)**, **inventario**, **clientes**, **contabilidad**, **facturación** y **recursos humanos**. Este Sprint 2 consolida módulos, mejora la navegación por roles y añade **exportaciones a Excel** y tableros con gráficos.

---

## 📋 Tabla de contenidos
- [Roles disponibles](#roles-disponibles)
- [Módulos y funcionalidades](#módulos-y-funcionalidades)
  - [Panel central](#panel-central)
  - [Punto de venta (Compras)](#punto-de-venta-compras)
  - [Gestión de clientes](#gestión-de-clientes)
  - [Inventario](#inventario)
  - [Contabilidad](#contabilidad)
  - [Historial de facturación](#historial-de-facturación)
  - [Administración de empleados](#administración-de-empleados)
- [Exportaciones y reportes](#exportaciones-y-reportes)
- [Diseño y navegación](#diseño-y-navegación)
- [Requisitos del entorno](#requisitos-del-entorno)
- [Configuración de la base de datos](#configuración-de-la-base-de-datos)
- [Usuarios y datos de ejemplo](#usuarios-y-datos-de-ejemplo)
- [Ejecución del proyecto](#ejecución-del-proyecto)
- [Estructura del repositorio](#estructura-del-repositorio)
- [Tecnologías y paquetes](#tecnologías-y-paquetes)

---

## 👥 Roles disponibles

La autenticación mantiene un **rol activo** por sesión (por ejemplo, `admin`, `contable`, `caja`, `inventario`, `cliente`). La barra lateral muestra solo los módulos permitidos para cada rol.

| Rol          | Descripción                                                      | Módulos habilitados |
|--------------|------------------------------------------------------------------|----------------------|
| `admin`      | Acceso total, configuración, usuarios y auditoría.               | Todos                |
| `contable`   | Libro diario, asientos y reportes financieros.                   | Contabilidad         |
| `caja`       | Flujo de ventas en POS y reimpresión de comprobantes.            | POS, Historial       |
| `inventario` | Altas/bajas de productos, reposición y órdenes de compra.        | Inventario           |
| `cliente`    | Consulta de su propio historial de facturas.                     | Historial            |

> Los permisos determinan dinámicamente qué botones aparecen en la **navegación lateral**.

---

## 🧩 Módulos y funcionalidades

### 🏠 Panel central
- KPIs de negocio (ventas del día, facturas emitidas, stock crítico).
- Gráficos por período (hoy, 7/30 días, anual) con controles de rango.
- Lanzadores rápidos a módulos y cierre de sesión global.

### 💳 Punto de venta (Compras)
- Carrito de compra con edición en línea y cálculo de impuestos.
- Búsqueda por código o nombre con autocompletado.
- Asociación de cliente y múltiples métodos de pago (efectivo, tarjeta, transferencia).
- Generación de factura (cabecera/ítems) y **descuento de stock automático**.

### 👥 Gestión de clientes
- CRUD con validaciones (correo, documento, teléfono).
- Búsqueda y filtros instantáneos; sincronización con otros módulos.
- Exportación del listado a Excel para campañas o auditoría.

### 📦 Inventario
- Catálogo de productos y **órdenes de compra** con filtros por estado/fecha/categoría.
- Indicadores de **stock bajo** y acciones rápidas de reposición.
- Edición masiva de precios y estados de producto.
- Exportación del catálogo/órdenes a Excel para control operativo.

### 📊 Contabilidad
- Libro diario con plan de cuentas simplificado (activos, pasivos, ingresos, gastos).
- Generación de asientos automáticos vinculados a ventas y compras.
- Resúmenes por cuenta, filtros por rango de fechas y **exportación directa a Excel**.

### 🧾 Historial de facturación
- Maestro-detalle de facturas con filtros combinables (cliente, estado, método de pago, fecha).
- Carga diferida del detalle para mejorar rendimiento.
- Reimpresión de comprobantes y exportación del historial a Excel.

### 🧑‍💼 Administración de empleados
- Catálogo de personal con CRUD, filtros por departamento y estado.
- Panel de detalle con datos laborales, bancarios y de seguridad social.
- Exportación de nómina/listados a Excel.

---

## 📤 Exportaciones y reportes

### Exportación a Excel (ClosedXML)
Se utiliza **ClosedXML** para generar archivos `.xlsx` sin requerir Office instalado. Ejemplo mínimo para exportar un `DataTable`:

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

Casos de uso incorporados:
- **Contabilidad**: libro diario y resúmenes por cuenta.
- **Inventario**: catálogo y órdenes de compra.
- **Clientes/Empleados**: listados filtrados.
- **Historial**: exportación de facturas por rango de fechas.

> Los archivos se guardan en una ruta elegida por el usuario (por ejemplo, `Documentos/MyMarketERP/Reportes`) y se sobrescriben previa confirmación.

---

## 🎨 Diseño y navegación
- **Sidebar** modular con botones dinámicos según rol.
- **NavigationService** para ruteo entre formularios.
- Controles visuales modernos (tarjetas, iconos, tipografía consistente).
- Gráficos integrados con **WinForms.DataVisualization** para dashboards.

---

## 🧰 Requisitos del entorno
- **Windows 10/11 (x64)**
- **.NET 9 SDK**
- **Microsoft SQL Server 2019+** (Express/Developer)
- **Visual Studio 2022** con “.NET Desktop Development”

---

## 🗄️ Configuración de la base de datos
1. Crear base `MyMarketERP` en su instancia (p. ej. `localhost\\SQLEXPRESS`).
2. Definir cadena de conexión (variable de entorno recomendada):
   ```powershell
   $env:MYMARKET_SQLSERVER_CS = "Server=localhost\\SQLEXPRESS;Database=MyMarketERP;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;"
   ```
3. Primera ejecución: el esquema se crea automáticamente (tablas de usuarios, productos, facturas, etc.).
4. Verifique la conexión con `sqlcmd` o desde el IDE.

---

## 🧪 Usuarios y datos de ejemplo
- Usuarios iniciales por rol (`admin`, `contable`, `caja`, `inventario`, `cliente`) con contraseñas de prueba.
- Semillas mínimas de **productos**, **empleados** y **facturas** para evaluar el flujo de punta a punta.

> Cambie las credenciales y semillas antes de pasar a producción.

---

## ▶️ Ejecución del proyecto
1. Clonar el repositorio.
2. Abrir `MyMarket_ERP_SPRINT_2/MyMarket_ERP.csproj` en Visual Studio.
3. Restaurar paquetes (`dotnet restore`) y compilar en `Release`.
4. Ejecutar (F5) e iniciar sesión con un usuario de prueba.
5. Validar que la barra lateral muestre los módulos correctos según el rol.

---

## 📂 Estructura del repositorio
```
MyMarket_ERP_SPRINT_2/
├── MyMarket_ERP.csproj
├── Program.cs
├── Database.cs
├── Modules/
│   ├── Clientes.cs
│   ├── Inventario.cs
│   ├── POSCompras.cs
│   ├── Contabilidad.cs
│   ├── Empleados.cs
│   └── Historial_Facturacion.cs
├── Components/
│   ├── SidebarControl.cs
│   ├── SidebarButton.cs
│   ├── NavigationService.cs
│   ├── ModernCard.cs
│   ├── IconGlyphs.cs
│   └── ModernTheme.cs
├── Reporting/
│   ├── ExcelExport.cs
│   └── (otros generadores de informes)
└── obj/ / bin/
```

---

## ⚙️ Tecnologías y paquetes
- **Target Framework**: `net9.0-windows`
- **Acceso a datos**: `Microsoft.Data.SqlClient`
- **Reportes Excel**: `ClosedXML`
- **Gráficos**: `WinForms.DataVisualization`
- **Gestión de paquetes**: `PackageReference` en el `.csproj`

---

## 🧭 Buenas prácticas y lineamientos
- Validaciones en formularios antes de persistir datos.
- Manejo de excepciones con mensajes al usuario y logs internos.
- Paginación y *debounce* en búsquedas para mejorar rendimiento.
- Control de transacciones al confirmar ventas y movimientos de inventario.
- Separación de responsabilidades (formularios, servicios, utilidades y reporting).

---

## 🗺️ Roadmap sugerido (Sprint 3)
- Exportación de reportes a **CSV** y **PDF** desde todos los módulos.
- Soporte para **multi-sucursal** y transferencias de inventario.
- Encriptación de cadenas de conexión y roles desde base de datos.
- Tests automatizados de integración para POS e Inventario.
