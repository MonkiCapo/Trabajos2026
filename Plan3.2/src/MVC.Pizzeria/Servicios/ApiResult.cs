namespace MVC.Pizzeria.Servicios;

// Resultado de una llamada a la API: o vienen los datos, o la lista de
// errores ya parseada lista para mostrar (reusa ApiErrorReader).
public class ApiResult<T>
{
    public bool Exito { get; }
    public T? Datos { get; }
    public List<(string Clave, string Mensaje)> Errores { get; }

    // Distingue "la API respondio con un error" de "la API no respondio":
    // con la API caida no hay que tocar datos locales (ej. la lista de Mis
    // Pedidos de la sesion), porque se perderian por una falla transitoria.
    public bool SinConexion { get; }

    // Codigo HTTP de la respuesta fallida (null si la API nunca respondio):
    // sirve para distinguir un 404 real de un 401/500 al decidir si tocar
    // datos locales.
    public int? CodigoEstado { get; }

    private ApiResult(bool exito, T? datos, List<(string Clave, string Mensaje)> errores, bool sinConexion, int? codigoEstado)
    {
        Exito = exito;
        Datos = datos;
        Errores = errores;
        SinConexion = sinConexion;
        CodigoEstado = codigoEstado;
    }

    public static ApiResult<T> Ok(T datos) =>
        new(true, datos, new List<(string Clave, string Mensaje)>(), false, null);

    public static ApiResult<T> Falla(List<(string Clave, string Mensaje)> errores, int? codigoEstado = null) =>
        new(false, default, errores, false, codigoEstado);

    public static ApiResult<T> Falla(string mensaje) =>
        new(false, default, new List<(string Clave, string Mensaje)> { (string.Empty, mensaje) }, false, null);

    public static ApiResult<T> Falla(string mensaje, int? codigoEstado) =>
        new(false, default, new List<(string Clave, string Mensaje)> { (string.Empty, mensaje) }, false, codigoEstado);

    public static ApiResult<T> FallaDeConexion(string mensaje) =>
        new(false, default, new List<(string Clave, string Mensaje)> { (string.Empty, mensaje) }, true, null);
}
