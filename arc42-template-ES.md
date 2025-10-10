# Introducción y Metas {#section-introduction-and-goals}

## Vista de Requerimientos {#_vista_de_requerimientos}

MyMarket ERP es una aplicación de escritorio Windows Forms que unifica la operación diaria de un supermercado: ventas, clientes, inventario, contabilidad, empleados y consulta de historial.【F:README.md†L1-L96】 El sistema se organiza en formularios especializados que comparten autenticación centralizada, navegación lateral y permisos por rol.【F:README.md†L22-L96】【F:NavigationService.cs†L9-L64】

Principales requerimientos funcionales identificados:

- Autenticación con correo y contraseña para todos los usuarios y apertura del tablero principal según el rol autenticado.【F:Login.cs†L68-L189】【F:AppSession.cs†L3-L24】
- Panel central con indicadores de ventas, analítica temporal y alertas de inventario/recursos humanos.【F:Central.cs†L31-L198】
- Punto de venta que permita crear facturas, manejar carrito de productos, validar stock, registrar el método de pago y descontar inventario.【F:POSCompras.cs†L15-L90】【F:POSCompras.cs†L392-L487】
- Gestión completa de clientes (alta, edición, eliminación, filtros y búsqueda) con actualización en vivo entre formularios.【F:Clientes.cs†L25-L188】【F:DataEvents.cs†L10-L223】
- Administración de inventario y órdenes de compra con filtros, paginación, panel de detalle y mantenimiento de productos críticos.【F:Inventario.cs†L26-L157】
- Contabilidad básica: libro diario, balances, generación de asientos de ejemplo y exportación a Excel.【F:Contabilidad.cs†L15-L146】
- Consulta de historial de facturación con filtros avanzados y detalle maestro–detalle, respetando el alcance de cada rol.【F:Historial_facturacion.cs†L20-L159】
- Gestión de empleados con filtros por departamento/estado y panel de detalle sincronizado mediante eventos de datos.【F:Empleados.cs†L24-L156】

## Metas de Calidad {#_metas_de_calidad}

| Meta | Descripción | Soporte en la implementación |
|------|-------------|------------------------------|
| Seguridad de acceso | Validación de credenciales, contraseñas hash SHA-256 y autorización por rol antes de abrir cada módulo. | `Login` calcula hashes y compara contra la base de datos usando `PasswordHasher`, mientras que `Permissions` y `NavigationService` bloquean el acceso no autorizado.【F:Login.cs†L110-L189】【F:PasswordHasher.cs†L1-L18】【F:Permissions.cs†L6-L44】【F:NavigationService.cs†L9-L33】 |
| Experiencia consistente | Los formularios comparten instalación de barra lateral, estilos modernos y navegación homogénea. | `SidebarInstaller` embebe el formulario en un contenedor con barra lateral y sincroniza el ancho al colapsar/expandir; los módulos la reutilizan al inicializarse.【F:SidebarInstaller.cs†L8-L123】【F:Clientes.cs†L29-L61】【F:Inventario.cs†L26-L82】【F:Contabilidad.cs†L33-L49】 |
| Sincronización de datos | Cambios en clientes, empleados, inventario, facturación, etc. se propagan automáticamente a otras vistas. | El bus `DataEvents` desacopla productores y consumidores con suscripciones débilmente referenciadas y publicaciones con debounce para evitar tormentas de eventos.【F:DataEvents.cs†L10-L223】 |
| Mantenibilidad modular | Cada área de negocio encapsula su lógica en formularios dedicados y modelos propios, apoyados por servicios reutilizables. | Componentes como `Database.EnsureInitialized`, `AppSession`, `NavigationService` y `SidebarControl` centralizan responsabilidades transversales y reducen duplicación.【F:Database.cs†L7-L232】【F:AppSession.cs†L3-L24】【F:NavigationService.cs†L9-L64】【F:SidebarControl.cs†L16-L157】 |

## Partes interesadas (Stakeholders) {#_partes_interesadas_stakeholders}

