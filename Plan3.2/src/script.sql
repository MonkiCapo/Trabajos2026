-- Habilitar claves foráneas en SQLite
PRAGMA foreign_keys = ON;

-- 1. CLIENTE
CREATE TABLE IF NOT EXISTS CLIENTE (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    nombre TEXT NOT NULL,
    telefono TEXT NOT NULL,
    direccion TEXT NOT NULL
);

-- 2. PIZZA
CREATE TABLE IF NOT EXISTS PIZZA (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    nombre TEXT NOT NULL,
    tamanio TEXT NOT NULL,
    precio REAL NOT NULL,
    descripcion TEXT
);

-- 3. INGREDIENTE
CREATE TABLE IF NOT EXISTS INGREDIENTE (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    nombre TEXT NOT NULL
);

-- 4. PIZZA_INGREDIENTE
CREATE TABLE IF NOT EXISTS PIZZA_INGREDIENTE (
    pizza_id INTEGER NOT NULL,
    ingrediente_id INTEGER NOT NULL,
    PRIMARY KEY (pizza_id, ingrediente_id),
    FOREIGN KEY (pizza_id) REFERENCES PIZZA (id) ON DELETE CASCADE,
    FOREIGN KEY (ingrediente_id) REFERENCES INGREDIENTE (id) ON DELETE CASCADE
);

-- 5. ESTADO_PEDIDO
CREATE TABLE IF NOT EXISTS ESTADO_PEDIDO (
    id INTEGER PRIMARY KEY,
    nombre TEXT NOT NULL,
    orden INTEGER
);

-- 6. PEDIDO
CREATE TABLE IF NOT EXISTS PEDIDO (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    cliente_id INTEGER NOT NULL,
    estado_id INTEGER NOT NULL,
    fecha_creacion TEXT NOT NULL,
    fecha_actualizacion TEXT NOT NULL,
    total REAL NOT NULL,
    FOREIGN KEY (cliente_id) REFERENCES CLIENTE (id),
    FOREIGN KEY (estado_id) REFERENCES ESTADO_PEDIDO (id)
);

-- 7. ITEM_PEDIDO
CREATE TABLE IF NOT EXISTS ITEM_PEDIDO (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    pedido_id INTEGER NOT NULL,
    pizza_id INTEGER NOT NULL,
    cantidad INTEGER NOT NULL,
    precio_unitario REAL NOT NULL,
    FOREIGN KEY (pedido_id) REFERENCES PEDIDO (id) ON DELETE CASCADE,
    FOREIGN KEY (pizza_id) REFERENCES PIZZA (id)
);

-- 8. HISTORIAL_ESTADO_PEDIDO
CREATE TABLE IF NOT EXISTS HISTORIAL_ESTADO_PEDIDO (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    pedido_id INTEGER NOT NULL,
    estado_id INTEGER NOT NULL,
    fecha_cambio TEXT NOT NULL,
    observacion TEXT,
    FOREIGN KEY (pedido_id) REFERENCES PEDIDO (id) ON DELETE CASCADE,
    FOREIGN KEY (estado_id) REFERENCES ESTADO_PEDIDO (id)
);

-- INSERCIÓN DE DATOS SEMILLA

-- EstadoPedido
INSERT OR IGNORE INTO ESTADO_PEDIDO (id, nombre, orden) VALUES (1, 'EsperaConfirmacion', 1);
INSERT OR IGNORE INTO ESTADO_PEDIDO (id, nombre, orden) VALUES (2, 'EnPreparacion', 2);
INSERT OR IGNORE INTO ESTADO_PEDIDO (id, nombre, orden) VALUES (3, 'EnViaje', 3);
INSERT OR IGNORE INTO ESTADO_PEDIDO (id, nombre, orden) VALUES (4, 'Entregado', 4);
INSERT OR IGNORE INTO ESTADO_PEDIDO (id, nombre, orden) VALUES (5, 'Cancelado', 5);

-- Pizzas
INSERT OR IGNORE INTO PIZZA (id, nombre, tamanio, precio, descripcion) VALUES (1, 'Pizza Pepperoni', 'Grande', 1500.00, 'Queso muzzarella, salsa de tomate y abundante pepperoni.');
INSERT OR IGNORE INTO PIZZA (id, nombre, tamanio, precio, descripcion) VALUES (2, 'Pizza Jamón y Queso', 'Grande', 1400.00, 'Queso muzzarella, jamón cocido y aceitunas.');
INSERT OR IGNORE INTO PIZZA (id, nombre, tamanio, precio, descripcion) VALUES (3, 'Pizza Muzzarella', 'Grande', 1200.00, 'Doble queso muzzarella, salsa de tomate y orégano.');
INSERT OR IGNORE INTO PIZZA (id, nombre, tamanio, precio, descripcion) VALUES (4, 'Pizza Napolitana', 'Grande', 1300.00, 'Queso muzzarella, rodajas de tomate, ajo y albahaca fresco.');

-- Ingredientes
INSERT OR IGNORE INTO INGREDIENTE (id, nombre) VALUES (1, 'Muzzarella');
INSERT OR IGNORE INTO INGREDIENTE (id, nombre) VALUES (2, 'Pepperoni');
INSERT OR IGNORE INTO INGREDIENTE (id, nombre) VALUES (3, 'Jamón cocido');
INSERT OR IGNORE INTO INGREDIENTE (id, nombre) VALUES (4, 'Salsa de tomate');
INSERT OR IGNORE INTO INGREDIENTE (id, nombre) VALUES (5, 'Orégano');
INSERT OR IGNORE INTO INGREDIENTE (id, nombre) VALUES (6, 'Aceitunas');
INSERT OR IGNORE INTO INGREDIENTE (id, nombre) VALUES (7, 'Tomate en rodajas');
INSERT OR IGNORE INTO INGREDIENTE (id, nombre) VALUES (8, 'Ajo');
INSERT OR IGNORE INTO INGREDIENTE (id, nombre) VALUES (9, 'Albahaca');

-- Mapeo de Pizza Ingrediente
INSERT OR IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (1, 1);
INSERT OR IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (1, 2);
INSERT OR IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (1, 4);
INSERT OR IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (1, 5);

INSERT OR IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (2, 1);
INSERT OR IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (2, 3);
INSERT OR IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (2, 4);
INSERT OR IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (2, 6);

INSERT OR IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (3, 1);
INSERT OR IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (3, 4);
INSERT OR IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (3, 5);
INSERT OR IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (3, 6);

INSERT OR IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (4, 1);
INSERT OR IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (4, 4);
INSERT OR IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (4, 7);
INSERT OR IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (4, 8);
INSERT OR IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (4, 9);
