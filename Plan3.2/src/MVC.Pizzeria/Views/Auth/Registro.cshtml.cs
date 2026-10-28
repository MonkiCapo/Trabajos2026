using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace MVC.Pizzeria.Views.Auth
{
    public class Registro : PageModel
    {
        private readonly ILogger<Registro> _logger;

        public Registro(ILogger<Registro> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}