+--------------------------+----------------------------+-------------------------------------------------------------+
| Rol/Nombre               | Contacto                   | Expectativas                                                |
+==========================+============================+=============================================================+
| Dirección / Admin        | Equipo interno             | Visibilidad integral de ventas, inventario y RR.HH.         |
+--------------------------+----------------------------+-------------------------------------------------------------+
| Contabilidad             | Contador corporativo       | Registrar asientos y exportar reportes contables.          |
+--------------------------+----------------------------+-------------------------------------------------------------+
| Caja                     | Supervisión de cajas       | Registrar ventas ágiles y confiables en el POS.            |
+--------------------------+----------------------------+-------------------------------------------------------------+
| Inventario               | Coordinador de bodega      | Controlar stock, órdenes de compra y productos críticos.    |
+--------------------------+----------------------------+-------------------------------------------------------------+
| Clientes finales         | Área de servicio al cliente| Consultar historial personal de facturación.               |
+--------------------------+----------------------------+-------------------------------------------------------------+
| Equipo TI                | Mesa de soporte            | Operar en Windows/.NET 9, conectar a SQL Server y mantener el despliegue.【F:README.md†L98-L158】 |
+--------------------------+----------------------------+-------------------------------------------------------------+

# Restricciones de la Arquitectura {#section-architecture-constraints}

- Plataforma: aplicación Windows Forms sobre .NET 9, restringida a sistemas operativos Windows x64.【F:MyMarket_ERP.csproj†L1-L13】【F:README.md†L98-L120】
- Base de datos corporativa: Microsoft SQL Server 2019+ accesible mediante cadena de conexión `MYMARKET_SQLSERVER_CS` y cliente `Microsoft.Data.SqlClient`.【F:Database.cs†L11-L38】【F:README.md†L113-L158】
- Despliegue monolítico: una única aplicación cliente que ejecuta consultas SQL directas y gestiona la inicialización del esquema en tiempo de arranque.【F:Program.cs†L12-L51】【F:Database.cs†L22-L232】
- Dependencias permitidas: `WinForms.DataVisualization` para analítica y `ClosedXML` para exportaciones contables.【F:MyMarket_ERP.csproj†L9-L13】

# Alcance y Contexto del Sistema {#section-context-and-scope}

## Contexto de Negocio {#_contexto_de_negocio}

| Actor externo | Interacción con el sistema |
|---------------|---------------------------|
| Administrador de tienda | Controla métricas operativas, empleados y módulos maestros mediante el panel central.【F:README.md†L36-L96】 |
| Personal de caja | Registra compras, medios de pago y clientes vinculados desde el módulo POS.【F:README.md†L47-L55】 |
| Inventario | Administra órdenes y productos, coordinando reposiciones con proveedores registrados externamente.【F:README.md†L64-L72】【F:Inventario.cs†L46-L157】 |
| Contabilidad | Consume datos de ventas y compras para elaborar estados financieros y exportarlos a Excel.【F:README.md†L73-L80】【F:Contabilidad.cs†L33-L146】 |
| Clientes finales | Consultan su historial de facturación y detalles de compra según permisos del rol `cliente`.【F:README.md†L82-L88】【F:Historial_facturacion.cs†L20-L159】 |

## Contexto Técnico {#_contexto_t_cnico}

| Elemento | Descripción |
|----------|-------------|
| Cliente WinForms | Ejecutable `MyMarket_ERP` que levanta `Database.EnsureInitialized`, crea el contexto de aplicación y renderiza formularios conforme al rol autenticado.【F:Program.cs†L12-L51】【F:Login.cs†L15-L189】 |
| Servidor SQL Server | Base `MyMarketERP` con tablas `Users`, `Customers`, `Products`, `Invoices`, `InvoiceItems`, `Employees` y relaciones que el cliente crea y actualiza al iniciar.【F:Database.cs†L43-L229】 |
| Integraciones futuras | Actualmente no hay APIs externas; el bus interno `DataEvents` actúa como mecanismo de integración entre formularios dentro del cliente.【F:DataEvents.cs†L10-L223】 |

# Estrategia de solución {#section-solution-strategy}

