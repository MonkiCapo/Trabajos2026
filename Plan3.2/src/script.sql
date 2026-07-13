DROP DATABASE IF EXISTS 5to_Pizzeria;
CREATE DATABASE 5to_Pizzeria;
USE 5to_Pizzeria;

-- 1. CLIENTE
CREATE TABLE IF NOT EXISTS CLIENTE (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    telefono VARCHAR(20) NOT NULL,
    direccion VARCHAR(200) NOT NULL
);

-- 2. PIZZA
CREATE TABLE IF NOT EXISTS PIZZA (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    tamanio VARCHAR(50) NOT NULL,
    precio DECIMAL(10,2) NOT NULL,
    descripcion TEXT
);

-- 3. INGREDIENTE
CREATE TABLE IF NOT EXISTS INGREDIENTE (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL
);

-- 4. PIZZA_INGREDIENTE
CREATE TABLE IF NOT EXISTS PIZZA_INGREDIENTE (
    pizza_id INT NOT NULL,
    ingrediente_id INT NOT NULL,
    PRIMARY KEY (pizza_id, ingrediente_id),
    FOREIGN KEY (pizza_id) REFERENCES PIZZA (id) ON DELETE CASCADE,
    FOREIGN KEY (ingrediente_id) REFERENCES INGREDIENTE (id) ON DELETE CASCADE
);

-- 5. ESTADO_PEDIDO
CREATE TABLE IF NOT EXISTS ESTADO_PEDIDO (
    id INT PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL,
    orden INT
);

-- 6. PEDIDO
CREATE TABLE IF NOT EXISTS PEDIDO (
    id INT AUTO_INCREMENT PRIMARY KEY,
    cliente_id INT NOT NULL,
    estado_id INT NOT NULL,
    fecha_creacion DATETIME NOT NULL,
    fecha_actualizacion DATETIME NOT NULL,
    total DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (cliente_id) REFERENCES CLIENTE (id),
    FOREIGN KEY (estado_id) REFERENCES ESTADO_PEDIDO (id)
);

-- 7. ITEM_PEDIDO
CREATE TABLE IF NOT EXISTS ITEM_PEDIDO (
    id INT AUTO_INCREMENT PRIMARY KEY,
    pedido_id INT NOT NULL,
    pizza_id INT NOT NULL,
    cantidad INT NOT NULL,
    precio_unitario DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (pedido_id) REFERENCES PEDIDO (id) ON DELETE CASCADE,
    FOREIGN KEY (pizza_id) REFERENCES PIZZA (id)
);

-- 8. HISTORIAL_ESTADO_PEDIDO
CREATE TABLE IF NOT EXISTS HISTORIAL_ESTADO_PEDIDO (
    id INT AUTO_INCREMENT PRIMARY KEY,
    pedido_id INT NOT NULL,
    estado_id INT NOT NULL,
    fecha_cambio DATETIME NOT NULL,
    observacion TEXT,
    FOREIGN KEY (pedido_id) REFERENCES PEDIDO (id) ON DELETE CASCADE,
    FOREIGN KEY (estado_id) REFERENCES ESTADO_PEDIDO (id)
);

-- INSERCION DE DATOS SEMILLA

-- EstadoPedido
INSERT IGNORE INTO ESTADO_PEDIDO (id, nombre, orden) VALUES (1, 'EsperaConfirmacion', 1);
INSERT IGNORE INTO ESTADO_PEDIDO (id, nombre, orden) VALUES (2, 'EnPreparacion', 2);
INSERT IGNORE INTO ESTADO_PEDIDO (id, nombre, orden) VALUES (3, 'EnViaje', 3);
INSERT IGNORE INTO ESTADO_PEDIDO (id, nombre, orden) VALUES (4, 'Entregado', 4);
INSERT IGNORE INTO ESTADO_PEDIDO (id, nombre, orden) VALUES (5, 'Cancelado', 5);

-- Pizzas
INSERT IGNORE INTO PIZZA (id, nombre, tamanio, precio, descripcion) VALUES (1, 'Pizza Pepperoni', 'Grande', 1500.00, 'Queso muzzarella, salsa de tomate y abundante pepperoni.');
INSERT IGNORE INTO PIZZA (id, nombre, tamanio, precio, descripcion) VALUES (2, 'Pizza Jamón y Queso', 'Grande', 1400.00, 'Queso muzzarella, jamón cocido y aceitunas.');
INSERT IGNORE INTO PIZZA (id, nombre, tamanio, precio, descripcion) VALUES (3, 'Pizza Muzzarella', 'Grande', 1200.00, 'Doble queso muzzarella, salsa de tomate y orégano.');
INSERT IGNORE INTO PIZZA (id, nombre, tamanio, precio, descripcion) VALUES (4, 'Pizza Napolitana', 'Grande', 1300.00, 'Queso muzzarella, rodajas de tomate, ajo y albahaca fresco.');

-- Ingredientes
INSERT IGNORE INTO INGREDIENTE (id, nombre) VALUES (1, 'Muzzarella');
INSERT IGNORE INTO INGREDIENTE (id, nombre) VALUES (2, 'Pepperoni');
INSERT IGNORE INTO INGREDIENTE (id, nombre) VALUES (3, 'Jamón cocido');
INSERT IGNORE INTO INGREDIENTE (id, nombre) VALUES (4, 'Salsa de tomate');
INSERT IGNORE INTO INGREDIENTE (id, nombre) VALUES (5, 'Orégano');
INSERT IGNORE INTO INGREDIENTE (id, nombre) VALUES (6, 'Aceitunas');
INSERT IGNORE INTO INGREDIENTE (id, nombre) VALUES (7, 'Tomate en rodajas');
INSERT IGNORE INTO INGREDIENTE (id, nombre) VALUES (8, 'Ajo');
INSERT IGNORE INTO INGREDIENTE (id, nombre) VALUES (9, 'Albahaca');

-- Mapeo de Pizza Ingrediente
INSERT IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (1, 1);
INSERT IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (1, 2);
INSERT IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (1, 4);
INSERT IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (1, 5);

INSERT IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (2, 1);
INSERT IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (2, 3);
INSERT IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (2, 4);
INSERT IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (2, 6);

INSERT IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (3, 1);
INSERT IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (3, 4);
INSERT IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (3, 5);
INSERT IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (3, 6);

INSERT IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (4, 1);
INSERT IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (4, 4);
INSERT IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (4, 7);
INSERT IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (4, 8);
INSERT IGNORE INTO PIZZA_INGREDIENTE (pizza_id, ingrediente_id) VALUES (4, 9);
