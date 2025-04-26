# TaskGeniusApi

TaskGeniusApi es una API desarrollada en .NET 8 que proporciona funcionalidades para la gestión de tareas y usuarios, así como recomendaciones inteligentes para mejorar la productividad. Este proyecto forma parte de un portafolio profesional y está diseñado para demostrar habilidades en el desarrollo de aplicaciones backend escalables y seguras.

## Características principales

- **Gestión de usuarios**: Registro, actualización y eliminación de usuarios.
- **Gestión de tareas**: Creación, actualización, eliminación y consulta de tareas.
- **Recomendaciones inteligentes**: Generación de consejos personalizados para mejorar la gestión de tareas.
- **Autenticación y autorización**: Implementación de JWT para garantizar la seguridad de la API.
- **Paginación**: Respuesta optimizada para grandes conjuntos de datos.

## Estructura del proyecto

El proyecto sigue una arquitectura modular y está organizado en las siguientes carpetas principales:

- **Controllers**: Contiene los controladores que manejan las solicitudes HTTP.
- **Data**: Incluye el contexto de la base de datos (Entity Framework Core).
- **DTOs**: Define los objetos de transferencia de datos utilizados para la comunicación entre capas.
- **Models**: Contiene los modelos de datos principales.
- **Services**: Implementa la lógica de negocio y los servicios de la aplicación.
- **Migrations**: Archivos generados por Entity Framework Core para la gestión de la base de datos.

## Tecnologías utilizadas

- **.NET 8**: Framework principal para el desarrollo de la API.
- **Entity Framework Core**: ORM para la gestión de la base de datos.
- **JWT (JSON Web Tokens)**: Para la autenticación y autorización.
- **SQLite**: Base de datos utilizada en el desarrollo.


## Endpoints principales

- **Autenticación**:
  - `POST /auth/login`: Inicia sesión y genera un token JWT.
  - `POST /auth/register`: Registra un nuevo usuario.

- **Usuarios**:
  - `GET /users`: Obtiene la lista de usuarios.
  - `PUT /users/{id}`: Actualiza un usuario existente.

- **Tareas**:
  - `GET /tasks`: Obtiene la lista de tareas.
  - `POST /tasks`: Crea una nueva tarea.
  - `PUT /tasks/{id}`: Actualiza una tarea existente.
  - `DELETE /tasks/{id}`: Elimina una tarea.

## Despliegue

El proyecto se encuentra desplegado en: [taskgeniusapi-production.up.railway.app](https://taskgeniusapi-production.up.railway.app)

## Contacto

Si tienes preguntas o comentarios sobre este proyecto, no dudes en contactarme a través de mi portafolio o correo electrónico.

---

Este proyecto es parte de mi portafolio profesional y está diseñado para demostrar mis habilidades en el desarrollo de aplicaciones backend modernas.