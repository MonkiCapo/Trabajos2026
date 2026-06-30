# Configuración del archivo
ARCHIVO_OBJETIVO = "Relevamiento_Actividad2_PizzeriaAPI(1).md"

# Verificar si el archivo existe en la misma carpeta
unless File.exist?(ARCHIVO_OBJETIVO)
  puts "¡Error! No se encontró el archivo '#{ARCHIVO_OBJETIVO}'."
  puts "Asegúrate de ejecutar el script en la misma carpeta donde está el archivo .md"
  exit
end

puts "Procesando '#{ARCHIVO_OBJETIVO}'..."
contenido = File.read(ARCHIVO_OBJETIVO)

# Expresión regular que busca:
# 1. El texto exacto "[cite_start]"
# 2. El patrón "[cite: seguido de números, comas y espacios, terminado en ]"
# Se usa Regexp.escape para que los corchetes [ ] se traten como texto y no como código Regex
regex_citas = /#{Regexp.escape("[cite_start]")}|\[cite:\s*[\d,\s]+\]/

# Reemplazamos todas las coincidencias por un string vacío
contenido_limpio = contenido.gsub(regex_citas, "")

# Guardamos los cambios en el mismo archivo
File.write(ARCHIVO_OBJETIVO, contenido_limpio)

puts "¡Listo! Se han eliminado todas las etiquetas de cita del archivo Markdown."
