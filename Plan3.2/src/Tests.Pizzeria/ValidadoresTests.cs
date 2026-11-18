using Core.Pizzeria.DTOs;
using Services.Pizzeria.Validations;

namespace Tests.Pizzeria;

public class ItemRequestValidatorTests
{
    private readonly ItemRequestValidator _validador = new();

    [Fact]
    public void SinAjustes_EsValido()
    {
        var item = new ItemRequest { PizzaNombre = "Muzzarella", Cantidad = 1 };

        Assert.True(_validador.Validate(item).IsValid);
    }

    [Fact]
    public void ConQuitarYExtras_EsValido()
    {
        var item = new ItemRequest
        {
            PizzaNombre = "Muzzarella",
            Cantidad = 1,
            Quitar = new List<string> { "Salsa" },
            Extras = new List<string> { "Jamón" }
        };

        Assert.True(_validador.Validate(item).IsValid);
    }

    [Fact]
    public void Extras_MasDelMaximo_Rechaza()
    {
        var item = new ItemRequest
        {
            PizzaNombre = "Muzzarella",
            Cantidad = 1,
            Extras = new List<string> { "A", "B", "C", "D", "E", "F" }
        };

        var resultado = _validador.Validate(item);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Extras");
    }

    [Fact]
    public void Extras_ConNombresRepetidos_Rechaza()
    {
        var item = new ItemRequest
        {
            PizzaNombre = "Muzzarella",
            Cantidad = 1,
            Extras = new List<string> { "Jamón", "jamón" }
        };

        Assert.False(_validador.Validate(item).IsValid);
    }

    [Fact]
    public void Quitar_ConNombresRepetidos_Rechaza()
    {
        var item = new ItemRequest
        {
            PizzaNombre = "Muzzarella",
            Cantidad = 1,
            Quitar = new List<string> { "Salsa", "SALSA" }
        };

        Assert.False(_validador.Validate(item).IsValid);
    }

    [Fact]
    public void QuitarYExtra_Solapados_Rechaza()
    {
        var item = new ItemRequest
        {
            PizzaNombre = "Muzzarella",
            Cantidad = 1,
            Quitar = new List<string> { "Salsa" },
            Extras = new List<string> { "salsa" }
        };

        Assert.False(_validador.Validate(item).IsValid);
    }

    [Fact]
    public void Quitar_ConNombreVacio_Rechaza()
    {
        var item = new ItemRequest
        {
            PizzaNombre = "Muzzarella",
            Cantidad = 1,
            Quitar = new List<string> { " " }
        };

        Assert.False(_validador.Validate(item).IsValid);
    }

    [Fact]
    public void Tamanio_Vacio_EsValido()
    {
        // Sin tamanio se asume "Grande": no es un error estructural.
        var item = new ItemRequest { PizzaNombre = "Muzzarella", Cantidad = 1 };

        Assert.True(_validador.Validate(item).IsValid);
    }

    [Fact]
    public void Tamanio_DemasiadoLargo_Rechaza()
    {
        var item = new ItemRequest
        {
            PizzaNombre = "Muzzarella",
            Cantidad = 1,
            Tamanio = new string('x', 51)
        };

        var resultado = _validador.Validate(item);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Tamanio");
    }
}

public class PedidoRequestValidatorTests
{
    private readonly PedidoRequestValidator _validador = new();

    private static PedidoRequest RequestValido() => new()
    {
        ClienteEmail = "juan@test.com",
        Items = new List<ItemRequest> { new() { PizzaNombre = "Muzzarella", Cantidad = 1 } }
    };

    [Fact]
    public void PedidoValido_EsValido()
    {
        Assert.True(_validador.Validate(RequestValido()).IsValid);
    }

    [Fact]
    public void Email_Vacio_Rechaza()
    {
        var request = RequestValido();
        request.ClienteEmail = string.Empty;

        var resultado = _validador.Validate(request);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "ClienteEmail");
    }

    [Fact]
    public void Email_ConFormatoInvalido_Rechaza()
    {
        var request = RequestValido();
        request.ClienteEmail = "esto no es un email";

        Assert.False(_validador.Validate(request).IsValid);
    }

    [Fact]
    public void Items_Vacio_Rechaza()
    {
        var request = RequestValido();
        request.Items.Clear();

        var resultado = _validador.Validate(request);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Items");
    }

    [Fact]
    public void Cantidad_Cero_Rechaza()
    {
        var request = RequestValido();
        request.Items[0].Cantidad = 0;

        Assert.False(_validador.Validate(request).IsValid);
    }

    [Fact]
    public void Cantidad_MayorAlMaximo_Rechaza()
    {
        var request = RequestValido();
        request.Items[0].Cantidad = 101;

        Assert.False(_validador.Validate(request).IsValid);
    }
}

