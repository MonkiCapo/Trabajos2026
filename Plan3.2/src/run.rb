require 'rbconfig'

def open_browser(url)
  case RbConfig::CONFIG['host_os']
  when /mswin|mingw|cygwin/
    system("start #{url}")
  when /darwin/
    system("open #{url}")
  when /linux|bsd/
    system("xdg-open '#{url}'")
  end
end

def launch_terminal(title, command)
  if RbConfig::CONFIG['host_os'] =~ /mswin|mingw|cygwin/
    # En Windows, start "titulo" cmd /k "comando"
    system("start \"#{title}\" cmd /k \"#{command}\"")
  else
    # Linux: detecta la terminal disponible en la distribución
    terminals = [
      "x-terminal-emulator -T '#{title}' -e 'bash -c \"#{command}; exec bash\"'",
      "gnome-terminal --title='#{title}' -- bash -c '#{command}; exec bash'",
      "konsole --new-tab -p tabtitle='#{title}' -e bash -c '#{command}; exec bash'",
      "xfce4-terminal --title='#{title}' -e 'bash -c \"#{command}; exec bash\"'",
      "alacritty -T '#{title}' -e bash -c '#{command}; exec bash'",
      "kitty -T '#{title}' bash -c '#{command}; exec bash'",
      "xterm -title '#{title}' -e 'bash -c \"#{command}; exec bash\"'"
    ]

    launched = false
    terminals.each do |term_cmd|
      term_name = term_cmd.split.first
      if system("which #{term_name} > /dev/null 2>&1")
        Process.spawn(term_cmd)
        launched = true
        break
      end
    end

    # Fallback si no hay emulador gráfico conocido
    unless launched
      Process.spawn("bash", "-c", command)
    end
  end
end

puts "================================================"
puts "Iniciando el MVC y la API a la vez"
puts "================================================"

# Lanzar ambos servicios en terminales independientes
launch_terminal("API Pizzeria (5183)", "cd Api.Pizzeria && dotnet run")
launch_terminal("MVC Pizzeria (5080)", "cd MVC.Pizzeria && dotnet run")

puts "\n✅ Servidores lanzados en terminales independientes:"
puts "   🔹 API Docs (Scalar):  http://localhost:5183/scalar/v1"
puts "   🔹 MVC Web:            http://127.0.0.1:5080"
puts "   🔹 Catálogo Pizzas:    http://127.0.0.1:5080/Home/Productos"
puts "\n💡 (Podés hacer Ctrl + Clic sobre cualquier link para abrirlo)"

puts "\nPresioná [ENTER] cuando quieras abrir la página en el navegador..."
gets

open_browser("http://127.0.0.1:5080/Home/Productos")
puts "🌐 ¡Navegador abierto!"