- **Inicialización automática de datos**: el cliente asegura el esquema y datos semilla en SQL Server antes de mostrar cualquier pantalla, evitando dependencias manuales de despliegue.【F:Program.cs†L12-L21】【F:Database.cs†L22-L232】
- **Shell reutilizable con barra lateral**: `SidebarInstaller` y `SidebarControl` encapsulan la navegación, manteniendo experiencia consistente y gestión de permisos por rol.【F:SidebarInstaller.cs†L8-L123】【F:SidebarControl.cs†L16-L157】【F:Permissions.cs†L6-L44】
- **Servicios compartidos ligeros**: `AppSession` centraliza el estado de la sesión y `NavigationService` abre formularios controlando permisos y geometría.【F:AppSession.cs†L3-L24】【F:NavigationService.cs†L9-L64】
- **Sincronización desacoplada**: `DataEvents` proporciona publicación/suscripción con referencias débiles y debounce para notificar cambios de datos sin bloquear la UI.【F:DataEvents.cs†L10-L223】
- **Procesos críticos con transacciones**: el POS encapsula la facturación en una transacción SQL, garantizando consistencia entre cabecera, detalle y stock.【F:POSCompras.cs†L392-L477】

# Vista de Bloques {#section-building-block-view}

## Sistema General de Caja Blanca {#_sistema_general_de_caja_blanca}

El sistema se compone de un ejecutable WinForms que orquesta formularios especializados apoyados por servicios transversales:

- **Bootstrap**: `Program` crea el `AppContext`, inicializa la base y mantiene viva la aplicación mientras existan formularios.【F:Program.cs†L12-L51】
- **Persistencia**: `Database` abre conexiones SQL, ejecuta comandos de creación/seed y expone utilidades de acceso directo.【F:Database.cs†L11-L232】
- **Sesión y seguridad**: `Login`, `PasswordHasher`, `AppSession`, `Permissions` y `NavigationService` autentican y autorizan el acceso a módulos.【F:Login.cs†L68-L189】【F:PasswordHasher.cs†L1-L18】【F:AppSession.cs†L3-L24】【F:Permissions.cs†L6-L44】【F:NavigationService.cs†L9-L64】
- **Shell UI**: `SidebarInstaller`, `SidebarControl` y botones asociados proveen navegación consistente y persistencia del estado colapsado.【F:SidebarInstaller.cs†L8-L123】【F:SidebarControl.cs†L16-L157】
- **Bus de eventos**: `DataEvents` propaga cambios entre formularios sin acoplamiento fuerte.【F:DataEvents.cs†L10-L223】
- **Módulos de dominio**: formularios `Central`, `POSCompras`, `Clientes`, `Inventario`, `Contabilidad`, `Historial_facturacion` y `Empleados` encapsulan la lógica de cada área.【F:Central.cs†L31-L198】【F:POSCompras.cs†L15-L90】【F:Clientes.cs†L25-L188】【F:Inventario.cs†L26-L157】【F:Contabilidad.cs†L33-L146】【F:Historial_facturacion.cs†L20-L159】【F:Empleados.cs†L24-L156】

### Bootstrap de la aplicación {#__bootstrap_de_la_aplicacin}

- **Propósito**: garantizar que el esquema de datos existe y arrancar el ciclo de formularios controlado por `AppContext`.
- **Interfaces**: invoca a `Database.EnsureInitialized`, abre el formulario de `Login` y detecta el cierre de la última ventana para finalizar el hilo de UI.【F:Program.cs†L12-L51】

### Servicios de sesión y navegación {#__servicios_de_sesin_y_navegacin}

- **Propósito**: autenticar, almacenar el usuario activo y abrir módulos permitidos controlando tamaño/posición de ventanas.
- **Interfaces**: `Login` interactúa con `Database` para validar credenciales, `AppSession` guarda el estado y `NavigationService` instancia formularios según `NavSection` si el rol lo permite.【F:Login.cs†L110-L189】【F:AppSession.cs†L3-L24】【F:NavigationService.cs†L9-L64】

### Shell reutilizable {#__shell_reutilizable}

- **Propósito**: dotar a todos los formularios de una barra lateral consistente con botones habilitados según permisos.
- **Interfaces**: `SidebarInstaller.Install` reacomoda los controles del formulario en un `SplitContainer`, mientras `SidebarControl` construye botones y publica eventos `SectionClicked` y `SidebarWidthChanged` consumidos por `NavigationService`.【F:SidebarInstaller.cs†L8-L123】【F:SidebarControl.cs†L16-L157】

### Bus de eventos de datos {#__bus_de_eventos_de_datos}

- **Propósito**: notificar cambios (clientes, empleados, inventario, facturación) a otros formularios sin referencias directas.
- **Interfaces**: métodos `Subscribe*` reciben un `Control` y un `Action`; `Publish*` dispara eventos con `DataEventPayload` tras un debounce configurable.【F:DataEvents.cs†L10-L223】