public class CheckoutRequestValidatorTests
{
    private readonly CheckoutRequestValidator _validador = new();

    private static CheckoutRequest RequestValido() => new()
    {
        Nombre = "Juan",
        Telefono = "11 5555-4444",
        Direccion = "Calle Falsa 123",
        Items = new List<ItemRequest> { new() { PizzaNombre = "Muzzarella", Cantidad = 1 } }
    };

    [Fact]
    public void CheckoutValido_EsValido()
    {
        Assert.True(_validador.Validate(RequestValido()).IsValid);
    }

    [Fact]
    public void Nombre_Vacio_Rechaza()
    {
        var request = RequestValido();
        request.Nombre = string.Empty;

        Assert.False(_validador.Validate(request).IsValid);
    }

    [Fact]
    public void Telefono_ConLetras_Rechaza()
    {
        var request = RequestValido();
        request.Telefono = "abc";

        Assert.False(_validador.Validate(request).IsValid);
    }

    [Fact]
    public void CarritoVacio_Rechaza()
    {
        var request = RequestValido();
        request.Items.Clear();

        Assert.False(_validador.Validate(request).IsValid);
    }

    [Fact]
    public void Cantidad_MayorA99_Rechaza()
    {
        var request = RequestValido();
        request.Items[0].Cantidad = 100;

        Assert.False(_validador.Validate(request).IsValid);
    }
}

public class PizzaRequestValidatorTests
{
    private readonly PizzaRequestValidator _validador = new();

    private static PizzaRequest RequestValido() => new()
    {
        Nombre = "Muzzarella",
        Tamanio = "Grande",
        Precio = 10m,
        Ingredientes = new List<string> { "Muzzarella", "Salsa" }
    };

    [Fact]
    public void PizzaValida_EsValida()
    {
        Assert.True(_validador.Validate(RequestValido()).IsValid);
    }

    [Fact]
    public void Nombre_Vacio_Rechaza()
    {
        var request = RequestValido();
        request.Nombre = string.Empty;

        Assert.False(_validador.Validate(request).IsValid);
    }

    [Fact]
    public void Precio_Cero_Rechaza()
    {
        var request = RequestValido();
        request.Precio = 0m;

        Assert.False(_validador.Validate(request).IsValid);
    }

    [Fact]
    public void Precio_ConMasDeDosDecimales_Rechaza()
    {
        var request = RequestValido();
        request.Precio = 10.999m;

        Assert.False(_validador.Validate(request).IsValid);
    }

    [Fact]
    public void Ingredientes_Repetidos_Rechaza()
    {
        var request = RequestValido();
        request.Ingredientes.Add("muzzarella");

        Assert.False(_validador.Validate(request).IsValid);
    }

    [Fact]
    public void Tamanio_Vacio_Rechaza()
    {
        var request = RequestValido();
        request.Tamanio = string.Empty;

        Assert.False(_validador.Validate(request).IsValid);
    }
}

public class TamanioUpdateRequestValidatorTests
{
    private readonly TamanioUpdateRequestValidator _validador = new();

    [Theory]
    [InlineData(0.10)]
    [InlineData(0.75)]
    [InlineData(1.00)]
    [InlineData(3.00)]
    public void Multiplicador_EnRango_EsValido(double multiplicador)
    {
        var request = new TamanioUpdateRequest { Multiplicador = (decimal)multiplicador };

        Assert.True(_validador.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(0.05)]
    [InlineData(0)]
    [InlineData(3.01)]
    [InlineData(-1)]
    public void Multiplicador_FueraDeRango_Rechaza(double multiplicador)
    {
        var request = new TamanioUpdateRequest { Multiplicador = (decimal)multiplicador };

        var resultado = _validador.Validate(request);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Multiplicador");
    }

    [Fact]
    public void Multiplicador_ConMasDeDosDecimales_Rechaza()
    {
        var request = new TamanioUpdateRequest { Multiplicador = 0.755m };

        Assert.False(_validador.Validate(request).IsValid);
    }
}
