# MyMarket ERP

MyMarket ERP es una aplicación de escritorio desarrollada en **.NET 9 (Windows Forms)** para gestionar operaciones clave de un supermercado o tienda de conveniencia. El sistema centraliza ventas, clientes, inventario, contabilidad y administración de personal mediante un conjunto de módulos integrados que comparten autenticación, navegación lateral contextual y un acceso controlado por roles.

## Tabla de contenidos
- [Roles disponibles](#roles-disponibles)
- [Módulos y funcionalidades](#módulos-y-funcionalidades)
  - [Panel central](#panel-central)
  - [Punto de venta (Compras)](#punto-de-venta-compras)
  - [Gestión de clientes](#gestión-de-clientes)
  - [Inventario](#inventario)
  - [Contabilidad](#contabilidad)
  - [Historial de facturación](#historial-de-facturación)
  - [Administración de empleados](#administración-de-empleados)
- [Requisitos del entorno](#requisitos-del-entorno)
- [Configuración de la base de datos](#configuración-de-la-base-de-datos)
- [Usuarios y datos de ejemplo](#usuarios-y-datos-de-ejemplo)
- [Ejecución del proyecto](#ejecución-del-proyecto)
- [Estructura del repositorio](#estructura-del-repositorio)
- [Tecnologías y paquetes](#tecnologías-y-paquetes)

## Roles disponibles

Los permisos de navegación se definen en `Permissions.cs` y controlan qué módulos aparecen en la barra lateral. Cada rol puede acceder exclusivamente a las secciones listadas a continuación:

| Rol          | Descripción                                                                 | Secciones habilitadas |
|--------------|------------------------------------------------------------------------------|------------------------|
| `admin`      | Superusuario con visibilidad total y capacidades administrativas.            | Panel central, Punto de venta, Clientes, Inventario, Contabilidad, Empleados, Historial |
| `contable`   | Responsable de registrar y auditar movimientos financieros.                  | Panel central, Contabilidad |
| `caja`       | Personal de caja encargado de registrar ventas en el POS.                   | Panel central, Punto de venta |
| `inventario` | Encargado de reponer stock y gestionar órdenes de compra y productos.        | Panel central, Inventario |
| `cliente`    | Usuario final con acceso restringido a su historial personal de compras.     | Historial |

> **Nota:** La lógica de sesión está centralizada en `AppSession`, que almacena el rol, correo y (cuando aplica) el identificador del cliente autenticado para utilizarlo en cada formulario.

## Módulos y funcionalidades

### Panel central

Formulario: `Central`

- Muestra indicadores clave (ventas del día, número de facturas, estado de inventario y personal activo).
- Provee analítica gráfica configurable por período (hoy, últimos 7/30 días, año en curso) utilizando gráficos de barras interactivos.
- Notifica alertas de bajo stock y reacomoda el contenedor para mantener la usabilidad incluso en ventanas redimensionadas.
- Permite cerrar sesión desde cualquier módulo y sincroniza la navegación con el resto del sistema.

### Punto de venta (Compras)

Formulario: `POSCompras`

- Flujo de venta completo con carrito (`BindingList<CartItem>`), cálculo automático de impuestos (IVA configurable) y métodos de pago (efectivo, tarjeta, transferencia).
- Búsqueda rápida por código o nombre con autocompletado y escaneo simulado, edición de cantidades y eliminación de líneas mediante doble clic.
- Registro del cliente asociado a la venta con sugerencias asincrónicas y validación de correo.
- Generación de facturas en la base de datos (cabecera e items), actualización de stock y control de estado del pago.

### Gestión de clientes

Formulario: `Clientes`

- Lista interactiva con ordenamiento, filtros instantáneos y recordatorio de selección entre recargas.
- Formulario para alta, edición y eliminación con validaciones (correo, documento, teléfono) y notificaciones.
- Sincronización en vivo mediante el bus de eventos (`DataEvents`) que actualiza la cuadrícula ante cambios registrados desde otros módulos.

### Inventario

Formulario: `Inventario`

- Panel dual para órdenes de compra y catálogo de productos con paginación, filtros por estado, rango de fechas y categoría.
- Detalle expandible de cada orden, con seguimiento de proveedor, totales y estado.
- Gestión de productos (crear, editar, desactivar) con indicadores visuales para stock crítico y botón rápido de reposición.
- Acciones auxiliares como registro de nuevos proveedores y generación de órdenes de compra de muestra (`SeedDemoOrdenes`).

### Contabilidad

Formulario: `Contabilidad`

- Libro diario en memoria alimentado con un plan de cuentas simplificado (activos, pasivos, ingresos y gastos).
- Generación de asientos contables de ejemplo para ventas, compras, aportes y gastos.
- Filtros por rango de fechas, resumen por cuenta y análisis de saldos calculados según el tipo contable.
- Exportación de la pestaña activa a Excel mediante **ClosedXML** para respaldos o auditoría externa.

### Historial de facturación

Formulario: `Historial_facturacion`

- Visor maestro-detalle de facturas con filtros combinables (cliente, estado de pago, método de pago, rango de fechas y búsqueda por texto).
- Temporizador de búsqueda para evitar consultas excesivas y carga diferida del detalle de cada factura.
- Los administradores pueden consultar cualquier cliente; los usuarios con rol `cliente` solo ven su propio historial.

### Administración de empleados

Formulario: `Empleados`

- Catálogo de personal con filtros por departamento y estado laboral (Activo, Vacaciones, Inactivo).
- Panel de detalle plegable que presenta información ampliada del empleado seleccionado (cargo, salario, fecha de ingreso).
- Soporte para operaciones CRUD con validación, junto con eventos de datos para mantener la lista sincronizada entre formularios.

#### Datos requeridos para registrar un nuevo empleado

Para agilizar el alta de personal en el módulo de RR. HH. y garantizar el cumplimiento normativo, reúne la información siguiente antes de abrir el formulario de creación:

1. **Datos personales**
   - Nombres y apellidos completos.
   - Tipo y número de documento (CC, CE, TI, pasaporte).
   - Fecha y lugar de nacimiento.
   - Estado civil.
   - Dirección completa de residencia, municipio y departamento.
   - Teléfono y correo electrónico personal.
   - Nacionalidad y género.
   - Grupo sanguíneo y RH (opcional, útil para emergencias).
   - Foto o firma digitalizada si se incorpora a la nómina electrónica.

2. **Datos laborales**
   - Código interno de empleado.
   - Fecha de ingreso, cargo y área/dependencia.
   - Tipo de contrato y fecha de terminación (si aplica).
   - Jornada laboral y tipo de salario (integral o no).
   - Centro de costo o sucursal asignada.
   - Supervisor o jefe inmediato.
   - Estado actual (activo, en licencia, retirado, etc.).

3. **Nómina y seguridad social**
   - Salario base y periodicidad de pago.
   - Banco, tipo y número de cuenta.
   - Fondo de pensiones, EPS, ARL y caja de compensación.
   - Fondo de cesantías y tipo de cotizante.
   - Porcentaje/base de cotización y beneficios o deducciones (subsidio transporte, retenciones, etc.).
   - Código DIAN del municipio donde labora.

4. **Formación y experiencia**
   - Nivel educativo, títulos y entidades.
   - Cursos, certificaciones y capacitaciones relevantes.
   - Experiencia laboral previa (empresa, cargo, duración).
   - Idiomas y habilidades destacadas.

5. **Control horario** (si el empleado está sujeto a marcación)
   - Horario asignado y modalidad de registro (biométrico o manual).
   - Historial de ausencias, incapacidades, permisos y vacaciones programadas.
   - Configuración de horas extras y recargos (dominicales, nocturnos, festivos).

6. **Seguridad y salud en el trabajo**
   - Contacto de emergencia (nombre, parentesco, teléfono).
   - Exámenes médicos de ingreso/periódicos y número de afiliación al sistema de seguridad social.
   - Registro de accidentes laborales o incapacidades.
   - Documentos adjuntos: contrato firmado, identificación, certificados y autorizaciones.
   - Consentimiento para tratamiento de datos personales (Ley 1581 de 2012).

7. **Datos del sistema**
   - Usuario, contraseña y roles dentro del ERP.
   - Fecha de creación y bitácora de modificaciones.
   - Observaciones internas del área de RR. HH.

## Requisitos del entorno

Antes de abrir la solución, confirme que su equipo cumple con **todos** los puntos siguientes:

1. **Sistema operativo:** Windows 10 u 11 con arquitectura x64. Verifique su versión ejecutando `winver` o desde *Configuración → Sistema → Acerca de*.
2. **SDK de .NET 9:**
   - Descargue el instalador oficial desde <https://dotnet.microsoft.com/en-us/download/dotnet/9.0> (versión SDK, no solo runtime).
   - Ejecute el instalador con permisos de administrador y finalice el asistente.
   - Abra una ventana de *PowerShell* nueva y confirme la instalación con:

     ```powershell
     dotnet --info
     ```

     Revise que la sección `SDK:` muestre la versión 9.x.x y que el *RID* incluya `win10-x64` o `win11-x64`.
3. **Microsoft SQL Server 2019 o superior:**
   - Puede usar SQL Server Developer (gratuito) o Express. Descargue el instalador desde <https://www.microsoft.com/es-es/sql-server/sql-server-downloads>.
   - Durante la instalación, habilite la instancia predeterminada `SQLEXPRESS` o, si usa otra instancia, anote su nombre porque lo necesitará para la cadena de conexión.
   - Active el protocolo TCP/IP mediante el *SQL Server Configuration Manager* y reinicie el servicio `SQL Server (SQLEXPRESS)`.
4. **SQL Server Management Studio (SSMS) o Azure Data Studio** (opcional pero recomendado) para inspeccionar la base de datos y ejecutar consultas manuales.
5. **IDE o editor:** Visual Studio 2022 (con la carga de trabajo “.NET Desktop Development”) o Visual Studio Code con la extensión *C# Dev Kit*. Asegúrese de que el IDE pueda compilar proyectos de Windows Forms.
6. **Permisos de ejecución:** utilice un usuario de Windows con privilegios para crear variables de entorno y escribir en el directorio del repositorio.

> 💡 *Si utiliza antivirus corporativo, agréguele una excepción a la carpeta del repositorio para evitar bloqueos cuando el proceso genere los archivos binarios.*

## Configuración de la base de datos

Siga el procedimiento completo para garantizar que la aplicación pueda inicializar el esquema al primer arranque:

1. **Crear la base vacía:**
   1. Abra *SQL Server Management Studio* y conéctese a su instancia (por ejemplo, `localhost\SQLEXPRESS`).
   2. En el *Object Explorer*, haga clic derecho en *Databases → New Database...*.
   3. Ingrese `MyMarketERP` como nombre y confirme con *OK*. No es necesario modificar archivos ni collation.
2. **Registrar la cadena de conexión:**
   1. Abra una ventana de *PowerShell* con permisos de administrador.
   2. Ejecute el siguiente comando ajustando el nombre de la instancia, credenciales y parámetros de seguridad según su entorno:

      ```powershell
      $env:MYMARKET_SQLSERVER_CS = "Server=localhost\\SQLEXPRESS;Database=MyMarketERP;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;"
      ```

      - Si usa autenticación SQL Server, reemplace por `User Id=su_usuario;Password=su_clave;`.
      - Para persistir la variable de forma permanente, ejecute:

        ```powershell
        [System.Environment]::SetEnvironmentVariable("MYMARKET_SQLSERVER_CS", "Server=localhost\\SQLEXPRESS;Database=MyMarketERP;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;", "User")
        ```

   3. Cierre y vuelva a abrir el IDE para que tome la nueva variable.
3. **Probar la conexión manualmente:**
   - Desde PowerShell, ejecute (requiere que el comando `sqlcmd` esté instalado, se incluye con SSMS):

     ```powershell
     sqlcmd -S localhost\SQLEXPRESS -d MyMarketERP -Q "SELECT 'Conectado'" -b
     ```

     Debe imprimirse `Conectado`. Si falla, revise firewall, nombre de instancia o credenciales y repita la prueba hasta obtener respuesta satisfactoria.
4. **Inicialización automática:**
   - Al ejecutar la aplicación por primera vez, `Database.EnsureInitialized()` crea tablas (`Users`, `Customers`, `Products`, `Employees`, `Invoices`, `InvoiceItems`, entre otras), llaves foráneas e índices.
   - Esta rutina detecta columnas faltantes y las agrega en caliente. Mantenga la aplicación abierta hasta que aparezca la pantalla de inicio de sesión; cerrar antes de tiempo puede dejar migraciones incompletas.
5. **Verificación en SSMS:**
   - Expanda la base `MyMarketERP → Tables` y confirme que existan al menos las tablas mencionadas. De no crearse, revise el archivo `Database.cs` y los mensajes de la consola de `dotnet run`.

## Usuarios y datos de ejemplo

La inicialización crea usuarios con contraseña hash SHA-256 en minúsculas (ejemplo: `Admin123` → `admin@erp.local`). Credenciales iniciales:

| Correo               | Contraseña | Rol        |
|----------------------|------------|------------|
| `admin@erp.local`    | `Admin123` | `admin`    |
| `conta@erp.local`    | `1234`     | `contable` |
| `caja@erp.local`     | `1234`     | `caja`     |
| `inv@erp.local`      | `1234`     | `inventario` |
| `cli@erp.local`      | `1234`     | `cliente`  |

También se insertan datos mínimos para comenzar a operar:

- Empleados de muestra en diferentes departamentos y estados (`Employees`).
- Catálogo inicial de productos con stock disponible (`Products`).
- Semillas y actualizaciones en tablas de facturación para mantener consistencia referencial (`Invoices`, `InvoiceItems`).

## Ejecución del proyecto

Una vez preparado el entorno, utilice los siguientes pasos sin omitir ninguno:

1. **Clonar o descargar el repositorio:**
   - Si usa Git: `git clone <url-del-repositorio> C:\MyMarketERP`.
   - Si recibe un archivo `.zip`, extraiga el contenido en una carpeta sin espacios en blanco en la ruta (ejemplo: `C:\Proyectos\MyMarketERP`).
2. **Abrir la solución en el IDE:**
   - Inicie Visual Studio como administrador (clic derecho → *Run as administrator*). Esto evita bloqueos al restaurar paquetes.
   - Seleccione *File → Open → Project/Solution* y cargue `MyMarket_ERP_VERSION FINAL\MyMarket_ERP.sln`.
3. **Restaurar paquetes NuGet:**
   - En el *Package Manager Console* ejecute:

     ```powershell
     dotnet restore "MyMarket_ERP_VERSION FINAL/MyMarket_ERP.sln"
     ```

   - Verifique que el comando concluya con `Restore completed in ...`. Si hay advertencias de certificado, confirme que su conexión a internet permite descargar de `nuget.org`.
4. **Compilar la solución:**
   - Desde el mismo terminal o desde *Build → Build Solution (Ctrl+Shift+B)*, ejecute:

     ```powershell
     dotnet build "MyMarket_ERP_VERSION FINAL/MyMarket_ERP.sln" --configuration Release
     ```

   - Se recomienda compilar en `Release` para detectar problemas de optimización. El proceso debe finalizar con `Build succeeded.` y `0 Failed`.
5. **Configurar el proyecto de inicio:**
   - En el *Solution Explorer*, clic derecho sobre `MyMarket_ERP` → *Set as Startup Project*.
   - Abra `MyMarket_ERP.csproj` y confirme que `OutputType` sea `WinExe`. No modifique esta propiedad.
6. **Ejecutar la aplicación:**
   - En *PowerShell* dentro de la carpeta raíz, ejecute:

     ```powershell
     dotnet run --project "MyMarket_ERP_VERSION FINAL/MyMarket_ERP.csproj" --configuration Release
     ```

   - La consola mostrará mensajes como `Iniciando EnsureInitialized...` y `Seed completado`. Espere a que aparezca la ventana de login.
7. **Iniciar sesión:**
   - Introduzca un correo y contraseña de la tabla [Usuarios y datos de ejemplo](#usuarios-y-datos-de-ejemplo).
   - Si el inicio de sesión falla, revise `Database.log` (se crea en la carpeta `bin\Release\net9.0-windows`). Allí encontrará excepciones detalladas.
8. **Prueba de navegación según rol:**
   - Con `admin@erp.local`, verifique que todos los módulos sean visibles.
   - Cierre sesión desde el icono de usuario en la barra superior y vuelva a ingresar como `caja@erp.local` para validar que solo aparezca el módulo POS.
9. **Cerrar la aplicación correctamente:**
   - Use el botón *Salir* del módulo central o `Alt+F4`. Esto asegura que `AppSession` limpie el contexto y que se libere la conexión a SQL Server.

## Estructura del repositorio

```
MyMarket_ERP_VERSION FINAL/
├── MyMarket_ERP.sln                # Solución principal
├── MyMarket_ERP.csproj             # Proyecto Windows Forms (.NET 9)
├── Program.cs / AppContext         # Punto de entrada y ciclo de vida de formularios
├── Database.cs                     # Inicialización de esquema y datos semilla
├── Login.cs / Login.Designer.cs    # Autenticación de usuarios
├── Central*.cs                     # Panel central y componentes
├── POSCompras*.cs                  # Punto de venta y lógica de facturación
├── Clientes*.cs                    # Gestión de clientes
├── Inventario*.cs                  # Órdenes de compra y productos
├── Contabilidad*.cs                # Libro diario y exportaciones
├── Historial_facturacion*.cs       # Historial de facturas y detalle
├── Empleados*.cs                   # Administración de personal
├── Sidebar*.cs / NavigationService # Componentes reutilizables de navegación
└── Resources (.resx)               # Diseños y cadenas de UI
```

## Tecnologías y paquetes

- **Microsoft.Data.SqlClient**: Acceso a SQL Server, ejecución de comandos y lectores asincrónicos.
- **WinForms.DataVisualization**: Renderizado de gráficos para el módulo de analítica en el panel central.
- **ClosedXML**: Exportación de reportes contables a Excel sin requerir interop.
- Patrones de Windows Forms modernos: `BindingSource`, `BindingList`, `ApplicationContext` personalizado, `SidebarInstaller` para compartir navegación y `DataEvents` para sincronización entre formularios.

Con este README tendrás una visión integral de las capacidades del sistema, el rol de cada módulo y la configuración necesaria para desplegarlo en tu entorno.
