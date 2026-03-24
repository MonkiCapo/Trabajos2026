# 4. ¿Y si se abren los huevos?

Para incorporar nuevos pájaros a la solución sin alterar la lógica de la isla o los eventos, nos apoyamos en los siguientes conceptos de la Programación Orientada a Objetos:

1. **Herencia y Clases Abstractas**: Al tener una clase base abstracta `Pajaro`, definimos un contrato general de qué es un Pájaro en nuestra isla (tiene `Ira`, calcula una `Fuerza`, puede `Enojarse` y `Tranquilizarse`).
2. **Polimorfismo**: La isla trata a todos sus habitantes como objetos del tipo abstracto `Pajaro`. La isla no necesita saber si el pájaro es de la clase `Red`, `Chuck` o una nueva clase. Al pedirle su `.Fuerza` o al llamar a su método `.Enojarse()`, cada pájaro sabe cómo responder de acuerdo a su propia implementación específica de las reglas.
3. **Principio Abierto/Cerrado (Open/Closed Principle de SOLID)**: El diseño actual asegura que nuestra aplicación esté *abierta a la extensión* (podemos agregar tantos pájaros nuevos como queramos simplemente heredando de `Pajaro` y sobreescribiendo sus métodos) pero *cerrada a la modificación* (no necesitamos modificar clases existentes como `IslaPajaro` o los `IEvento` añadiendo sentencias `if`, `switch` o casteando tipos de datos cada vez que introducimos un tipo distinto de pájaro).
