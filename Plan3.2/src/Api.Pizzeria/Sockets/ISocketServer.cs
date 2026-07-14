using System.Threading;
using System.Threading.Tasks;
using Core.Pizzeria.Entidades;

namespace Api.Pizzeria.Sockets;

public interface ISocketServer
{
    Task<bool> EnviarPedidoACocinaAsync(Pedido pedido, CancellationToken ct);
    Task<bool> EnviarPedidoARepartoAsync(Pedido pedido, string direccion, CancellationToken ct);
}
