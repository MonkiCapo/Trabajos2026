using System.Data;
using Dapper;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;
using Core.Pizzeria.Servicios.Enum;

namespace Dapper.Pizzeria;

public class PedidoRepositorio : DapperRepo, IPedidoRepositorio
{
    public PedidoRepositorio(IAdo ado) : base(ado) { }

    public async Task<int> CrearPedidoAsync(Pedido pedido, IDbConnection conexion, IDbTransaction transaction)
    {
        var sql = @"INSERT INTO PEDIDO (cliente_id, estado_id, fecha_creacion, fecha_actualizacion, total)
                    VALUES (@ClienteId, @EstadoId, @FechaCreacion, @FechaActualizacion, @Total);
                    SELECT LAST_INSERT_ID();";

        return await conexion.ExecuteScalarAsync<int>(sql, new
        {
            pedido.ClienteId,
            EstadoId = (int)pedido.Estado,
            FechaCreacion = pedido.FechaCreacion.ToString("yyyy-MM-dd HH:mm:ss"),
            FechaActualizacion = pedido.FechaActualizacion.ToString("yyyy-MM-dd HH:mm:ss"),
            pedido.Total
        }, transaction);
    }

    public async Task CrearItemsPedidoAsync(List<ItemPedido> items, int pedidoId, IDbConnection conexion, IDbTransaction transaction)
    {
        const string insertItemSql = @"
            INSERT INTO ITEM_PEDIDO (pedido_id, pizza_id, cantidad, precio_unitario)
            VALUES (@PedidoId, @PizzaId, @Cantidad, @PrecioUnitario);";

        foreach (var item in items)
        {
            item.PedidoId = pedidoId;
            await conexion.ExecuteAsync(insertItemSql, new
            {
                item.PedidoId,
                item.PizzaId,
                item.Cantidad,
                item.PrecioUnitario
            }, transaction);
        }
    }

    public async Task CrearHistorialAsync(int pedidoId, EstadoPedido estado, string observacion, IDbConnection conexion, IDbTransaction transaction)
    {
        const string insertHistorialSql = @"
            INSERT INTO HISTORIAL_ESTADO_PEDIDO (pedido_id, estado_id, fecha_cambio, observacion)
            VALUES (@PedidoId, @EstadoId, @FechaCambio, @Observacion);";

        await conexion.ExecuteAsync(insertHistorialSql, new
        {
            PedidoId = pedidoId,
            EstadoId = (int)estado,
            FechaCambio = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            Observacion = observacion
        }, transaction);
    }

    public async Task<bool> ActualizarEstadoAsync(int pedidoId, EstadoPedido nuevoEstado, IDbConnection conexion, IDbTransaction transaction)
    {
        var sql = @"
            UPDATE PEDIDO 
            SET estado_id = @EstadoId, fecha_actualizacion = @FechaActualizacion 
            WHERE id = @Id";
        var rowsAffected = await conexion.ExecuteAsync(sql, new
        {
            EstadoId = (int)nuevoEstado,
            FechaActualizacion = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            Id = pedidoId
        }, transaction);
        return rowsAffected > 0;
    }

    public async Task<Pedido?> ObtenerPedidoPorIdAsync(int id)
    {
        const string sqlPedido = @"
            SELECT id, cliente_id AS ClienteId, estado_id AS Estado, fecha_creacion AS FechaCreacion, 
                   fecha_actualizacion AS FechaActualizacion, total
            FROM PEDIDO
            WHERE id = @Id";

        const string sqlItems = @"
            SELECT ip.id, ip.pedido_id AS PedidoId, ip.pizza_id AS PizzaId, ip.cantidad, 
                   ip.precio_unitario AS PrecioUnitario, pz.nombre AS PizzaNombre
            FROM ITEM_PEDIDO ip
            JOIN PIZZA pz ON ip.pizza_id = pz.id
            WHERE ip.pedido_id = @PedidoId";

        using var conexion = NuevaConexion();

        var pedido = await conexion.QueryFirstOrDefaultAsync<Pedido>(sqlPedido, new { Id = id });
        if (pedido == null) return null;

        // Las dos consultas van sobre la misma conexion: el pedido y sus items
        // se leen de forma consistente, sin abrir dos conexiones al pool.
        var items = await conexion.QueryAsync<ItemPedido>(sqlItems, new { PedidoId = id });
        pedido.Items = items.ToList();

        return pedido;
    }

    public async Task<IEnumerable<HistorialEstadoPedido>> ObtenerHistorialAsync(int pedidoId)
    {
        const string sql = @"
            SELECT h.id,
                   h.pedido_id AS PedidoId,
                   h.estado_id AS Estado,
                   h.fecha_cambio AS FechaCambio,
                   h.observacion AS Observacion
            FROM HISTORIAL_ESTADO_PEDIDO h
            WHERE h.pedido_id = @PedidoId
            ORDER BY h.fecha_cambio, h.id";

        using var conexion = NuevaConexion();

        return await conexion.QueryAsync<HistorialEstadoPedido>(sql, new { PedidoId = pedidoId });
    }

    public async Task<IEnumerable<Pedido>> ObtenerPedidosAsync()
    {
        var sql = @"
            SELECT id, cliente_id AS ClienteId, estado_id AS Estado, fecha_creacion AS FechaCreacion, 
                   fecha_actualizacion AS FechaActualizacion, total
            FROM PEDIDO;";

        using var conexion = NuevaConexion();

        return await conexion.QueryAsync<Pedido>(sql);
    }
}
