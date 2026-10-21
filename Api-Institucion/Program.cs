using Api_Institucion.Services;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// 1. Agregar soporte para Controladores
builder.Services.AddControllers();

// 2. Registrar el Servicio en Memoria como Singleton (para mantener el estado de las colecciones sin BD)
builder.Services.AddSingleton<IInstitucionService, InstitucionService>();

// 3. Configuración de OpenAPI y Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Institución - Gestión de Cursos e Inscripciones",
        Version = "v1",
        Description = "API REST desarrollada en C# / ASP.NET Core con almacenamiento en memoria para la cátedra de Desarrollo de Sistemas. Permite gestionar Cursos, Inscripciones, Estudiantes y Docentes respetando el diagrama de clases UML."
    });
});

var app = builder.Build();

// 4. Configuración del pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Swagger y Swagger UI habilitados para pruebas interactivas y Postman
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Institución v1");
    c.RoutePrefix = "swagger";
});

// Redireccionar raíz a la documentación de Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseHttpsRedirection();
app.UseAuthorization();

// 5. Mapeo de Controladores REST
app.MapControllers();

app.Run();
