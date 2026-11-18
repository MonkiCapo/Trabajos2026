using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Core.Pizzeria.Entidades;
using Core.Pizzeria.Servicios.IRepositorios;
using Core.Pizzeria.Servicios.Enum;

namespace Dapper.Pizzeria;

public class PedidoRepositorio : DapperRepo, IPedidoRepositorio
{
    // Proyeccion auxiliar: cada fila es un ajuste de un item del pedido.
    // No es una entidad de negocio, asi que queda privada al repositorio.
    private class AjusteItem
    {
        public int ItemPedidoId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
    }

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
        if (items.Count == 0)
        {
            return;
        }

        // INSERT multi-fila: un pedido con N items hace un solo round-trip a la
        // base en lugar de N. Los parametros se numeran para no repetir nombres.
        var filas = new List<string>(items.Count);
        var parametros = new DynamicParameters();

        for (var indice = 0; indice < items.Count; indice++)
        {
            var item = items[indice];
            item.PedidoId = pedidoId;

            filas.Add($"(@PedidoId{indice}, @PizzaId{indice}, @Cantidad{indice}, @PrecioUnitario{indice}, @Tamanio{indice})");
            parametros.Add($"PedidoId{indice}", pedidoId);
            parametros.Add($"PizzaId{indice}", item.PizzaId);
            parametros.Add($"Cantidad{indice}", item.Cantidad);
            parametros.Add($"PrecioUnitario{indice}", item.PrecioUnitario);
            parametros.Add($"Tamanio{indice}", item.Tamanio);
        }

        var sql = $@"
            INSERT INTO ITEM_PEDIDO (pedido_id, pizza_id, cantidad, precio_unitario, tamanio)
            VALUES {string.Join(", ", filas)};";

        await conexion.ExecuteAsync(sql, parametros, transaction);

        // Los AUTO_INCREMENT se asignan en el orden de las filas del INSERT,
        // asi que al releerlos por pedido (recien creado, sin items previos)
        // el orden coincide con el de la lista. Cada item necesita su id para
        // colgar los ajustes.
        var ids = await conexion.QueryAsync<int>(
            "SELECT id FROM ITEM_PEDIDO WHERE pedido_id = @PedidoId ORDER BY id;",
            new { PedidoId = pedidoId }, transaction);

        var posicion = 0;
        foreach (var id in ids)
        {
            items[posicion].Id = id;
            posicion++;
        }
    }

    public async Task CrearAjustesAsync(int itemPedidoId, IEnumerable<int> ingredienteIds, string tipo, IDbConnection conexion, IDbTransaction transaction)
    {
        var ids = ingredienteIds.ToList();
        if (ids.Count == 0)
        {
            return;
        }

        // INSERT multi-fila: los ajustes de un item se insertan de una sola vez.
        var filas = new List<string>(ids.Count);
        var parametros = new DynamicParameters();
        parametros.Add("ItemPedidoId", itemPedidoId);
        parametros.Add("Tipo", tipo);

        for (var indice = 0; indice < ids.Count; indice++)
        {
            filas.Add($"(@ItemPedidoId, @IngredienteId{indice}, @Tipo)");
            parametros.Add($"IngredienteId{indice}", ids[indice]);
        }

        var sql = $@"
            INSERT INTO ITEM_PEDIDO_AJUSTE (item_pedido_id, ingrediente_id, tipo)
            VALUES {string.Join(", ", filas)};";

        await conexion.ExecuteAsync(sql, parametros, transaction);
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

    public async Task<bool> ActualizarEstadoAsync(int pedidoId, EstadoPedido estadoActual, EstadoPedido nuevoEstado, IDbConnection conexion, IDbTransaction transaction)
    {
        // UPDATE condicional: si otro proceso ya cambio el estado, no se afecta
        // ninguna fila y el servicio lo toma como conflicto de concurrencia.
        var sql = @"
            UPDATE PEDIDO 
            SET estado_id = @NuevoEstadoId, fecha_actualizacion = @FechaActualizacion 
            WHERE id = @Id AND estado_id = @EstadoActualId";
        var rowsAffected = await conexion.ExecuteAsync(sql, new
        {
            NuevoEstadoId = (int)nuevoEstado,
            EstadoActualId = (int)estadoActual,
            FechaActualizacion = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            Id = pedidoId
        }, transaction);
        return rowsAffected > 0;
    }

    public async Task<Pedido?> ObtenerPedidoPorIdAsync(int id)
    {
        using var conexion = NuevaConexion();
        return await ObtenerPedidoPorIdAsync(id, conexion, null);
    }

    public async Task<Pedido?> ObtenerPedidoPorIdAsync(int id, IDbConnection conexion, IDbTransaction? transaction)
    {
        const string sqlPedido = @"
            SELECT id, cliente_id AS ClienteId, estado_id AS Estado, fecha_creacion AS FechaCreacion, 
                   fecha_actualizacion AS FechaActualizacion, total
            FROM PEDIDO
            WHERE id = @Id";

        const string sqlItems = @"
            SELECT ip.id, ip.pedido_id AS PedidoId, ip.pizza_id AS PizzaId, ip.cantidad, 
                   ip.precio_unitario AS PrecioUnitario, ip.tamanio AS Tamanio, pz.nombre AS PizzaNombre
            FROM ITEM_PEDIDO ip
            JOIN PIZZA pz ON ip.pizza_id = pz.id
            WHERE ip.pedido_id = @PedidoId";

        var pedido = await conexion.QueryFirstOrDefaultAsync<Pedido>(sqlPedido, new { Id = id }, transaction);
        if (pedido == null) return null;

        // Las dos consultas van sobre la misma conexion: el pedido y sus items
        // se leen de forma consistente, sin abrir dos conexiones al pool.
        var items = await conexion.QueryAsync<ItemPedido>(sqlItems, new { PedidoId = id }, transaction);
        pedido.Items = items.ToList();

        // Ajustes de "Arma tu pizza": una unica consulta para todos los items
        // del pedido (sin N+1), agrupada en memoria por item y tipo.
        if (pedido.Items.Count > 0)
        {
            const string sqlAjustes = @"
                SELECT a.item_pedido_id AS ItemPedidoId, a.tipo AS Tipo, i.nombre AS Nombre
                FROM ITEM_PEDIDO_AJUSTE a
                JOIN INGREDIENTE i ON i.id = a.ingrediente_id
                WHERE a.item_pedido_id IN @Ids
                ORDER BY a.id";

            var idsDeItems = pedido.Items.Select(item => item.Id).ToList();
            var ajustes = await conexion.QueryAsync<AjusteItem>(sqlAjustes, new { Ids = idsDeItems }, transaction);

            foreach (var ajuste in ajustes)
            {
                var item = pedido.Items.FirstOrDefault(i => i.Id == ajuste.ItemPedidoId);
                if (item is null) continue;

                if (string.Equals(ajuste.Tipo, "quitar", StringComparison.OrdinalIgnoreCase))
                {
                    item.Quitar.Add(ajuste.Nombre);
                }
                else
                {
                    item.Extras.Add(ajuste.Nombre);
                }
            }
        }

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
}
