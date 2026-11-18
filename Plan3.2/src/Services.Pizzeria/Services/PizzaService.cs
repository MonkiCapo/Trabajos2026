using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Core.Pizzeria.DTOs;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.Excepciones;
using Core.Pizzeria.Servicios.IRepositorios;
using Core.Pizzeria.Servicios.IService;

namespace Services.Pizzeria.Services;

public class PizzaService : IPizzaService
{
    private readonly IPizzaRepositorio _pizzaRepo;
    private readonly IIngredienteRepositorio _ingredienteRepo;
    private readonly IValidator<PizzaRequest> _validadorPizza;
    private readonly IAdo _ado;

    public PizzaService(
        IPizzaRepositorio pizzaRepo,
        IIngredienteRepositorio ingredienteRepo,
        IValidator<PizzaRequest> validadorPizza,
        IAdo ado)
    {
        _pizzaRepo = pizzaRepo;
        _ingredienteRepo = ingredienteRepo;
        _validadorPizza = validadorPizza;
        _ado = ado;
    }

    public async Task<IEnumerable<Pizza>> ObtenerPizzasAsync()
    {
        return await _pizzaRepo.ObtenerPizzasAsync();
    }

    public async Task<Pizza?> ObtenerPizzaPorNombreAsync(string nombre)
    {
        return await _pizzaRepo.ObtenerPizzaPorNombreAsync(nombre);
    }

    public async Task<Pizza> CrearPizzaAsync(PizzaRequest request)
    {
        await ValidarAsync(request);

        // Nombre unico: el UNIQUE de la BD es el backstop, esto da un 400 amigable.
        if (await _pizzaRepo.ObtenerPizzaPorNombreAsync(request.Nombre.Trim()) is not null)
        {
            throw new ValidationException($"Ya existe una pizza con el nombre '{request.Nombre}'.");
        }

        using var conexion = _ado.GetDbConnection();
        conexion.Open();
        using var transaction = conexion.BeginTransaction();

        try
        {
            var ingredienteIds = await ResolverIngredientesAsync(request.Ingredientes, conexion, transaction);

            var nueva = new Pizza
            {
                Nombre = request.Nombre.Trim(),
                Tamanio = request.Tamanio.Trim(),
                Precio = request.Precio
            };

            nueva.Id = await _pizzaRepo.InsertarPizzaAsync(nueva, conexion, transaction);
            await _pizzaRepo.ReemplazarIngredientesAsync(nueva.Id, ingredienteIds, conexion, transaction);

            transaction.Commit();

            nueva.Ingredientes = request.Ingredientes
                .Select(n => n.Trim())
                .Where(n => n.Length > 0)
                .ToList();

            return nueva;
        }
        catch (PizzaYaExisteException)
        {
            transaction.Rollback();
            throw new ValidationException($"Ya existe una pizza con el nombre '{request.Nombre}'.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<Pizza?> ActualizarPizzaAsync(int id, PizzaRequest request)
    {
        await ValidarAsync(request);

        var existente = await _pizzaRepo.ObtenerPizzaPorIdAsync(id);
        if (existente == null)
        {
            return null;
        }

        // Nombre unico excluyendo a la propia pizza.
        var conMismoNombre = await _pizzaRepo.ObtenerPizzaPorNombreAsync(request.Nombre.Trim());
        if (conMismoNombre is not null && conMismoNombre.Id != id)
        {
            throw new ValidationException($"Ya existe una pizza con el nombre '{request.Nombre}'.");
        }

        using var conexion = _ado.GetDbConnection();
        conexion.Open();
        using var transaction = conexion.BeginTransaction();

        try
        {
            var ingredienteIds = await ResolverIngredientesAsync(request.Ingredientes, conexion, transaction);

            existente.Nombre = request.Nombre.Trim();
            existente.Tamanio = request.Tamanio.Trim();
            existente.Precio = request.Precio;

            await _pizzaRepo.ActualizarPizzaAsync(existente, conexion, transaction);
            await _pizzaRepo.ReemplazarIngredientesAsync(id, ingredienteIds, conexion, transaction);

            transaction.Commit();
        }
        catch (PizzaYaExisteException)
        {
            transaction.Rollback();
            throw new ValidationException($"Ya existe una pizza con el nombre '{request.Nombre}'.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        // Releer con la lista de ingredientes ya persistida.
        return await _pizzaRepo.ObtenerPizzaPorIdAsync(id);
    }

    public async Task<bool> EliminarPizzaAsync(int id)
    {
        // PizzaEnUsoException (FK de ITEM_PEDIDO) viaja hasta el endpoint,
        // que la traduce a 409.
        var afectadas = await _pizzaRepo.EliminarPizzaAsync(id);
        return afectadas > 0;
    }

    private async Task ValidarAsync(PizzaRequest request)
    {
        var resultado = await _validadorPizza.ValidateAsync(request);
        if (!resultado.IsValid)
        {
            throw new ValidationException(resultado.Errors);
        }
    }

    // Convierte los nombres de ingredientes del request en ids existentes.
    // Los nombres que no estan en la BD se informan en un unico mensaje.
    private async Task<List<int>> ResolverIngredientesAsync(List<string> nombres, IDbConnection conexion, IDbTransaction transaction)
    {
        var pedidos = nombres.Select(n => n.Trim()).Where(n => n.Length > 0).ToList();
        if (pedidos.Count == 0)
        {
            return new List<int>();
        }

        var existentes = (await _ingredienteRepo.ObtenerPorNombresAsync(pedidos, conexion, transaction)).ToList();

        var faltantes = pedidos
            .Where(n => !existentes.Any(e => e.Nombre.Equals(n, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (faltantes.Count > 0)
        {
            throw new ValidationException($"Ingredientes inexistentes: {string.Join(", ", faltantes)}.");
        }

        return existentes.Select(e => e.Id).Distinct().ToList();
    }
}
