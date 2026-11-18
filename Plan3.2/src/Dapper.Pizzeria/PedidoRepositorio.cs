using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.Enum;
using Core.Pizzeria.Servicios.IRepositorios;

namespace Dapper.Pizzeria;

public class PedidoRepositorio : DapperRepo, IPedidoRepositorio
{
    public PedidoRepositorio(IAdo ado) : base(ado) { }

    public async Task<int> CrearPedidoAsync(Pedido pedido, DbConnection conexion, DbTransaction transaction, CancellationToken ct = default)
    {
        var sql = @"INSERT INTO PEDIDO (cliente_id, estado_id, fecha_creacion, fecha_actualizacion, total)
                    VALUES (@ClienteId, @EstadoId, @FechaCreacion, @FechaActualizacion, @Total);
                    SELECT LAST_INSERT_ID();";

        return await conexion.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
        {
            pedido.ClienteId,
            EstadoId = (int)pedido.Estado,
            pedido.FechaCreacion,
            pedido.FechaActualizacion,
            pedido.Total
        }, transaction, cancellationToken: ct));
    }

    public async Task CrearItemsPedidoAsync(List<ItemPedido> items, int pedidoId, DbConnection conexion, DbTransaction transaction, CancellationToken ct = default)
    {
        if (items.Count == 0)
        {
            return;
        }

        var valores = new List<string>(items.Count);
        var parametros = new DynamicParameters();

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            valores.Add($"(@PedidoId{i}, @PizzaId{i}, @Cantidad{i}, @PrecioUnitario{i})");
            parametros.Add($"PedidoId{i}", pedidoId);
            parametros.Add($"PizzaId{i}", item.PizzaId);
            parametros.Add($"Cantidad{i}", item.Cantidad);
            parametros.Add($"PrecioUnitario{i}", item.PrecioUnitario);
        }

        var sql = $"INSERT INTO ITEM_PEDIDO (pedido_id, pizza_id, cantidad, precio_unitario) VALUES {string.Join(", ", valores)};";
        await conexion.ExecuteAsync(new CommandDefinition(sql, parametros, transaction, cancellationToken: ct));
    }

    public async Task CrearHistorialAsync(int pedidoId, EstadoPedido estado, string observacion, DbConnection conexion, DbTransaction transaction, CancellationToken ct = default)
    {
        const string sql = @"INSERT INTO HISTORIAL_ESTADO_PEDIDO (pedido_id, estado_id, fecha_cambio, observacion)
                             VALUES (@PedidoId, @EstadoId, @FechaCambio, @Observacion);";

        await conexion.ExecuteAsync(new CommandDefinition(sql, new
        {
            PedidoId = pedidoId,
            EstadoId = (int)estado,
            FechaCambio = DateTime.UtcNow,
            Observacion = observacion
        }, transaction, cancellationToken: ct));
    }

    public async Task<bool> ActualizarEstadoAsync(int pedidoId, EstadoPedido nuevoEstado, DbConnection conexion, DbTransaction transaction, CancellationToken ct = default)
    {
        const string sql = @"UPDATE PEDIDO
                             SET estado_id = @EstadoId, fecha_actualizacion = @FechaActualizacion
                             WHERE id = @Id;";

        var rowsAffected = await conexion.ExecuteAsync(new CommandDefinition(sql, new
        {
            EstadoId = (int)nuevoEstado,
            FechaActualizacion = DateTime.UtcNow,
            Id = pedidoId
        }, transaction, cancellationToken: ct));
        return rowsAffected > 0;
    }

    public async Task<Pedido?> ObtenerPedidoPorIdAsync(int id, CancellationToken ct = default)
    {
        using var conexion = await AbrirConexionAsync(ct);

        const string sqlPedido = @"
            SELECT id, cliente_id AS ClienteId, estado_id AS Estado, fecha_creacion AS FechaCreacion,
                   fecha_actualizacion AS FechaActualizacion, total
            FROM PEDIDO
            WHERE id = @Id;";

        var pedido = await conexion.QueryFirstOrDefaultAsync<Pedido>(new CommandDefinition(sqlPedido, new { Id = id }, cancellationToken: ct));
        if (pedido == null)
        {
            return null;
        }

        const string sqlItems = @"
            SELECT ip.id, ip.pedido_id AS PedidoId, ip.pizza_id AS PizzaId, ip.cantidad,
                   ip.precio_unitario AS PrecioUnitario, pz.nombre AS PizzaNombre
            FROM ITEM_PEDIDO ip
            JOIN PIZZA pz ON ip.pizza_id = pz.id
            WHERE ip.pedido_id = @PedidoId;";

        var items = await conexion.QueryAsync<ItemPedido>(new CommandDefinition(sqlItems, new { PedidoId = id }, cancellationToken: ct));
        pedido.Items = items.ToList();

        return pedido;
    }

    public async Task<EstadoPedido?> ObtenerEstadoActualAsync(int pedidoId, CancellationToken ct = default)
    {
        using var conexion = await AbrirConexionAsync(ct);
        const string sql = "SELECT estado_id FROM PEDIDO WHERE id = @Id;";
        var estadoId = await conexion.QueryFirstOrDefaultAsync<int?>(new CommandDefinition(sql, new { Id = pedidoId }, cancellationToken: ct));
        return estadoId.HasValue ? (EstadoPedido)estadoId.Value : null;
    }

    public async Task<IEnumerable<Pedido>> ObtenerPedidosAsync(EstadoPedido? estado = null, CancellationToken ct = default)
    {
        using var conexion = await AbrirConexionAsync(ct);

        var sql = @"
            SELECT id, cliente_id AS ClienteId, estado_id AS Estado, fecha_creacion AS FechaCreacion,
                   fecha_actualizacion AS FechaActualizacion, total
            FROM PEDIDO";

        if (estado.HasValue)
        {
            sql += " WHERE estado_id = @EstadoId";
        }

        sql += " ORDER BY id;";

        return await conexion.QueryAsync<Pedido>(new CommandDefinition(sql, estado.HasValue ? new { EstadoId = (int)estado.Value } : null, cancellationToken: ct));
    }
}