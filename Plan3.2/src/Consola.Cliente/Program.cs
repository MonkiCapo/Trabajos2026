using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Consola.Cliente;

class Program
{
    private static readonly HttpClient _httpClient = new() { BaseAddress = new Uri("http://localhost:5000") };

    static async Task Main(string[] args)
    {
        Console.Title = "Pizzería - Cliente";
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================");
        Console.WriteLine("        SIMULADOR CLIENTE - PIZZERÍA");
        Console.WriteLine("==================================================");
        Console.ResetColor();

        try
        {
            // 1. Registrar Cliente
            Console.WriteLine("\n[1] Registrando cliente...");
            var clientData = new
            {
                nombre = "Juan Pérez",
                telefono = "11-1234-5678",
                direccion = "Av. Siempreviva 742"
            };

            var clientResponse = await _httpClient.PostAsJsonAsync("/api/clientes", clientData);
            if (!clientResponse.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] No se pudo registrar el cliente. Status: {clientResponse.StatusCode}");
                return;
            }

            var clientJson = await clientResponse.Content.ReadFromJsonAsync<JsonElement>();
            int clienteId = clientJson.GetProperty("id").GetInt32();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[EXITO] Cliente registrado con ID: {clienteId}");
            Console.ResetColor();

            // 2. Mostrar Catálogo de Pizzas
            Console.WriteLine("\n[2] Consultando catálogo de pizzas...");
            var pizzasResponse = await _httpClient.GetAsync("/api/pizzas");
            if (pizzasResponse.IsSuccessStatusCode)
            {
                var pizzas = await pizzasResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
                Console.WriteLine("-----------------------------------------------------------------");
                Console.WriteLine($"{"ID",-4} | {"Nombre",-22} | {"Tamaño",-8} | {"Precio",-10}");
                Console.WriteLine("-----------------------------------------------------------------");
                foreach (var pizza in pizzas!)
                {
                    Console.WriteLine($"{pizza.GetProperty("id").GetInt32(),-4} | {pizza.GetProperty("nombre").GetString(),-22} | {pizza.GetProperty("tamanio").GetString(),-8} | ${pizza.GetProperty("precio").GetDouble(),-10:N2}");
                }
                Console.WriteLine("-----------------------------------------------------------------");
            }

            // 3. Crear Pedido
            Console.WriteLine("\n[3] Generando un pedido nuevo...");
            var pedidoRequest = new
            {
                clienteId = clienteId,
                items = new[]
                {
                    new { pizzaId = 3, cantidad = 2 }, // Pizza Muzzarella
                    new { pizzaId = 1, cantidad = 1 }  // Pizza Pepperoni
                }
            };

            Console.WriteLine("Enviando pedido a la API...");
            var orderResponse = await _httpClient.PostAsJsonAsync("/api/pedidos", pedidoRequest);

            if (orderResponse.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                var errObj = await orderResponse.Content.ReadFromJsonAsync<JsonElement>();
                Console.WriteLine($"\n[ERROR 503] La Cocina no está disponible en este momento.");
                Console.WriteLine($"Detalle: {errObj.GetProperty("error").GetString()}");
                Console.ResetColor();
                return;
            }

            if (!orderResponse.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                string errRaw = await orderResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"[ERROR] Error al crear pedido: {orderResponse.StatusCode} - {errRaw}");
                Console.ResetColor();
                return;
            }

            var orderJson = await orderResponse.Content.ReadFromJsonAsync<JsonElement>();
            int pedidoId = orderJson.GetProperty("pedidoId").GetInt32();
            string estadoInicial = orderJson.GetProperty("estado").GetString() ?? "";
            double total = orderJson.GetProperty("total").GetDouble();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[EXITO] Pedido creado exitosamente!");
            Console.WriteLine($"Pedido ID: {pedidoId} | Estado Inicial: {estadoInicial} | Total: ${total:N2}");
            Console.ResetColor();

            // 4. Encuestas (Polling) periódicas para ver el estado
            Console.WriteLine("\n[4] Iniciando polling de estado de pedido...");
            string estadoActual = estadoInicial;

            while (estadoActual != "Entregado" && estadoActual != "Cancelado")
            {
                await Task.Delay(2000);
                
                var pollResponse = await _httpClient.GetAsync($"/api/pedidos/{pedidoId}");
                if (!pollResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[POLLING] Error consultando estado: {pollResponse.StatusCode}");
                    continue;
                }

                var pollJson = await pollResponse.Content.ReadFromJsonAsync<JsonElement>();
                estadoActual = pollJson.GetProperty("estado").GetString() ?? "";
                string ultimaActualizacion = pollJson.GetProperty("ultimaActualizacion").GetString() ?? "";

                Console.ForegroundColor = estadoActual switch
                {
                    "EsperaConfirmacion" => ConsoleColor.Yellow,
                    "EnPreparacion" => ConsoleColor.Blue,
                    "EnViaje" => ConsoleColor.Magenta,
                    "Entregado" => ConsoleColor.Green,
                    "Cancelado" => ConsoleColor.Red,
                    _ => ConsoleColor.White
                };

                Console.WriteLine($"[POLLING] {DateTime.Now:HH:mm:ss} - Pedido #{pedidoId} - Estado: {estadoActual} (Act: {ultimaActualizacion})");
                Console.ResetColor();
            }

            Console.WriteLine("\n==================================================");
            if (estadoActual == "Entregado")
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("        ¡PIZZA ENTREGADA! ¡BUEN PROVECHO!");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("        EL PEDIDO FUE CANCELADO.");
            }
            Console.ResetColor();
            Console.WriteLine("==================================================");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[ERROR CRITICO] Ocurrió una excepción: {ex.Message}");
            Console.WriteLine("Asegúrese de que el servidor Minimal API esté ejecutándose en http://localhost:5000");
            Console.ResetColor();
        }

        Console.WriteLine("\nPresione ENTER para salir...");
        Console.ReadLine();
    }
}
