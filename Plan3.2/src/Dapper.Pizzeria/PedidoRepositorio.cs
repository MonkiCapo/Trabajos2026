using System.Data;
using Dapper;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;
using Core.Pizzeria.Servicios.Enum;

namespace Dapper.Pizzeria;

public class PedidoRepositorio : DapperRepo, IPedidoRepositorio
{
    public PedidoRepositorio(IAdo _ado) : base(_ado) { }

    public Pedido AgregarPedido(Pedido pedido)
    {
        var sql = @"INSERT INTO PEDIDO (cliente_id, estado_id, fecha_creacion, fecha_actualizacion, total)
                    VALUES (@ClienteId, @EstadoId, @FechaCreacion, @FechaActualizacion, @Total);
                    SELECT LAST_INSERT_ID();";

        var id = Conexion.ExecuteScalar<int>(sql, new
        {
            pedido.ClienteId,
            EstadoId = (int)pedido.Estado,
            FechaCreacion = pedido.FechaCreacion.ToString("yyyy-MM-dd HH:mm:ss"),
            FechaActualizacion = pedido.FechaActualizacion.ToString("yyyy-MM-dd HH:mm:ss"),
            pedido.Total
        });
        pedido.Id = id;

        // Insertar items del pedido
        const string insertItemSql = @"
            INSERT INTO ITEM_PEDIDO (pedido_id, pizza_id, cantidad, precio_unitario)
            VALUES (@PedidoId, @PizzaId, @Cantidad, @PrecioUnitario);";

        foreach (var item in pedido.Items)
        {
            item.PedidoId = pedido.Id;
            Conexion.Execute(insertItemSql, new
            {
                item.PedidoId,
                item.PizzaId,
                item.Cantidad,
                item.PrecioUnitario
            });
        }

        // Insertar historial inicial
        const string insertHistorialSql = @"
            INSERT INTO HISTORIAL_ESTADO_PEDIDO (pedido_id, estado_id, fecha_cambio, observacion)
            VALUES (@PedidoId, @EstadoId, @FechaCambio, @Observacion);";

        Conexion.Execute(insertHistorialSql, new
        {
            PedidoId = pedido.Id,
            EstadoId = (int)pedido.Estado,
            FechaCambio = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            Observacion = "Creación de pedido. Esperando confirmación de cocina."
        });

        return pedido;
    }

    public bool ActualizarEstado(int pedidoId, EstadoPedido nuevoEstado)
    {
        var sql = @"
            UPDATE PEDIDO 
            SET estado_id = @EstadoId, fecha_actualizacion = @FechaActualizacion 
            WHERE id = @Id";
        var rowsAffected = Conexion.Execute(sql, new
        {
            EstadoId = (int)nuevoEstado,
            FechaActualizacion = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            Id = pedidoId
        });
        return rowsAffected > 0;
    }

    public Pedido ObtenerPedidoPorId(int id)
    {
        const string sqlPedido = @"
            SELECT id, cliente_id AS ClienteId, estado_id AS Estado, fecha_creacion AS FechaCreacion, 
                   fecha_actualizacion AS FechaActualizacion, total
            FROM PEDIDO
            WHERE id = @Id";

        var pedido = Conexion.QueryFirstOrDefault<Pedido>(sqlPedido, new { Id = id });
        if (pedido == null) return null;

        const string sqlItems = @"
            SELECT ip.id, ip.pedido_id AS PedidoId, ip.pizza_id AS PizzaId, ip.cantidad, 
                   ip.precio_unitario AS PrecioUnitario, pz.nombre AS PizzaNombre
            FROM ITEM_PEDIDO ip
            JOIN PIZZA pz ON ip.pizza_id = pz.id
            WHERE ip.pedido_id = @PedidoId";

        var items = Conexion.Query<ItemPedido>(sqlItems, new { PedidoId = id });
        pedido.Items = items.ToList();

        return pedido;
    }

    public IEnumerable<Pedido> ObtenerPedidos()
    {
        var sql = @"
            SELECT id, cliente_id AS ClienteId, estado_id AS Estado, fecha_creacion AS FechaCreacion, 
                   fecha_actualizacion AS FechaActualizacion, total
            FROM PEDIDO;";
        return Conexion.Query<Pedido>(sql);
    }
}