### Módulos funcionales {#__mdulos_funcionales}

- **Central**: consulta métricas clave e inicializa gráficos y alertas de stock, apoyándose en consultas agregadas a `Invoices`, `Products` y `Employees`.【F:Central.cs†L31-L198】
- **POSCompras**: mantiene un carrito en memoria, calcula impuestos, resuelve cliente asociado, inserta cabecera/detalle de factura y actualiza inventario dentro de una transacción SQL.【F:POSCompras.cs†L19-L110】【F:POSCompras.cs†L392-L487】
- **Clientes**: lista, filtra y edita clientes con recargas asincrónicas y manejo de selección persistente, escuchando eventos `DataEvents`.【F:Clientes.cs†L25-L188】
- **Inventario**: combina grid de órdenes con panel de detalle y catálogo de productos paginado, permitiendo acciones sobre proveedores y productos críticos.【F:Inventario.cs†L46-L157】
- **Contabilidad**: arma el libro diario en `DataTable`, calcula balances/ER y exporta pestañas activas a Excel mediante `ClosedXML`.【F:Contabilidad.cs†L15-L146】
- **Historial de facturación**: ofrece filtros combinables, carga diferida y control de acceso diferenciado para admin vs cliente final.【F:Historial_facturacion.cs†L20-L159】
- **Empleados**: administra personal con filtros dinámicos y panel plegable, refrescando datos ante eventos `EmpleadosChanged`.【F:Empleados.cs†L24-L156】

## Nivel 2 {#_nivel_2}

Los módulos se apoyan en modelos simples (clases `Customer`, `Employee`, `CartItem`, etc.) declarados en los mismos archivos para minimizar dependencias. La lógica de acceso a datos se concentra en cada formulario, usando `Database.OpenConnection` y comandos parametrizados.【F:POSCompras.cs†L392-L467】【F:Clientes.cs†L117-L141】【F:Empleados.cs†L131-L156】

## Nivel 3 {#_nivel_3}

En este proyecto WinForms, los subcomponentes de nivel 3 corresponden a controles reutilizables (`SidebarButton`, `ModernCard`, `ModernTheme`) que encapsulan comportamiento visual. Su integración se realiza desde el diseñador y no introduce dependencias lógicas adicionales.

# Vista de Ejecución {#section-runtime-view}

## Escenario de ejecución 1: Inicio de sesión {#__escenario_de_ejecuci_n_1}

1. `Program.Main` invoca `Database.EnsureInitialized()` y abre el formulario `Login` dentro de `AppContext` para mantener la aplicación viva.【F:Program.cs†L12-L51】
2. El usuario ingresa credenciales; `Login` valida formato, consulta `dbo.Users`, calcula el hash SHA-256 y compara contra el almacenado.【F:Login.cs†L68-L149】【F:PasswordHasher.cs†L1-L18】
3. Si el rol corresponde a `cliente`, se asocia un `CustomerId`; en cualquier caso se llama a `AppSession.StartSession` y se navega al formulario permitido usando `NavigationService`.【F:Login.cs†L151-L189】【F:AppSession.cs†L3-L16】【F:NavigationService.cs†L9-L33】

## Escenario de ejecución 2: Venta en el POS {#__escenario_de_ejecuci_n_2}

1. `POSCompras` carga el catálogo de productos activos y configura autocompletado y métodos de pago al mostrarse.【F:POSCompras.cs†L19-L109】
2. Al cobrar, valida que haya productos y calcula subtotal/IVA/total. Resuelve el cliente por correo, documento o nombre.【F:POSCompras.cs†L393-L426】
3. Dentro de una transacción SQL, inserta la factura, detalla cada ítem y descuenta stock asegurando disponibilidad; en caso de error revierte y notifica al usuario.【F:POSCompras.cs†L427-L475】
4. Tras commit, recarga la caché de productos, limpia el carrito y publica eventos de facturación (mediante `DataEvents`) si se requiere, permitiendo que otros módulos reaccionen.【F:POSCompras.cs†L477-L487】【F:DataEvents.cs†L54-L109】

## Escenario de ejecución 3: Actualización de clientes {#__escenario_de_ejecuci_n_3}

