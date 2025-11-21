*AUN NO TERMINADO, HAY QUE AÑADIR BCRYPT Y QUITAR COOKIE*
🚀 UltimateProyect - Sistema de Gestión de Almacén
Tu sistema integral de gestión de almacén, ahora más rápido, inteligente y visual que nunca. 

Badge

Badge

Badge

📋 Tabla de Contenidos
📌 Descripción
📦 Requisitos
⚙️ Instalación y Configuración
1. Clonar el repositorio
2. Configurar la Base de Datos
3. Configurar la Cadena de Conexión
4. Restaurar Dependencias
5. Ejecutar la Aplicación
📁 Estructura del Proyecto
🧩 Funcionalidades
🔧 Cómo Contribuir
📄 Licencia
📌 Descripción
UltimateProyect es una aplicación web desarrollada con ASP.NET Core 7.0 y Blazor Server, diseñada para gestionar operaciones de almacén, incluyendo:

✅ Verificación y confirmación de salidas.
✅ Recepción de mercancía (con soporte para números de serie).
✅ Seguimiento de productos con número de serie (NSERIES_SEGUIMIENTO).
✅ Asignación automática de palets.
✅ Registro de logs de picking.
✅ Interfaz amigable para ICP y Clientes.
La aplicación está optimizada para entornos empresariales y utiliza Entity Framework Core para la interacción con Microsoft SQL Server.

📦 Requisitos
Antes de comenzar, asegúrate de tener instalado lo siguiente:

.NET 7.0 SDK (o superior).
Visual Studio 2022.
Microsoft SQL Server 2019 o superior .
Un servidor SQL con permisos para crear bases de datos y ejecutar procedimientos almacenados.
⚙️ Instalación y Configuración
1. Clonar el repositorio
bash


1
2
git clone https://github.com/tu-usuario/UltimateProyect.git
cd UltimateProyect
2. Configurar la Base de Datos
Opción A: Usar un script SQL existente (Recomendado)
Si tienes un archivo .sql con la estructura completa de la base de datos (tablas, vistas, procedimientos almacenados), simplemente ejecútalo en tu instancia de SQL Server.

💡 Sugerencia: Crea una nueva base de datos llamada FCT_OCT_1 y ejecuta el script. 

Opción B: Crear la base de datos desde cero con EF Migrations
Si prefieres que Entity Framework cree la base de datos automáticamente:

Abre el proyecto en Visual Studio.
Abre la Consola del Administrador de Paquetes.
Ejecuta los siguientes comandos:
powershell


1
2
Add-Migration InitialCreate
Update-Database
⚠️ Importante: Esto solo creará las tablas definidas en tus modelos. Si necesitas procedimientos almacenados o vistas específicas, deberás crearlos manualmente o usar un script SQL. 

3. Configurar la Cadena de Conexión
Edita el archivo appsettings.json en la carpeta Server:

json


1
2
3
4
5
⌄
⌄
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=FCT_OCT_1;Integrated Security=true;TrustServerCertificate=True"
  }
}
Opciones comunes:

Server: El nombre de tu servidor SQL (puede ser localhost, 127.0.0.1, o un nombre de red).
Database: El nombre de la base de datos (debe coincidir con la que creaste).
Integrated Security=true: Usa autenticación de Windows. Si usas autenticación SQL, cambia a:
json


1
"Server=localhost;Database=FCT_OCT_1;User ID=tu_usuario;Password=tu_contraseña;"
4. Restaurar Dependencias
En la raíz del proyecto, ejecuta:

bash


1
dotnet restore
Esto instalará todas las dependencias necesarias.

5. Ejecutar la Aplicación
Desde la terminal, en la carpeta Server:

bash


1
dotnet run
O, si usas Visual Studio, presiona F5.

La aplicación se abrirá en tu navegador en http://localhost:5000.

📁 Estructura del Proyecto


1
2
3
4
5
6
7
8
9
10
UltimateProyect/
├── Server/                  # Backend (API + Lógica de negocio)
│   ├── Data/                # Contexto de EF y migraciones
│   ├── Controllers/         # Controladores API
│   └── Program.cs           # Punto de entrada
├── Shared/                  # Modelos compartidos entre cliente y servidor
├── Client/                  # Frontend (Blazor Components)
│   ├── Pages/               # Componentes de página
│   └── wwwroot/             # Assets (CSS, JS, imágenes)
└── README.md                # Este archivo
🧩 Funcionalidades
Dashboard Principal: Vista global para ICP y Clientes.
Gestión de Salidas: Verificar y confirmar pedidos de salida.
Gestión de Recepciones: Registrar nuevas recepciones y asignar palets.
Seguimiento de Números de Serie: Registrar el uso de productos con NSerie.
Logs de Picking: Historial de operaciones realizadas.
Validación de Datos: Validaciones en cliente y servidor para garantizar la integridad de los datos.
