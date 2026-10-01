using MVC.Pizzeria.Servicios;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// El carrito y la lista de "mis pedidos" viven en la sesion: el usuario no
// tiene que iniciar sesion, la cookie solo guarda que pizzas eligio, en que
// cantidad y que pedidos confirmo.
//
// Ojo: la cookie de sesion tiene un limite de ~4 KB. Con el catalogo actual
// sobra; si el catalogo creciera mucho, habria que mover el carrito a un
// almacen del lado del servidor.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "MVC.Pizzeria.Sesion";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

    // 7 dias para que "mis pedidos" sobreviva al cierre del navegador: si
    // fuera solo de sesion, el usuario perderia el pedido al cerrar y no
    // tendria como volver a encontrarlo. MaxAge vuelve la cookie persistente.
    options.Cookie.MaxAge = TimeSpan.FromDays(7);
    options.IdleTimeout = TimeSpan.FromDays(7);
});

// AddHttpContextAccessor habilita IHttpContextAccessor, que es la unica forma
// de llegar a la sesion desde un servicio: ISession NO se puede inyectar por
// constructor, ASP.NET Core no lo registra en el contenedor. Solo existe
// HttpContext.Session, que arma el middleware app.UseSession().
builder.Services.AddHttpContextAccessor();

// Scoped porque los stores leen la sesion de la request en curso.
builder.Services.AddScoped<ICarritoStore, CarritoStore>();
builder.Services.AddScoped<IMisPedidosStore, MisPedidosStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// La sesion va entre UseRouting y UseAuthorization: despues de que existe
// la ruta y antes de que se ejecuten los endpoints.
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Urls.Add("http://127.0.0.1:5080");
app.Run();