1. `Clientes` se suscribe a `DataEvents.SubscribeClientes` al inicializarse y solicita la carga inicial asincrónica.【F:Clientes.cs†L25-L115】
2. Cuando otro formulario publica un evento `ClientesChanged`, se dispara una recarga cancelable que ejecuta una consulta `SELECT` ordenada para poblar la grilla.【F:Clientes.cs†L58-L141】【F:DataEvents.cs†L74-L109】
3. Tras obtener los datos, reaplica filtros, mantiene la selección anterior y actualiza indicadores de estado en la UI.【F:Clientes.cs†L168-L188】

# Vista de Despliegue {#section-deployment-view}

## Nivel de infraestructura 1 {#_nivel_de_infraestructura_1}

La solución se despliega como un cliente pesado en estaciones Windows 10/11 x64 con .NET 9 instalado. Cada estación se conecta a una instancia de SQL Server (local o en red corporativa) usando autenticación Windows o SQL Server configurada en la variable `MYMARKET_SQLSERVER_CS`. `Database.EnsureInitialized` se ejecuta en el primer arranque para crear tablas e índices, por lo que el servidor debe permitir ejecución de scripts DDL desde la aplicación cliente.【F:README.md†L98-L158】【F:Database.cs†L11-L232】

Mapeo de bloques a infraestructura:

- `MyMarket_ERP.exe` (WinForms) se ejecuta en el puesto de trabajo y contiene todos los módulos funcionales.
- `MyMarketERP` (SQL Server) aloja datos persistentes de usuarios, productos, facturas, clientes y empleados.【F:Database.cs†L43-L229】

## Nivel de Infraestructura 2 {#_nivel_de_infraestructura_2}

No se contemplan nodos adicionales en esta iteración; la comunicación se limita al cliente y la base de datos corporativa. Integraciones con sistemas externos (facturación electrónica, proveedores) quedarían para iteraciones futuras.

# Conceptos Transversales (Cross-cutting) {#section-concepts}

## Seguridad y control de acceso {#__seguridad_y_control_de_acceso}

Las contraseñas se almacenan como hashes SHA-256 en la tabla `Users`; el login valida formato y credenciales antes de abrir cualquier módulo. `Permissions` define un mapa de roles a secciones, aplicado por `NavigationService` y materializado en la barra lateral que solo muestra accesos permitidos.【F:Login.cs†L68-L189】【F:PasswordHasher.cs†L1-L18】【F:Permissions.cs†L6-L44】【F:NavigationService.cs†L9-L33】

## Sincronización de datos y consistencia {#__sincronizacin_de_datos_y_consistencia}

`DataEvents` gestiona suscripciones por control, elimina referencias muertas y aplica debounce para evitar tormentas de eventos, permitiendo que módulos como Clientes y Empleados reaccionen a cambios sin bloquear la UI.【F:DataEvents.cs†L27-L223】【F:Clientes.cs†L58-L115】【F:Empleados.cs†L59-L129】 Los procesos críticos (facturación) usan transacciones para mantener integridad referencial entre facturas, detalle e inventario.【F:POSCompras.cs†L392-L477】

## Experiencia de usuario y shell común {#__experiencia_de_usuario_y_shell_comn}

`SidebarInstaller` reestructura cada formulario para insertarlo en un `SplitContainer` con barra lateral, ajustando automáticamente el ancho y forzando `PerformLayout` tras cada cambio. `SidebarControl` guarda un estado global de colapso, muestra tooltips cuando está contraída y emite eventos de navegación reutilizados por todos los módulos.【F:SidebarInstaller.cs†L8-L123】【F:SidebarControl.cs†L16-L157】

# Decisiones de Diseño {#section-design-decisions}

- Adoptar un cliente WinForms monolítico para acelerar la entrega del Sprint 1, reutilizando controles visuales y minimizando infraestructura adicional.【F:Program.cs†L12-L51】【F:SidebarInstaller.cs†L8-L123】
- Gestionar la base con scripts SQL embebidos en código para evitar dependencias de herramientas externas y permitir despliegues “click-once”.【F:Database.cs†L22-L229】
- Centralizar la navegación lateral y los permisos en componentes reutilizables (`SidebarControl`, `Permissions`, `NavigationService`) que simplifican la adición de nuevos módulos.【F:SidebarControl.cs†L16-L157】【F:Permissions.cs†L6-L44】【F:NavigationService.cs†L9-L33】
- Utilizar un bus de eventos in-process (`DataEvents`) con referencias débiles para evitar fugas de memoria en formularios que se abren y cierran repetidamente.【F:DataEvents.cs†L27-L223】

