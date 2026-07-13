using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Consola.Cliente;

class Program
{
    private static readonly HttpClient _httpClient = new() { BaseAddress = new Uri("http://localhost:5183") };
    private static int _clienteId = 0;
    private static string _clienteEmail = "";

    static async Task Main(string[] args)
    {
        Console.Title = "Pizzeria - Cliente";
        bool running = true;

        while (running)
        {
            MostrarMenu();
            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1":
                    await RegistrarCliente();
                    break;
                case "2":
                    await BuscarCliente();
                    break;
                case "3":
                    await VerCatalogo();
                    break;
                case "4":
                    await HacerPedido();
                    break;
                case "5":
                    await ConsultarPedido();
                    break;
                case "0":
                    running = false;
                    break;
                default:
                    MostrarError("Opcion no valida.");
                    break;
            }
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\nGracias por usar el Simulador Cliente. Hasta luego!");
        Console.ResetColor();
    }

    static void MostrarMenu()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================");
        Console.WriteLine("        SIMULADOR CLIENTE - PIZZERIA");
        Console.WriteLine("==================================================");
        Console.ResetColor();

        if (_clienteId > 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  Cliente activo: {_clienteEmail} (ID: {_clienteId})");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Sin cliente seleccionado (opciones 1 o 2)");
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.WriteLine("  1. Registrar nuevo cliente");
        Console.WriteLine("  2. Buscar cliente existente (ID o email)");
        Console.WriteLine("  3. Ver catalogo de pizzas");
        Console.WriteLine("  4. Hacer un pedido");
        Console.WriteLine("  5. Consultar estado de un pedido");
        Console.WriteLine("  0. Salir");
        Console.WriteLine();
        Console.Write("  Seleccion: ");
    }

    // --- OPCION 1: Registrar Cliente ---
    static async Task RegistrarCliente()
    {
        Console.WriteLine("\n--- Registrar Nuevo Cliente ---");
        Console.Write("  Nombre: ");
        string nombre = Console.ReadLine()?.Trim() ?? "";
        Console.Write("  Email: ");
        string email = Console.ReadLine()?.Trim() ?? "";
        Console.Write("  Telefono: ");
        string telefono = Console.ReadLine()?.Trim() ?? "";
        Console.Write("  Direccion: ");
        string direccion = Console.ReadLine()?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(telefono) || string.IsNullOrWhiteSpace(direccion))
        {
            MostrarError("Todos los campos son obligatorios.");
            return;
        }

        var cliente = new { nombre, email, telefono, direccion };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/clientes", cliente);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                _clienteId = json.GetProperty("id").GetInt32();
                _clienteEmail = email;
                MostrarExito($"Cliente registrado: {nombre} (ID: {_clienteId}, Email: {email})");
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                MostrarError($"Error {response.StatusCode}: {body}");
            }
        }
        catch (Exception ex)
        {
            MostrarError($"No se pudo conectar a la API: {ex.Message}");
        }
    }

    // --- OPCION 2: Buscar Cliente ---
    static async Task BuscarCliente()
    {
        Console.WriteLine("\n--- Buscar Cliente ---");
        Console.Write("  Buscar por (1) ID o (2) Email? Seleccion: ");
        string tipo = Console.ReadLine()?.Trim() ?? "";

        try
        {
            string url = tipo == "2"
                ? $"/api/clientes/email/{Console.ReadLine()?.Trim()}"
                : $"/api/clientes/{Console.ReadLine()?.Trim()}";

            // Re-leer input despues del tipo
            Console.Write(tipo == "2" ? "  Email: " : "  ID: ");
            string valor = Console.ReadLine()?.Trim() ?? "";
            url = tipo == "2"
                ? $"/api/clientes/email/{valor}"
                : $"/api/clientes/{valor}";

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                _clienteId = json.GetProperty("id").GetInt32();
                string nombre = json.GetProperty("nombre").GetString() ?? "";
                _clienteEmail = json.GetProperty("email").GetString() ?? "";
                MostrarExito($"Cliente encontrado: {nombre} (ID: {_clienteId}, Email: {_clienteEmail})");
            }
            else
            {
                MostrarError("Cliente no encontrado.");
            }
        }
        catch (Exception ex)
        {
            MostrarError($"No se pudo conectar a la API: {ex.Message}");
        }
    }

    // --- OPCION 3: Ver Catalogo ---
    static async Task VerCatalogo()
    {
        Console.WriteLine("\n--- Catalogo de Pizzas ---");

        try
        {
            var response = await _httpClient.GetAsync("/api/pizzas");
            if (!response.IsSuccessStatusCode)
            {
                MostrarError("No se pudo obtener el catalogo.");
                return;
            }

            var pizzas = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
            if (pizzas == null || pizzas.Count == 0)
            {
                MostrarError("El catalogo esta vacio.");
                return;
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("-----------------------------------------------------------------");
            Console.WriteLine($"  {"ID",-4} | {"Nombre",-24} | {"Tamano",-10} | {"Precio",-12}");
            Console.WriteLine("-----------------------------------------------------------------");
            foreach (var p in pizzas)
            {
                Console.WriteLine($"  {p.GetProperty("id").GetInt32(),-4} | {p.GetProperty("nombre").GetString(),-24} | {p.GetProperty("tamanio").GetString(),-10} | ${p.GetProperty("precio").GetDouble(),-11:N2}");
            }
            Console.WriteLine("-----------------------------------------------------------------");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            MostrarError($"No se pudo conectar a la API: {ex.Message}");
        }
    }

    // --- OPCION 4: Hacer Pedido ---
    static async Task HacerPedido()
    {
        if (_clienteId == 0 || string.IsNullOrEmpty(_clienteEmail))
        {
            MostrarError("Primero selecciona un cliente (opcion 1 o 2).");
            return;
        }

        Console.WriteLine("\n--- Hacer Pedido ---");

        // Mostrar catalogo primero
        await VerCatalogo();

        var items = new List<(string nombre, int cantidad)>();

        Console.WriteLine("  Agrega pizzas al pedido (escribe 'fin' en el nombre para terminar):");
        while (true)
        {
            Console.Write($"  Pizza #{items.Count + 1} - Nombre exacto (o 'fin'): ");
            string nombre = Console.ReadLine()?.Trim() ?? "";
            if (nombre.ToLower() == "fin" || nombre == "") break;

            Console.Write($"  Cantidad: ");
            if (!int.TryParse(Console.ReadLine()?.Trim(), out int cantidad) || cantidad <= 0)
            {
                MostrarError("Cantidad invalida. Se usa 1.");
                cantidad = 1;
            }

            items.Add((nombre, cantidad));
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"    + {cantidad}x {nombre}");
            Console.ResetColor();
        }

        if (items.Count == 0)
        {
            MostrarError("No se agregaron items al pedido.");
            return;
        }

        var pedidoRequest = new
        {
            clienteEmail = _clienteEmail,
            items = items.Select(i => new { pizzaNombre = i.nombre, cantidad = i.cantidad }).ToArray()
        };

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n  Enviando pedido ({items.Count} tipos de pizza)...");
        Console.ResetColor();

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/pedidos", pedidoRequest);

            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                var errObj = await response.Content.ReadFromJsonAsync<JsonElement>();
                MostrarError($"[503] Cocina no disponible: {errObj.GetProperty("error").GetString()}");
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                MostrarError($"Error {response.StatusCode}: {body}");
                return;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            int pedidoId = json.GetProperty("pedidoId").GetInt32();
            string estado = json.GetProperty("estado").GetString() ?? "";
            double total = json.GetProperty("total").GetDouble();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  ==================================");
            Console.WriteLine("  PEDIDO CREADO EXITOSAMENTE!");
            Console.WriteLine($"  Pedido ID:  {pedidoId}");
            Console.WriteLine($"  Estado:     {estado}");
            Console.WriteLine($"  Total:      ${total:N2}");
            Console.WriteLine("  ==================================");
            Console.ResetColor();

            // Polling de estado
            Console.WriteLine("\n  Iniciando seguimiento del pedido...");
            await SeguirPedido(pedidoId, estado);
        }
        catch (Exception ex)
        {
            MostrarError($"No se pudo conectar a la API: {ex.Message}");
        }
    }

    // --- OPCION 5: Consultar Pedido ---
    static async Task ConsultarPedido()
    {
        Console.Write("\n  Ingrese el ID del pedido: ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out int pedidoId))
        {
            MostrarError("ID invalido.");
            return;
        }

        try
        {
            var response = await _httpClient.GetAsync($"/api/pedidos/{pedidoId}");
            if (!response.IsSuccessStatusCode)
            {
                MostrarError("Pedido no encontrado.");
                return;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            string estado = json.GetProperty("estado").GetString() ?? "";
            double total = json.GetProperty("total").GetDouble();
            string fecha = json.GetProperty("fechaCreacion").GetString() ?? "";

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n  ┌─────────────────────────────────────┐");
            Console.WriteLine($"  │ Pedido #{pedidoId,-28} │");
            Console.WriteLine($"  │ Estado: {estado,-26} │");
            Console.WriteLine($"  │ Total:  ${total,-25:N2} │");
            Console.WriteLine($"  │ Creado: {fecha,-26} │");
            Console.WriteLine("  └─────────────────────────────────────┘");

            if (json.TryGetProperty("items", out var items))
            {
                Console.WriteLine("  Items:");
                foreach (var item in items.EnumerateArray())
                {
                    string pizza = item.GetProperty("pizza").GetString() ?? "";
                    int cant = item.GetProperty("cantidad").GetInt32();
                    double precio = item.GetProperty("precioUnitario").GetDouble();
                    Console.WriteLine($"    - {cant}x {pizza} (${precio:N2} c/u)");
                }
            }
            Console.ResetColor();

            // Si el pedido esta activo, ofrecer polling
            if (estado != "Entregado" && estado != "Cancelado")
            {
                Console.Write("\n  Seguir este pedido en tiempo real? (s/n): ");
                if (Console.ReadLine()?.Trim().ToLower() == "s")
                {
                    await SeguirPedido(pedidoId, estado);
                }
            }
        }
        catch (Exception ex)
        {
            MostrarError($"No se pudo conectar a la API: {ex.Message}");
        }
    }

    // --- Polling de estado de pedido ---
    static async Task SeguirPedido(int pedidoId, string estadoActual)
    {
        string estado = estadoActual;

        while (estado != "Entregado" && estado != "Cancelado")
        {
            await Task.Delay(2000);

            try
            {
                var pollResponse = await _httpClient.GetAsync($"/api/pedidos/{pedidoId}");
                if (!pollResponse.IsSuccessStatusCode) continue;

                var pollJson = await pollResponse.Content.ReadFromJsonAsync<JsonElement>();
                estado = pollJson.GetProperty("estado").GetString() ?? "";

                Console.ForegroundColor = estado switch
                {
                    "EsperaConfirmacion" => ConsoleColor.Yellow,
                    "EnPreparacion" => ConsoleColor.Blue,
                    "EnViaje" => ConsoleColor.Magenta,
                    "Entregado" => ConsoleColor.Green,
                    "Cancelado" => ConsoleColor.Red,
                    _ => ConsoleColor.White
                };

                Console.WriteLine($"  [{DateTime.Now:HH:mm:ss}] Pedido #{pedidoId} -> {estado}");
                Console.ResetColor();
            }
            catch
            {
                // Silenciar errores de red en polling
            }
        }

        Console.WriteLine();
        if (estado == "Entregado")
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ============================================");
            Console.WriteLine("        PIZZA ENTREGADA! BUEN PROVECHO!");
            Console.WriteLine("  ============================================");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ============================================");
            Console.WriteLine("        EL PEDIDO FUE CANCELADO.");
            Console.WriteLine("  ============================================");
        }
        Console.ResetColor();
    }

    // --- Utilidades ---
    static void MostrarExito(string mensaje)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  [OK] {mensaje}");
        Console.ResetColor();
    }

    static void MostrarError(string mensaje)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  [ERROR] {mensaje}");
        Console.ResetColor();
    }
}