# Requerimientos de Calidad {#section-quality-scenarios}

## Árbol de Calidad {#__rbol_de_calidad}

- **Seguridad**
  - Confidencialidad de credenciales (hash SHA-256, sesiones por rol).【F:Login.cs†L110-L189】【F:PasswordHasher.cs†L1-L18】
  - Control de acceso por rol (navegación condicionada).【F:Permissions.cs†L6-L44】
- **Usabilidad**
  - Navegación uniforme mediante barra lateral y atajos (Enter para login, autocompletado en POS).【F:Login.cs†L19-L67】【F:POSCompras.cs†L19-L110】【F:SidebarInstaller.cs†L8-L123】
  - Visualizaciones y filtros en dashboards y listados.【F:Central.cs†L31-L198】【F:Clientes.cs†L40-L188】
- **Confiabilidad**
  - Transacciones ACID en facturación, validaciones de stock y manejo de excepciones.【F:POSCompras.cs†L392-L492】
  - Debounce de eventos para evitar cargas simultáneas innecesarias.【F:DataEvents.cs†L54-L137】
- **Mantenibilidad**
  - Componentes transversales reutilizables y módulos aislados por dominio.【F:SidebarInstaller.cs†L8-L123】【F:Clientes.cs†L25-L188】【F:Inventario.cs†L26-L157】

## Escenarios de calidad {#_escenarios_de_calidad}

1. **Seguridad**: si un usuario intenta iniciar sesión con credenciales inválidas, `Login` bloquea el acceso, limpia el campo de contraseña y mantiene la sesión cerrada, protegiendo la confidencialidad.【F:Login.cs†L132-L149】
2. **Disponibilidad de datos**: al registrar una venta con stock insuficiente, el POS revierte la transacción y notifica el error, evitando inconsistencia entre inventario y facturación.【F:POSCompras.cs†L447-L472】
3. **Mantenibilidad**: al agregar un nuevo módulo, basta con definir la sección en `NavSection`, mapearla en `Permissions` y crear el formulario que use `SidebarInstaller`, replicando el patrón existente sin tocar otros formularios.【F:NavSection.cs†L3-L12】【F:Permissions.cs†L6-L44】【F:SidebarInstaller.cs†L8-L123】

# Riesgos y deuda técnica {#section-technical-risks}

- **Bloqueo de UI por operaciones sincrónicas**: varias consultas se ejecutan en el hilo principal (ej. POS, inventario). Aunque algunos formularios usan `Task.Run`, se recomienda migrar a operaciones asincrónicas en toda la app para mejorar la respuesta.【F:POSCompras.cs†L392-L487】【F:Inventario.cs†L46-L157】
- **Scripts SQL embebidos**: cambios de esquema requieren recompilar el ejecutable; considerar migraciones versionadas para ambientes mayores.【F:Database.cs†L43-L229】
- **Ausencia de pruebas automatizadas**: el proyecto actual carece de tests unitarios/integración, por lo que validaciones dependen de pruebas manuales descritas en el README.【F:README.md†L179-L208】

# Glosario {#section-glossary}

+-----------------------+---------------------------------------------------------------+
| Término               | Definición                                                    |
+=======================+===============================================================+
| POS                   | Punto de venta utilizado por el personal de caja para facturar compras.【F:POSCompras.cs†L15-L110】 |
+-----------------------+---------------------------------------------------------------+
| Sidebar               | Barra lateral de navegación que lista módulos habilitados según el rol del usuario.【F:SidebarControl.cs†L16-L157】 |
+-----------------------+---------------------------------------------------------------+
| DataEvents            | Bus interno de publicación/suscripción que sincroniza formularios tras cambios de datos.【F:DataEvents.cs†L10-L223】 |
+-----------------------+---------------------------------------------------------------+
| Libro diario          | Tabla contable que registra movimientos de debe/haber y alimenta balances.【F:Contabilidad.cs†L15-L146】 |
+-----------------------+---------------------------------------------------------------+
| Seed                  | Conjunto de datos semilla insertados automáticamente en SQL Server durante la inicialización.【F:Database.cs†L186-L229】 |
+-----------------------+---------------------------------------------------------------+
